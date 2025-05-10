using System.Diagnostics;

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

    // Rolling buffer of log messages.
    private readonly string?[] _messages = new string[1000];
    private int _nextMessageIndex = 0;

    // TODO: InterpolatedStringHandler
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

        if (level == LogLevel.Debug)
            Console.WriteLine(s);

        if (level == LogLevel.Error)
            Console.Error.WriteLine(s);

        var index = _nextMessageIndex % _messages.Length;
        _messages[index] = $"[{_cpu.Pc:X4}]: [{level}] {s}";
        _nextMessageIndex = index + 1;
    }

    public List<string> GetLogs(int recentCount)
    {
        List<string> result = [];
        var startIndex = _nextMessageIndex - recentCount;
        for (int i = 0; i < recentCount; i++)
        {
            var currentIndex = modPositive(startIndex + i, _messages.Length);
            if (_messages[currentIndex] is string message)
                result.Add(message);
        }

        return result;

        static int modPositive(int x, int m)
        {
            Debug.Assert(m > 0);
            return (x % m + m) % m;
        }
    }
}