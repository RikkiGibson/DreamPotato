using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

using Microsoft.Win32.SafeHandles;

namespace DreamPotato.Core;

/// <summary>
/// Flycast integration: sending/receiving Maple messages over TCP and forwarding them to the emulated VMU.
/// </summary>
public class MapleMessageBroker
{
    private const int MaxMaplePacketSize = 1025;
    public const int BasePort = 37393;

    /// <summary>A logger independent from Cpu for thread safety reasons.</summary>
    private readonly Logger Logger;

    /// <summary>
    /// Guards <see cref="_clientConnected"/>, <see cref="_vmuDocked"/>, <see cref="_vmuFlashData"/>, <see cref="_vmuFileHandle"/>.
    /// </summary>
    private readonly Lock _lock = new Lock();

    /// <summary>Guarded by <see cref="_lock"/>.</summary>
    private Socket? _clientSocket;

    private readonly Channel<MapleMessage> _inboundCpuMessages = Channel.CreateUnbounded<MapleMessage>();
    private readonly Channel<MapleMessage> _outboundMessages = Channel.CreateUnbounded<MapleMessage>();

    /// <summary>Guarded by <see cref="_lock"/>.</summary>
    private bool _vmuDocked;

    /// <summary>Guarded by <see cref="_lock"/>.</summary>
    private readonly byte[] _vmuFlashData = new byte[Cpu.FlashSize];

    /// <summary>Guarded by <see cref="_lock"/>.</summary>
    private SafeFileHandle? _vmuFileHandle;
    private DreamcastPort _dreamcastPort;

    private Task? _serverTask;
    private CancellationTokenSource? _cancellationTokenSource;
    public MapleMessageBroker(LogLevel minimumLogLevel)
    {
        Logger = new Logger(minimumLogLevel, LogCategories.Maple, _cpu: null);
    }

    public bool IsRunning => _serverTask != null;

    public void ShutdownServer()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;
        _serverTask = null;
    }

    public void StartServer(DreamcastPort dreamcastPort)
    {
        if (_serverTask != null || _cancellationTokenSource?.IsCancellationRequested == true)
            throw new InvalidOperationException();

        _dreamcastPort = dreamcastPort;
        _cancellationTokenSource = new CancellationTokenSource();
        _serverTask = Task.Run(() => SocketListenerEntryPoint(dreamcastPort, _cancellationTokenSource.Token));
    }

    internal void Resync(bool vmuDocked, bool writeToMapleFlash, Span<byte> flash, SafeFileHandle? vmuFileHandle)
    {
        // Keeping an extra copy of the flash memory and maintaining both is a pain, but, it is thought to be preferable to synchronizing on a single copy.
        // This is because the CPU needs to read it every cycle to decode instructions (which can change while emulation is running due to STF--store flash).
        // Acquiring a lock potentially ~100Ks of times per second is thought to be excessively expensive.
        // Acquiring a lock per-frame is too slow (can't wait up to 16ms to do I/O). Doing it lock-free also seems like it would be harder to get right than copying.
        // The basic design here is that when the VMU is "docked" in the controller, the current flash state is copied over to the socket thread.
        // The socket thread immediately responds to flash I/O requests rather than waiting for main game thread to handle them.
        // Write LCD and buzzer requests are still routed to main game thread, but no response is expected for those.
        lock (_lock)
        {
            bool dockedChanged = _vmuDocked != vmuDocked;
            _vmuDocked = vmuDocked;

            if (writeToMapleFlash)
                flash.CopyTo(_vmuFlashData);
            else
                _vmuFlashData.CopyTo(flash);

            _vmuFileHandle = vmuFileHandle;

            if (dockedChanged && _clientSocket is { })
            {
                // Send a message telling client to re-query devices
                var message = new MapleMessage() { Type = (MapleMessageType)0xff, Sender = new MapleAddress(0xff), Recipient = new MapleAddress(0xff), Length = 0xff, AdditionalWords = [] };
                var written = _outboundMessages.Writer.TryWrite(message);
                Debug.Assert(written); // Channel is unbounded, this should always succeed
            }
        }
    }

    /// <summary>Allows main game thread to receive messages affecting game-visible state (e.g. LCD, buzzer).</summary>
    internal bool TryReceiveCpuMessage(out MapleMessage mapleMessage)
    {
        return _inboundCpuMessages.Reader.TryRead(out mapleMessage);
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

    internal void ScanAsciiHexFragment(List<byte> asciiMessageBuilder, Queue<MapleMessage> inboundMessages, ReadOnlySpan<byte> fragment)
    {
        for (int i = 0; i < fragment.Length; i++)
        {
            var @byte = fragment[i];
            asciiMessageBuilder.Add(@byte);
            if (@byte == '\r' && i < fragment.Length - 1 && fragment[i + 1] == '\n')
            {
                i++; // skip past the '\r', loop header will skip past the '\n'
                asciiMessageBuilder.Add((byte)'\n');
                DecodeAndSubmitInboundMessage(asciiMessageBuilder, inboundMessages);
            }
        }
    }

    private int DecodeAsciiHexLine(List<byte> asciiMessageBuilder, byte[] dest)
    {
        Debug.Assert(asciiMessageBuilder.Count > 0);
        Debug.Assert(asciiMessageBuilder is [.., (byte)'\r', (byte)'\n']);

        int destIndex = 0;
        for (int i = 0; i < asciiMessageBuilder.Count - 2;)
        {
            var msb = asciiMessageBuilder[i++];
            var lsb = asciiMessageBuilder[i++];
            var byteValue = fromAsciiHexDigits(msb, lsb);
            dest[destIndex++] = byteValue;

            if (asciiMessageBuilder[i] == (byte)' ')
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

    private void DecodeAndSubmitInboundMessage(List<byte> asciiMessageBuilder, Queue<MapleMessage> inboundMessages)
    {
        var rawBytes = new byte[asciiMessageBuilder.Count / 3];
        var rawLength = DecodeAsciiHexLine(asciiMessageBuilder, rawBytes);
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

        asciiMessageBuilder.Clear();
        inboundMessages.Enqueue(message);
    }

    /// <summary>Internal for testing only.</summary>
    internal MapleMessage HandleMapleMessage(MapleMessage message)
    {
        // Don't allow the docking, connection, flash state etc to change while we are in the process of creating a reply.
        using var _ = _lock.EnterScope();

        Debug.Assert(message.HasValue);
        if (!_vmuDocked)
        {
            if ((message.Type, message.Function) == (MapleMessageType.GetCondition, MapleFunction.Input))
            {
                // Report no VMU in slot 1
                Logger.LogDebug("(GetCondition, Input): No VMUs", LogCategories.Maple);
                var reply = new MapleMessage()
                {
                    Type = MapleMessageType.Ack,
                    Recipient = new MapleAddress { Port = _dreamcastPort, Slot = DreamcastSlot.Dreamcast },
                    Sender = new MapleAddress { Port = _dreamcastPort, Slot = DreamcastSlot.Dreamcast },
                    Length = 0,
                    AdditionalWords = [],
                };
                return reply;
            }
            else
            {
                Logger.LogDebug($"Received unexpected message while VMU disconnected: ({message.Type}, {message.Function})", LogCategories.Maple);
                return default;
            }

            throw new InvalidOperationException("Unreachable code");
        }

        switch (message.Type, message.Function)
        {
            case (MapleMessageType.SetCondition, MapleFunction.Clock):
                // VMU beeps over maple are not supported.
                // The biggest reason is that if the remote emulator pauses,
                // we don't have any indication of when to stop producing a beep.
                // Making the very high pitched CF oscillator beeps sound right is also a pain.
                // It would realistically require pulling in a DSP library and/or spending a bunch of time with textbooks.
                return default;
            case (MapleMessageType.WriteBlock, MapleFunction.Clock):
                // Update the VMU's real time clock
                // This probably needs to forward to CPU, as ram state, not flash, needs to be updated.
                // Also, it's probably not meaningful to implement this until keeping the clock running while docked is supported.
                Logger.LogWarning($"(WriteBlock, Clock) not yet implemented", category: LogCategories.Maple);
                return default; // No reply
            case (MapleMessageType.WriteBlock, MapleFunction.LCD):
                // Cpu handles this message.
                var written = _inboundCpuMessages.Writer.TryWrite(message);
                Debug.Assert(written);
                return default; // No reply
            case (MapleMessageType.GetCondition, MapleFunction.Input):
                return handleGetConditionInput();
            case (MapleMessageType.ReadBlock, MapleFunction.Storage):
                return handleReadBlockStorage(message);
            case (MapleMessageType.WriteBlock, MapleFunction.Storage):
                return handleWriteBlockStorage(message);
            case (MapleMessageType.CompleteWrite, MapleFunction.Storage):
                return handleCompleteWriteStorage(message);
            default:
                Debug.Fail($"Unhandled Maple message '({message.Type}, {message.Function})'");
                Logger.LogError($"Unhandled Maple message '({message.Type}, {message.Function})'", category: LogCategories.Maple);
                return default; // No reply
        }

        MapleMessage handleGetConditionInput()
        {
            Logger.LogDebug("(GetCondition, Input): VMU in slot 1", LogCategories.Maple);
            var reply = new MapleMessage()
            {
                Type = MapleMessageType.Ack,
                Recipient = new MapleAddress { Port = _dreamcastPort, Slot = DreamcastSlot.Dreamcast },
                Sender = new MapleAddress { Port = _dreamcastPort, Slot = DreamcastSlot.Slot1 },
                Length = 0,
                AdditionalWords = [],
            };
            return reply;
        }

        MapleMessage handleReadBlockStorage(MapleMessage message)
        {
            var blockNumber = message.AdditionalWords[1] >> 24 & 0xff;
            var startAddress = blockNumber * Memory.WorkRamSize;
            var responseBytes = _vmuFlashData.AsSpan(startAddress, Memory.WorkRamSize);

            // TODO: verify phase and pt are zero

            const int responseSize = 130;
            Debug.Assert(responseSize == Memory.WorkRamSize / 4 + 2);
            var additionalWords = new int[responseSize];
            additionalWords[0] = (int)MapleFunction.Storage;
            additionalWords[1] = message.AdditionalWords[1];
            for (int i = 0; i < Memory.WorkRamSize / 4; i++)
            {
                additionalWords[i + 2] = BinaryPrimitives.ReadInt32LittleEndian(responseBytes[(i * 4)..((i + 1) * 4)]);
            }

            var reply = new MapleMessage()
            {
                Type = MapleMessageType.DataTransfer,
                Recipient = new MapleAddress { Port = _dreamcastPort, Slot = DreamcastSlot.Dreamcast },
                Sender = new MapleAddress { Port = _dreamcastPort, Slot = DreamcastSlot.Slot1 },
                Length = responseSize,
                AdditionalWords = additionalWords
            };
            return reply;
        }

        MapleMessage handleWriteBlockStorage(MapleMessage message)
        {
            const int preambleWordCount = 2; // 0: function ID, 1: block/phase IDs
            const int writePayloadWordCount = Memory.WorkRamSize / 4 / 4; // 4 phases, 4 bytes per word
            const int expectedSize = preambleWordCount + writePayloadWordCount;
            if (message.Length != expectedSize)
            {
                Logger.LogError($"Unexpected WriteBlock_Storage length: {message.Length}", LogCategories.Maple);
                return new MapleMessage()
                {
                    Type = MapleMessageType.ErrorInvalidFlashAddress,
                    Recipient = new MapleAddress { Port = _dreamcastPort, Slot = DreamcastSlot.Dreamcast },
                    Sender = new MapleAddress { Port = _dreamcastPort, Slot = DreamcastSlot.Slot1 },
                    Length = 0,
                    AdditionalWords = [],
                };
            }

            // Note: WriteBlock_Storage generally comes in sequences of 4, where the "phase" value is incremented.
            // Each message holds 128 bytes to be written.
            var blockNumber = (message.AdditionalWords[1] >> 24) & 0xff;
            var phaseNumber = (message.AdditionalWords[1] >> 8) & 0xff;
            if ((message.AdditionalWords[1] & 0xff) is not 0 and var pt)
                Logger.LogWarning($"Unexpected 'pt' value: {pt}", LogCategories.Maple);

            var startAddress = blockNumber * Memory.WorkRamSize + phaseNumber * Memory.WorkRamSize / 4;
            var destSpan = _vmuFlashData.AsSpan(startAddress, Memory.WorkRamSize / 4);

            for (int i = 0; i < writePayloadWordCount; i++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(destSpan[(i * 4)..((i + 1) * 4)], message.AdditionalWords[i + 2]);
            }

            if (_vmuFileHandle is not null)
            {
                Logger.LogDebug($"Writing to VMU file at address 0x{startAddress:X}", LogCategories.Maple);
                RandomAccess.Write(_vmuFileHandle, destSpan, fileOffset: startAddress);
            }

            // Also notify game thread so that we can flash the IO icon.
            var written = _inboundCpuMessages.Writer.TryWrite(message);
            Debug.Assert(written);

            var reply = new MapleMessage()
            {
                Type = MapleMessageType.Ack,
                Recipient = new MapleAddress { Port = _dreamcastPort, Slot = DreamcastSlot.Dreamcast },
                Sender = new MapleAddress { Port = _dreamcastPort, Slot = DreamcastSlot.Slot1 },
                Length = 0,
                AdditionalWords = [],
            };
            return reply;
        }

        MapleMessage handleCompleteWriteStorage(MapleMessage message)
        {
            if (message.Length != 2)
            {
                Logger.LogWarning($"Unexpected CompleteWrite message length: {message.Length}", LogCategories.Maple);
            }
            else
            {
                var phase = (message.AdditionalWords[1] >> 8) & 0xff;
                if (phase != 4)
                    Logger.LogWarning($"Unexpected CompleteWrite phase: {phase}", LogCategories.Maple);
            }

            // Also notify game thread so that we can flash the IO icon.
            var written = _inboundCpuMessages.Writer.TryWrite(message);
            Debug.Assert(written);

            var reply = new MapleMessage()
            {
                Type = MapleMessageType.Ack,
                Recipient = new MapleAddress { Port = _dreamcastPort, Slot = DreamcastSlot.Dreamcast },
                Sender = new MapleAddress { Port = _dreamcastPort, Slot = DreamcastSlot.Slot1 },
                Length = 0,
                AdditionalWords = [],
            };
            return reply;
        }
    }

    internal int EncodeAsciiHexData(MapleMessage message, byte[] dest)
    {
        byte[] messageBytes = new byte[4 * (message.EffectiveLength + 1)];
        message.WriteTo(messageBytes, out var bytesWritten);
        Debug.Assert(bytesWritten == messageBytes.Length || message.IsResetMessage);

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

    private async Task SocketReaderEntryPoint(Socket clientSocket, CancellationToken cancellationToken)
    {
        // The Maple message bytes are encoded as ASCII hex digits separated by spaces. e.g.
        // Message 0x01020304 is encoded as "04 03 02 01\r\n"
        // Maple sends most of its data in 32-bit words which seem to get endian-swapped before and after transmission.
        List<byte> asciiMessageBuilder = [];
        Queue<MapleMessage> localInboundMessages = [];
        byte[] rawSocketBuffer = new byte[MaxMaplePacketSize * 4];
        while (true)
        {
            var receivedLen = await clientSocket.ReceiveAsync(rawSocketBuffer, cancellationToken);
            if (receivedLen <= 0)
            {
                // disconnected
                asciiMessageBuilder.Clear();
                return;
            }

            if (asciiMessageBuilder.Count == 0)
                Logger.LogTrace($"Received message: {Encoding.UTF8.GetString(rawSocketBuffer.AsSpan(start: 0, length: receivedLen))}");
            else
                Logger.LogTrace(Encoding.UTF8.GetString(rawSocketBuffer.AsSpan(start: 0, length: receivedLen)));

            ScanAsciiHexFragment(asciiMessageBuilder, localInboundMessages, rawSocketBuffer.AsSpan()[0..receivedLen]);

            while (localInboundMessages.TryDequeue(out var message))
            {
                var outboundMessage = HandleMapleMessage(message);
                if (!outboundMessage.HasValue)
                    continue;

                await _outboundMessages.Writer.WriteAsync(outboundMessage, cancellationToken);
            }
        }
    }

    private async Task SocketWriterEntryPoint(Socket clientSocket, CancellationToken cancellationToken)
    {
        byte[] rawSocketBuffer = new byte[MaxMaplePacketSize * 4];
        while (true)
        {
            var outboundMessage = await _outboundMessages.Reader.ReadAsync(cancellationToken);
            Debug.Assert(outboundMessage.HasValue);

            var bytesWritten = EncodeAsciiHexData(outboundMessage, rawSocketBuffer);
            if (outboundMessage.Type != MapleMessageType.Ack)
                Logger.LogTrace($"Sending message: {Encoding.UTF8.GetString(rawSocketBuffer.AsSpan(start: 0, length: bytesWritten))}");
            await clientSocket.SendAsync(rawSocketBuffer.AsMemory(start: 0, length: bytesWritten), cancellationToken);
        }
    }

    private async Task SocketListenerEntryPoint(DreamcastPort dreamcastPort, CancellationToken cancellationToken)
    {
        using var listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, BasePort + (int)dreamcastPort));
        listener.Listen(backlog: 1);

        while (true) // Accept a new client whenever one disconnects
        {
            Logger.LogDebug("Waiting for client to connect", LogCategories.Maple);
            var clientSocket = await listener.AcceptAsync(cancellationToken);
            Logger.LogDebug("Client connected", LogCategories.Maple);
            lock (_lock)
            {
                _clientSocket = clientSocket;
            }

            var clientCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await waitForClientDisconnectAsync(clientSocket, clientCancellationSource.Token);
            // Ensure all socket tasks are completed or canceled
            clientCancellationSource.Cancel();

            lock (_lock)
            {
                _clientSocket.Close();
                _clientSocket = null;
            }

            OnDisconnect();
        }

        async Task waitForClientDisconnectAsync(Socket clientSocket, CancellationToken cancellationToken)
        {
            try
            {
                await Task.WhenAny(
                    Task.Run(() => SocketReaderEntryPoint(clientSocket, cancellationToken), cancellationToken),
                    Task.Run(() => SocketWriterEntryPoint(clientSocket, cancellationToken), cancellationToken));
            }
            catch (OperationCanceledException)
            {
                // Operation was canceled using token rather than completing (e.g. ReceiveAsync returning 0).
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, LogCategories.Maple);
            }

            Logger.LogDebug("Client disconnected", LogCategories.Maple);
        }
    }

    private void OnDisconnect()
    {
        // discard all messages from the last connection
        while (_inboundCpuMessages.Reader.TryRead(out _)) { }
        while (_outboundMessages.Reader.TryRead(out _)) { }

        // clear the screen
        var additionalWords = new int[50];
        additionalWords[0] = (int)MapleFunction.LCD;
        bool written = _inboundCpuMessages.Writer.TryWrite(new MapleMessage
        {
            Type = MapleMessageType.WriteBlock,
            AdditionalWords = additionalWords,
        });
        Debug.Assert(written);
    }
}