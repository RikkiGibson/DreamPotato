using VEmu.Core;

namespace VEmu.Tests;

public class CpuTests
{
    // TODO: expose state on CPU in order to setup with program data, run some instructions, and verify final state
    [Fact]
    public void ADD_Immediate()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> inst = [0b1000_0001, 42];
        inst.CopyTo(cpu.ROM);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        cpu.Step();

        Assert.Equal(2, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Immediate_SetCarry()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> inst = [0b1000_0001, 200, 0b1000_0001, 200];
        inst.CopyTo(cpu.ROM);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        cpu.Step();

        Assert.Equal(2, cpu.Pc);
        Assert.Equal(200, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(4, cpu.Pc);
        Assert.Equal(144, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Cy);
        Assert.Equal(0b1000_0000, cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Direct_Bank0()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> inst = [0b1000_0010, 0b1000];
        inst.CopyTo(cpu.ROM);

        cpu.RamBank0[0b1000] = 42;

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        cpu.Step();

        Assert.Equal(2, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }
}
