using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace VEmu.Core;

public enum LogLevel
{
    Trace,
    Debug,
    Warning,
    Error,
}

public class Logger(LogLevel _minimumLogLevel, Cpu _cpu)
{
    private readonly LogLevel _minimumLogLevel = _minimumLogLevel;
    private readonly Cpu _cpu = _cpu;

    // Rolling buffer of log messages.
    private readonly string?[] _messages = new string[1000];
    private int _nextMessageIndex = 0;

    public void LogTrace(string s)
        => LogCore(LogLevel.Trace, $"{s}");

    public void LogTrace(DefaultInterpolatedStringHandler handler)
        => LogCore(LogLevel.Trace, handler);

    public void LogDebug(string s)
        => LogCore(LogLevel.Debug, $"{s}");

    public void LogDebug(DefaultInterpolatedStringHandler handler)
        => LogCore(LogLevel.Debug, handler);

    public void LogWarning(string s)
        => LogCore(LogLevel.Warning, $"{s}");

    public void LogWarning(DefaultInterpolatedStringHandler handler)
        => LogCore(LogLevel.Warning, handler);

    public void LogError(string s)
        => LogCore(LogLevel.Error, $"{s}");

    public void LogError(DefaultInterpolatedStringHandler handler)
        => LogCore(LogLevel.Error, handler);

    // TODO: do we need ISpanFormattable impl to avoid work on Instruction.ToString() etc?
    private void LogCore(LogLevel level, DefaultInterpolatedStringHandler handler)
    {
        if (level < _minimumLogLevel)
            return;

        string message = $"{_cpu.InstructionBank}@[{_cpu.Pc:X4}]: [{level}] {handler.ToStringAndClear()}";
        if (level == LogLevel.Debug)
            Console.WriteLine(message);

        if (level == LogLevel.Error)
            Console.Error.WriteLine(message);

        var index = _nextMessageIndex % _messages.Length;
        _messages[index] = message;
        _nextMessageIndex = index + 1;
    }

    // TODO: this could be some enumerable struct instead
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