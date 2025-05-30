using DreamPotato.Core;

namespace DreamPotato.Tests;

public class LoggerTests
{
    [Fact]
    public void GetLogs_0()
    {
        var cpu = new Cpu();
        var logs = cpu.Logger.GetLogs(2);
        Assert.Empty(logs);
    }

    [Fact]
    public void GetLogs_1()
    {
        var cpu = new Cpu();
        cpu.Step();
        var logs = cpu.Logger.GetLogs(2);
        Assert.Single(logs);
    }

    [Fact]
    public void GetLogs_2()
    {
        var cpu = new Cpu();
        var logs = cpu.Logger.GetLogs(20000);
        Assert.Empty(logs);
    }
}