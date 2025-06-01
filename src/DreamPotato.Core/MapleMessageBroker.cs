using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

using DreamPotato.Core;

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
    private readonly List<byte> _currentMessageBuilder = [];

    private readonly Lock _inboundOutboundMessagesLock = new Lock();
    private readonly List<MapleMessage> _receivedMessages = [];
    private readonly List<MapleMessage> _outboundMessages = [];

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

    private void SubmitMessage()
    {
        var message = new MapleMessage();
        message.HasValue = true;
        message.RawBytes = _currentMessageBuilder.ToArray();
        _currentMessageBuilder.Clear();

        lock (_inboundOutboundMessagesLock)
        {
            _receivedMessages.Add(message);
        }
    }

    internal MapleMessage TryReceiveMessage()
    {
        lock (_inboundOutboundMessagesLock)
        {
            if (_receivedMessages.Count == 0)
                return default;

            var message = _receivedMessages[0];
            _receivedMessages.RemoveAt(0);
            return message;
        }
    }

    /// <returns>false if the message was malformed.</returns>
    internal bool AppendMessageFragment(ReadOnlySpan<byte> fragment)
    {
        var stringValue = Encoding.UTF8.GetString(fragment);

        // TODO: do this without allocating.
        try
        {
            for (int i = 0; i < stringValue.Length;)
            {
                if (stringValue.Substring(i, 2).Equals("\r\n"))
                {
                    SubmitMessage();
                    i += 2;
                    continue;
                }

                if (stringValue[i] == ' ')
                    i++;

                _currentMessageBuilder.Add(Convert.ToByte(stringValue.Substring(i, 2)));
                i += 2;
            }
        }
        catch (FormatException)
        {
            // TODO: normal cpu Logger is not thread safe.
            Console.Error.WriteLine($"Bad format: '{stringValue}'", LogCategories.Maple);
            return false;
        }
        catch (IndexOutOfRangeException)
        {
            Console.Error.WriteLine($"Bad format: '{stringValue}'", LogCategories.Maple);
            return false;
        }

        return true;
    }

    private async Task StartServerWorker(CancellationToken cancellationToken)
    {
        using var listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, BasePort));
        listener.Listen(backlog: 1);

        var handler = await listener.AcceptAsync(cancellationToken);
        while (handler.Connected)
        {
            // Receive message.
            var receivedLen = await handler.ReceiveAsync(_rawSocketBuffer, SocketFlags.None, cancellationToken);
            AppendMessageFragment(_rawSocketBuffer.AsSpan()[0..receivedLen]);

            foreach (var message in _outboundMessages)
            {
                await handler.SendAsync(new byte[0], SocketFlags.None, cancellationToken);
            }
        }
    }
}