using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace DreamPotato.Core;

/// <summary>
/// Flycast integration: sending/receiving Maple messages over TCP and forwarding them to the emulated VMU.
/// </summary>
public class MapleMessageBroker
{
    private const int MaxMaplePacketSize = 1025;
    public const int BasePort = 37393;

    // The Maple message bytes are encoded as ASCII hex digits separated by spaces. e.g.
    // Message 0x01020304 is encoded as "04 03 02 01 \r\n"
    // Maple sends most of its data in 32-bit words which seem to get endian-swapped before and after transmission.
    private readonly byte[] _rawSocketBuffer = new byte[MaxMaplePacketSize * 4];
    private readonly List<byte> _currentInboundMessageBuilder = [];

    /// <summary>
    /// Guards <see cref="_clientConnected"/>, <see cref="_inboundMessages"/>, <see cref="_outboundMessages"/>
    /// </summary>
    private readonly Lock _lock = new Lock();

    /// <summary>Guarded by <see cref="_lock"/>.</summary>
    private Socket? _clientSocket;

    /// <summary>Guarded by <see cref="_lock"/>.</summary>
    private readonly Queue<MapleMessage> _inboundMessages = [];

    /// <summary>Guarded by <see cref="_lock"/>.</summary>
    private readonly Queue<MapleMessage> _outboundMessages = [];

    private Task? _serverTask;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    public MapleMessageBroker()
    {
    }

    public bool IsRunning => _serverTask != null;

    public void ShutdownServer()
    {
        _cancellationTokenSource.Cancel();
        _serverTask = null;
    }

    public void StartServer()
    {
        if (_serverTask != null || _cancellationTokenSource.IsCancellationRequested)
            throw new InvalidOperationException();

        _serverTask = Task.Run(() => StartServerWorker(_cancellationTokenSource.Token));
    }

    /// <summary>Can be called by threads besides the socket server thread.</summary>
    internal bool TryReceiveMessage(out MapleMessage mapleMessage)
    {
        lock (_lock)
        {
            return _inboundMessages.TryDequeue(out mapleMessage);
        }
    }

    /// <summary>Can be called by threads besides the socket server thread.</summary>
    internal void SendMessage(MapleMessage mapleMessage)
    {
        lock (_lock)
        {
            _outboundMessages.Enqueue(mapleMessage);
        }
    }

    /// <summary>Can be called by threads besides the socket server thread.</summary>
    internal bool IsConnected
    {
        get
        {
            lock (_lock)
            {
                return _clientSocket?.Connected ?? false;
            }
        }
    }

    internal void Disconnect()
    {
        lock (_lock)
        {
            if (_clientSocket is not null)
            {
                _clientSocket.Shutdown(SocketShutdown.Both);
                _clientSocket.Disconnect(reuseSocket: false);
            }
        }
    }

    internal MapleMessage PeekInboundMessage_TestingOnly()
    {
        lock (_lock)
        {
            _inboundMessages.TryPeek(out var result);
            return result;
        }
    }

    internal MapleMessage DequeueOutboundMessage()
    {
        lock (_lock)
        {
            _outboundMessages.TryDequeue(out var result);
            return result;
        }
    }

    internal void ScanAsciiHexFragment(ReadOnlySpan<byte> fragment)
    {
        for (int i = 0; i < fragment.Length; i++)
        {
            var @byte = fragment[i];
            _currentInboundMessageBuilder.Add(@byte);
            if (@byte == '\r' && i < fragment.Length - 1 && fragment[i + 1] == '\n')
            {
                i++; // skip past the '\r', loop header will skip past the '\n'
                _currentInboundMessageBuilder.Add((byte)'\n');
                DecodeAndSubmitInboundMessage();
            }
        }
    }

    private int DecodeAsciiHexLine(byte[] dest)
    {
        Debug.Assert(_currentInboundMessageBuilder.Count > 0);
        Debug.Assert(_currentInboundMessageBuilder is [.., (byte)'\r', (byte)'\n']);

        int destIndex = 0;
        for (int i = 0; i < _currentInboundMessageBuilder.Count - 2;)
        {
            var msb = _currentInboundMessageBuilder[i++];
            var lsb = _currentInboundMessageBuilder[i++];
            var byteValue = fromAsciiHexDigits(msb, lsb);
            dest[destIndex++] = byteValue;

            if (_currentInboundMessageBuilder[i] == (byte)' ')
                i++;
        }

        return destIndex;

        static byte fromAsciiHexDigits(byte msb, byte lsb)
        {
            var result = (getNybble(msb) << 4) | getNybble(lsb);
            return (byte)result;

            static int getNybble(byte asciiHexDigit)
            {
                if (asciiHexDigit is >= (byte)'0' and <= (byte)'9')
                    return asciiHexDigit - (byte)'0';

                if (asciiHexDigit is >= (byte)'a' and <= (byte)'f')
                    return asciiHexDigit - (byte)'a' + 10;

                if (asciiHexDigit is >= (byte)'A' and <= (byte)'F')
                    return asciiHexDigit - (byte)'A' + 10;

                throw new ArgumentException($"'{(char)asciiHexDigit}' was not an ascii hex digit", nameof(asciiHexDigit));
            }
        }
    }

    private void DecodeAndSubmitInboundMessage()
    {
        var rawBytes = new byte[_currentInboundMessageBuilder.Count / 3];
        var rawLength = DecodeAsciiHexLine(rawBytes);
        var rawSpan = rawBytes.AsSpan(start: 0, rawLength);

        var type = (MapleMessageType)rawSpan[0];
        var recipient = new MapleAddress(rawSpan[1]);
        var sender = new MapleAddress(rawSpan[2]);
        var length = rawSpan[3];

        int[] additionalWords = new int[length];
        for (int i = 4, destIndex = 0; i < rawSpan.Length; i += 4, destIndex++)
        {
            additionalWords[destIndex] = BinaryPrimitives.ReadInt32LittleEndian(rawSpan[i..(i + 4)]);
        }
        var message = new MapleMessage()
        {
            Type = type,
            Recipient = recipient,
            Sender = sender,
            Length = length,
            AdditionalWords = additionalWords,
        };

        _currentInboundMessageBuilder.Clear();

        lock (_lock)
        {
            _inboundMessages.Enqueue(message);
        }
    }

    internal int EncodeAsciiHexData(MapleMessage message, byte[] dest)
    {
        byte[] messageBytes = new byte[4 * (message.Length + 1)];
        message.WriteTo(messageBytes, out var bytesWritten);
        Debug.Assert(bytesWritten == messageBytes.Length);

        int destIndex = 0;
        for (int i = 0; i < messageBytes.Length; i++)
        {
            var (msb, lsb) = toAsciiHexDigits(messageBytes[i]);
            dest[destIndex++] = msb;
            dest[destIndex++] = lsb;
            if (i == messageBytes.Length - 1)
            {
                dest[destIndex++] = (byte)'\r';
                dest[destIndex++] = (byte)'\n';
            }
            else
            {
                dest[destIndex++] = (byte)' ';
            }
        }
        return destIndex;

        static (byte msb, byte lsb) toAsciiHexDigits(byte value)
        {
            byte msb = getSingleAsciiHexDigit((value & 0xf0) >> 4);
            byte lsb = getSingleAsciiHexDigit(value & 0xf);
            return (msb, lsb);

            static byte getSingleAsciiHexDigit(int nybble)
            {
                Debug.Assert((nybble & 0xf) == nybble);
                if (nybble is >= 0xa and <= 0xf)
                {
                    byte c = (byte)('A' + (nybble - 0xa));
                    return c;
                }
                else
                {
                    byte c = (byte)('0' + nybble);
                    return c;
                }
            }
        }
    }

    private async Task StartServerWorker(CancellationToken cancellationToken)
    {
        using var listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, BasePort));
        listener.Listen(backlog: 1);

        while (true) // Accept a new client whenever one disconnects
        {
            Console.WriteLine("Waiting for client");
            var clientSocket = await listener.AcceptAsync(cancellationToken);
            Console.WriteLine("Client connected");
            clientSocket.ReceiveTimeout = 50;
            lock (_lock)
            {
                _clientSocket = clientSocket;
            }
            try
            {
                while (clientSocket.Connected)
                {
                    // Receive message.
                    if (clientSocket.Available > 0)
                    {
                        var receivedLen = await clientSocket.ReceiveAsync(_rawSocketBuffer, SocketFlags.None, cancellationToken);
                        if (_currentInboundMessageBuilder.Count == 0)
                            Console.Write($"Received message: {Encoding.UTF8.GetString(_rawSocketBuffer.AsSpan(start: 0, length: receivedLen))}");
                        else
                            Console.Write(Encoding.UTF8.GetString(_rawSocketBuffer.AsSpan(start: 0, length: receivedLen)));

                        ScanAsciiHexFragment(_rawSocketBuffer.AsSpan()[0..receivedLen]);
                    }

                    // TODO: a condition variable/semaphore seems appropriate here so this thread is not just hammering the queue waiting for a message.
                    if (DequeueOutboundMessage() is { HasValue: true } outboundMessage)
                    {
                        var bytesWritten = EncodeAsciiHexData(outboundMessage, _rawSocketBuffer);
                        if (outboundMessage.Type != MapleMessageType.Ack)
                            Console.WriteLine($"Sending message: {Encoding.UTF8.GetString(_rawSocketBuffer.AsSpan(start: 0, length: bytesWritten))}");
                        await clientSocket.SendAsync(_rawSocketBuffer.AsMemory(start: 0, length: bytesWritten), cancellationToken);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Connection closed");
            }
            lock (_lock)
            {
                _clientSocket = null;
                _inboundMessages.Clear();
                _outboundMessages.Clear();
            }
        }
    }
}