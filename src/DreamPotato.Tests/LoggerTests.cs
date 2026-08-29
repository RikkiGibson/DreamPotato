using DreamPotato.Core;

namespace DreamPotato.Tests;

public class LoggerTests
{
    [Fact]
    public void GetLogs_0()
    {
        var cpu = new Cpu();
        var logs = cpu.Logger.GetLogs(2);
        Assert.Single(logs);
    }

    [Fact]
    public void GetLogs_1()
    {
        var cpu = new Cpu();
        cpu.Logger.MinimumLogLevel = LogLevel.Trace;
        cpu.Step();
        var logs = cpu.Logger.GetLogs(2);
        Assert.Equal(2, logs.Count);
        Assert.Contains("""
            Cpu.ROM@[0000]: [Trace] NOP
            """, logs[1]);
    }

    [Fact]
    public void GetLogs_2()
    {
        var cpu = new Cpu();
        var logs = cpu.Logger.GetLogs(20000);
        Assert.Single(logs);
    }
}