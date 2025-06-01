using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DreamPotato.Core;

public enum LogLevel
{
    Trace,
    Debug,
    Warning,
    Error,
}

public enum LogCategories
{
    None = 0,
    General = 1 << 0,
    Instructions = 1 << 1,
    Interrupts = 1 << 2,
    Timers = 1 << 3,
    Halt = 1 << 4,
    SystemClock = 1 << 5,
    Audio = 1 << 6,
    Maple = 1 << 7,
}

public class Logger(LogLevel _minimumLogLevel, LogCategories _categories, Cpu _cpu)
{
    private readonly LogLevel _minimumLogLevel = _minimumLogLevel;
    private readonly LogCategories _categories = _categories;
    private readonly Cpu _cpu = _cpu;

    // Rolling buffer of log messages.
    private readonly string?[] _messages = new string[1000];
    private int _nextMessageIndex = 0;

    // TODO: CallerFilePath, CallerLineNumber
    public void LogTrace(string s, LogCategories category = LogCategories.General)
        => LogCore(LogLevel.Trace, $"{s}", category);

    public void LogTrace(DefaultInterpolatedStringHandler handler, LogCategories category = LogCategories.General)
        => LogCore(LogLevel.Trace, handler, category);

    public void LogDebug(string s, LogCategories category = LogCategories.General)
        => LogCore(LogLevel.Debug, $"{s}", category);

    public void LogDebug(DefaultInterpolatedStringHandler handler, LogCategories category = LogCategories.General)
        => LogCore(LogLevel.Debug, handler, category);

    public void LogWarning(string s, LogCategories category = LogCategories.General)
        => LogCore(LogLevel.Warning, $"{s}", category);

    public void LogWarning(DefaultInterpolatedStringHandler handler, LogCategories category = LogCategories.General)
        => LogCore(LogLevel.Warning, handler, category);

    public void LogError(string s, LogCategories category = LogCategories.General)
        => LogCore(LogLevel.Error, $"{s}", category);

    public void LogError(DefaultInterpolatedStringHandler handler, LogCategories category = LogCategories.General)
        => LogCore(LogLevel.Error, handler, category);

    // TODO: do we need ISpanFormattable impl to avoid work on Instruction.ToString() etc?
    private void LogCore(LogLevel level, DefaultInterpolatedStringHandler handler, LogCategories category)
    {
        if (level < _minimumLogLevel)
            return;

        // Do write errors even if we didn't subscribe to the category
        if (level < LogLevel.Error && (_categories & category) == 0)
            return;

        var timestamp = DateTimeOffset.Now;
        string message = $"{timestamp.TimeOfDay} {_cpu.InstructionBank}@[{_cpu.Pc:X4}]: [{level}] {handler.ToStringAndClear()}";
        if (level is LogLevel.Debug or LogLevel.Warning)
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