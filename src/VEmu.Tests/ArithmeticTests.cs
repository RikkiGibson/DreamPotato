using VEmu.Core;

namespace VEmu.Tests;

public class ArithmeticTests
{
    [Fact]
    public void ADD_Immediate()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0001, 42];
        instructions.CopyTo(cpu.ROM);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        cpu.Step();

        Assert.Equal(2, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Immediate_Carry()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0001, 200, 0b1000_0001, 200];
        instructions.CopyTo(cpu.ROM);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        cpu.Step();

        Assert.Equal(2, cpu.Pc);
        Assert.Equal(200, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);
        Assert.Equal(0, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(4, cpu.Pc);
        Assert.Equal(144, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Cy);
        Assert.True(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);
        Assert.Equal(0b1100_0000, cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Immediate_Overflow()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0001, unchecked((byte)-127), 0b1000_0001, unchecked((byte)-127)];
        instructions.CopyTo(cpu.ROM);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        cpu.Step();

        Assert.Equal(2, cpu.Pc);
        Assert.Equal(unchecked((byte)-127), cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);
        Assert.Equal(0, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(4, cpu.Pc);
        Assert.Equal(unchecked((byte)-254), cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.True(cpu.SFRs.Ov);
        Assert.Equal(0b1000_0100, cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Direct_Bank0()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0010, 0b1000];
        instructions.CopyTo(cpu.ROM);

        cpu.RamBank0[0b1000] = 42;

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        cpu.Step();

        Assert.Equal(2, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }
}
