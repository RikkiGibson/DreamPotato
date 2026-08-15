using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DreamPotato.Core;

public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,

#if DEBUG
    Default = Debug,
#else
    Default = Info,
#endif
}

public static class LogLevelExtensions
{
    public static string[] Names { get; } = ["Trace", "Debug", "Info", "Warning", "Error"];
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
    SerialTransfer = 1 << 8,

    Default = General | SerialTransfer | Instructions | Audio,
}

public class Logger(Cpu? cpu = null)
{
    private readonly Cpu? _cpu = cpu;

    public LogCategories Categories { get; set; } = LogCategories.Default;
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Default;

    public StreamWriter? FileWriter { get; set { field?.Dispose(); field = value; } }

    // Rolling buffer of log messages.
    private readonly string?[] _messages = new string[1000];
    private int _nextMessageIndex = 0;

    // TODO: CallerFilePath, CallerLineNumber
    [Conditional("DEBUG")]
    public void LogTrace(string s, LogCategories category = LogCategories.General)
        => LogCore(LogLevel.Trace, $"{s}", category);

    [Conditional("DEBUG")]
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
        if (level < MinimumLogLevel)
            return;

        // Do write errors even if we didn't subscribe to the category
        if (level < LogLevel.Error && (Categories & category) == 0)
            return;

        var timestamp = DateTimeOffset.Now;
        var cpuDescription = _cpu is null ? $"" : (DefaultInterpolatedStringHandler)$" {_cpu.DisplayName}.{_cpu.CurrentInstructionBankId}@[{_cpu.Pc:X4}]";
        string message = $"{timestamp.TimeOfDay}{cpuDescription.ToStringAndClear()}: [{level}] {handler.ToStringAndClear()}";

        // Trace logs are too noisy for console even when explicitly enabled
        if (level > LogLevel.Trace)
            Console.WriteLine(message);

        if (level == LogLevel.Error)
            Console.Error.WriteLine(message);

        FileWriter?.WriteLine(message);

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
            var currentIndex = BitHelpers.ModPositive(startIndex + i, _messages.Length);
            if (_messages[currentIndex] is string message)
                result.Add(message);
        }

        return result;
    }
}