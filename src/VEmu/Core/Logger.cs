namespace VEmu.Core;

enum LogLevel
{
    Trace,
    Debug,
    Error,
}

class Logger(LogLevel _minimumLogLevel, Cpu _cpu)
{
    private readonly LogLevel _minimumLogLevel = _minimumLogLevel;
    private readonly Cpu _cpu = _cpu;

    private readonly string?[] _messages = new string[1000];
    private int _messageCount = 0;

    public void LogTrace(string s)
        => LogCore(LogLevel.Trace, s);

    public void LogDebug(string s)
        => LogCore(LogLevel.Debug, s);

    public void LogError(string s)
        => LogCore(LogLevel.Error, s);

    private void LogCore(LogLevel level, string s)
    {
        if (level < _minimumLogLevel)
            return;

        var index = _messageCount % _messages.Length;
        _messages[index] = $"[{_cpu.Pc:X4}]: [{level}] {s}";
        _messageCount = index + 1;
    }

    public string?[] GetLogs(int recentCount)
    {
        var result = new string?[recentCount];
        var messageIndex = (_messageCount - recentCount) % _messages.Length;
        for (int i = 0; i < recentCount; i++, messageIndex++)
        {
            result[i] = _messages[messageIndex];
        }

        return result;
    }
}