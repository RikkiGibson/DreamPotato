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

    [Fact]
    public void ADD_Direct_Bank1()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0010, 0b1000];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Rambk0 = true;
        cpu.RamBank1[0b1000] = 42;

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0b10, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(2, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0b10, cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R0_Bank0()
    {
        // Reads 0x00-0xff of bank 0
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0100];
        instructions.CopyTo(cpu.ROM);

        cpu.IndirectAddressRegisters[0] = 0b1000;
        cpu.RamBank0[0b1000] = 42;

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R1_Bank0()
    {
        // Reads 0x00-0xff of bank 0
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0101];
        instructions.CopyTo(cpu.ROM);

        cpu.IndirectAddressRegisters[1] = 0b1000;
        cpu.RamBank0[0b1000] = 42;

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R2_Bank0()
    {
        // Reads 0x100-0x1ff of bank 0 (i.e. SFRs and XRAM)
        // XRAM (easier to write since it won't stomp on SFRs such as Psw) starts at 0x180
        // Therefore an interesting address to read from would be 0x18f
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0110];
        instructions.CopyTo(cpu.ROM);

        cpu.IndirectAddressRegisters[2] = 0x8f;
        cpu.XRam_0[0xf] = 42;
        Assert.Equal(42, cpu.RamBank0[0x18f]);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R3_Bank0()
    {
        // Reads 0x100-0x1ff of bank 0 (i.e. SFRs and XRAM)
        // XRAM (easier to write since it won't stomp on SFRs such as Psw) starts at 0x180
        // Therefore an interesting address to read from would be 0x18f
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0111];
        instructions.CopyTo(cpu.ROM);

        cpu.IndirectAddressRegisters[3] = 0x8f;
        cpu.XRam_0[0xf] = 42;
        Assert.Equal(42, cpu.RamBank0[0x18f]);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R0_Bank1()
    {
        // Reads 0x0-0xff of bank 1 (user/application ram)
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0100, 0b1000_0100];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Irbk0 = true;
        cpu.IndirectAddressRegisters[4] = 0b1000;
        cpu.RamBank1[0b1000] = 42;

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0b1000, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0b1000, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(2, cpu.Pc);
        Assert.Equal(84, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Ac);
        Assert.Equal(0b1001000, cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R1_Bank1()
    {
        // Reads 0x0-0xff of bank 1 (user/application ram)
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0101, 0b1000_0101];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Irbk0 = true;
        cpu.IndirectAddressRegisters[5] = 0b1000;
        cpu.RamBank1[0b1000] = 42;

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0b1000, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0b1000, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(2, cpu.Pc);
        Assert.Equal(84, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Ac);
        Assert.Equal(0b1001000, cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R2_Bank1()
    {
        // Reads 0x100-0x1ff of bank 1 (lower half xram being in 0x180-1xff)
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0110, 0b1000_0110];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Irbk0 = true;
        cpu.IndirectAddressRegisters[6] = 0x8f;
        cpu.XRam_1[0xf] = 42;

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0b1000, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0b1000, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(2, cpu.Pc);
        Assert.Equal(84, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Ac);
        Assert.Equal(0b1001000, cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R0_Bank2()
    {
        // Reads from bank 2 are allowed for now but we need to bounds check.
        var cpu = new Cpu() { Logger = new StringWriter() };
        ReadOnlySpan<byte> instructions = [0b1000_0100, 0b1000_0100];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Irbk1 = true;
        cpu.IndirectAddressRegisters[8] = 0xf;
        cpu.RamBank2[0xf] = 42;

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0b1_0000, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal<object>("""
            Accessing bank 2, but no bounds checks are implemented

            """, cpu.Logger.ToString());
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0b1_0000, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(2, cpu.Pc);
        Assert.Equal(84, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Ac);
        Assert.Equal(0b101_0000, cpu.SFRs.Psw);
        Assert.Equal<object>("""
            Accessing bank 2, but no bounds checks are implemented
            Accessing bank 2, but no bounds checks are implemented

            """, cpu.Logger.ToString());
    }

    [Fact]
    public void ADD_Indirect_R0_Bank3()
    {
        // AFIAK, there is no bank 3, so there's no reason to ever use these registers. All reads will return 0(?).
        var cpu = new Cpu() { Logger = new StringWriter() };
        ReadOnlySpan<byte> instructions = [0b1000_0100, 0b1000_0100];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Irbk0 = true;
        cpu.SFRs.Irbk1 = true;
        cpu.IndirectAddressRegisters[12] = 0xf;
        cpu.RamBank2[0xf] = 42;

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0b1_1000, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal<object>("""
            Accessing nonexistent bank 3

            """, cpu.Logger.ToString());
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0b1_1000, cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(2, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Ac);
        Assert.Equal(0b1_1000, cpu.SFRs.Psw);
        Assert.Equal<object>("""
            Accessing nonexistent bank 3
            Accessing nonexistent bank 3

            """, cpu.Logger.ToString());
    }
}
