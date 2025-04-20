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
        // Note that <object> is passed here and many other places to ensure the entire log is printed when the assertion fails
        Assert.Equal<object>("""
            [PC: 0x0] Accessing bank 2, but no bounds checks are implemented

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
            [PC: 0x0] Accessing bank 2, but no bounds checks are implemented
            [PC: 0x1] Accessing bank 2, but no bounds checks are implemented

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
            [PC: 0x0] Accessing nonexistent bank 3

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
            [PC: 0x0] Accessing nonexistent bank 3
            [PC: 0x1] Accessing nonexistent bank 3

            """, cpu.Logger.ToString());
    }

    [Fact]
    public void ADD_Immediate_Example()
    {
        // Based on example in VMC-156
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            0b1000_0001, 0x13, // ADD #013H
            0b1000_0001, 0x0a, // ADD #00AH
            0b1000_0001, 0x0f, // ADD #00FH
            0b1000_0001, 0x80, // ADD #080H
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x55;

        cpu.Step();
        Assert.Equal(0x68, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0x72, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.True(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0x81, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.True(cpu.SFRs.Ac);
        Assert.True(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0x01, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.True(cpu.SFRs.Ov);
    }

    [Fact]
    public void ADD_Direct_Example1()
    {
        // Based on example in VMC-157
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            0b1000_0001, 0x0c, // ADD #00CH
            0b1000_0010, 0x23, // ADD 023H
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x55;
        cpu.RamBank0[0x023] = 0x68;

        cpu.Step();
        Assert.Equal(0x61, cpu.SFRs.Acc);
        Assert.Equal(0x68, cpu.RamBank0[0x023]);
        Assert.False(cpu.SFRs.Cy);
        Assert.True(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0xc9, cpu.SFRs.Acc);
        Assert.Equal(0x68, cpu.RamBank0[0x023]);
        Assert.False(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.True(cpu.SFRs.Ov);
    }

    [Fact]
    public void ADD_Direct_Example2()
    {
        // Based on example in VMC-157
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            0b1000_0001, 0x02, // ADD #002H
            0b1000_0011, 0x02, // ADD B
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x70;
        cpu.SFRs.B = 0x95;

        cpu.Step();
        Assert.Equal(0x72, cpu.SFRs.Acc);
        Assert.Equal(0x95, cpu.SFRs.B);
        Assert.False(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0x07, cpu.SFRs.Acc);
        Assert.Equal(0x95, cpu.SFRs.B);
        Assert.True(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);
    }

    [Fact]
    public void ADD_Indirect_Example1()
    {
        // Based on example in VMC-158
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            0b1000_0001, 0x15, // ADD #015H
            0b1000_0100 // ADD @R0
        ];
        instructions.CopyTo(cpu.ROM);

        // TODO: it would be good to use MOVs instead once it is implemented.
        cpu.SFRs.Acc = 0x55;
        cpu.IndirectAddressRegisters[0] = 0x68;
        cpu.RamBank0[0x68] = 0x10;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x6a, cpu.SFRs.Acc);
        Assert.Equal(0x68, cpu.IndirectAddressRegisters[0]);
        Assert.Equal(0x10, cpu.RamBank0[0x68]);
        Assert.False(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x7a, cpu.SFRs.Acc);
        Assert.Equal(0x68, cpu.IndirectAddressRegisters[0]);
        Assert.Equal(0x10, cpu.RamBank0[0x68]);
        Assert.False(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);
    }

    [Fact]
    public void ADD_Indirect_Example2()
    {
        // Based on example in VMC-158
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            0b1000_0001, 0x01, // ADD #001H
            0b1000_0110 // ADD @R2
        ];
        instructions.CopyTo(cpu.ROM);

        // TODO: it would be good to use MOVs instead once it is implemented.
        cpu.SFRs.Acc = 0xaa;
        cpu.IndirectAddressRegisters[2] = 0x04;
        cpu.RamBank0[0x104] = 0x55;

        cpu.Step();
        Assert.Equal(0xab, cpu.SFRs.Acc);
        Assert.Equal(0x04, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0x55, cpu.RamBank0[0x104]);
        Assert.False(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0x00, cpu.SFRs.Acc);
        Assert.Equal(0x04, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0x55, cpu.RamBank0[0x104]);
        Assert.True(cpu.SFRs.Cy);
        Assert.True(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);
    }

    [Fact]
    public void ADDC_Immediate_Example()
    {
        // Based on example in VMC-159
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.ADD.Compose(AddressingMode.Immediate), 0x13,
            OpcodePrefix.ADDC.Compose(AddressingMode.Immediate), 0x0a,
            OpcodePrefix.ADDC.Compose(AddressingMode.Immediate), 0x0f,
            OpcodePrefix.ADDC.Compose(AddressingMode.Immediate), 0x80,
            OpcodePrefix.ADDC.Compose(AddressingMode.Immediate), 0x01,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x55;

        cpu.Step();
        Assert.Equal(0x68, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0x72, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.True(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0x81, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.True(cpu.SFRs.Ac);
        Assert.True(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0x01, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.True(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0x03, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);
    }

    [Fact]
    public void ADDC_AuxiliaryCarry_01()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.ADD.Compose(AddressingMode.Immediate), 0x08,
            OpcodePrefix.ADDC.Compose(AddressingMode.Immediate), 0x08,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xff;

        cpu.Step();
        Assert.Equal(0x7, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Cy);
        Assert.True(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0x10, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.True(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);
    }

    [Fact]
    public void ADDC_AuxiliaryCarry_02()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.ADD.Compose(AddressingMode.Immediate), 0x10,
            OpcodePrefix.ADDC.Compose(AddressingMode.Immediate), 0x01,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xff;

        cpu.Step();
        Assert.Equal(0x0f, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0x11, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.True(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);
    }

    [Fact]
    public void SUB_Immediate_Example()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.SUB.Compose(AddressingMode.Immediate), 0x13,
            OpcodePrefix.SUB.Compose(AddressingMode.Immediate), 0x03,
            OpcodePrefix.SUB.Compose(AddressingMode.Immediate), 0x3f,
            OpcodePrefix.SUB.Compose(AddressingMode.Immediate), 0x02,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x55;

        cpu.Step();
        Assert.Equal(0x42, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0x3f, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.True(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0xfe, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Cy);
        Assert.True(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);
    }

    [Fact]
    public void SUB_Direct_Example2()
    {
        // VMC-162
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.SUB.Compose(AddressingMode.Immediate), 0x02,
            OpcodePrefix.SUB.Compose(AddressingMode.Direct1), 0x02, // B
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x80;
        cpu.SFRs.B = 0x95;

        cpu.Step();
        Assert.Equal(0x7e, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.True(cpu.SFRs.Ac);
        Assert.True(cpu.SFRs.Ov);

        cpu.Step();
        Assert.Equal(0xe9, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.True(cpu.SFRs.Ov);
    }

    [Fact]
    public void SUBC_Direct_Example2()
    {
        // VMC-162
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.SUB.Compose(AddressingMode.Immediate), 0x02,
            OpcodePrefix.SUBC.Compose(AddressingMode.Direct1), 0x02, // B
            OpcodePrefix.SUBC.Compose(AddressingMode.Direct1), 0x02, // B
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x80;
        cpu.SFRs.B = 0x95;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x7e, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.True(cpu.SFRs.Ac);
        Assert.True(cpu.SFRs.Ov);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xe9, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.True(cpu.SFRs.Ov);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x53, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        // VMC says this should be True, but we are effectively doing (decimal) (-23)-(-107)=83.
        // Doesn't seem like an overflow. Could be a misprint.
        Assert.False(cpu.SFRs.Ov);
    }

    [Fact]
    public void SUBC_Indirect_Example1()
    {
        // VMC-166
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.SUB.Compose(AddressingMode.Immediate), 0x16,
            OpcodePrefix.SUBC.Compose(AddressingMode.Indirect0),
            OpcodePrefix.SUBC.Compose(AddressingMode.Indirect0),
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x55;
        cpu.IndirectAddressRegisters[0] = 0x68;
        cpu.RamBank0[0x68] = 0x40;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x3f, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.True(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xbe, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);
        Assert.False(cpu.SFRs.Ac);
        Assert.False(cpu.SFRs.Ov);
    }

    [Fact]
    public void INC_Direct_Example1()
    {
        // VMC-167
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x0, // ACC
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x0, // ACC
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x0, // ACC
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x0, // ACC
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xfd;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xfe, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x01, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void INC_Direct_Example2()
    {
        // VMC-167
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct0), 0x7f,
            OpcodePrefix.INC.Compose(AddressingMode.Direct0), 0x7f,
            OpcodePrefix.INC.Compose(AddressingMode.Direct0), 0x7f,
            OpcodePrefix.INC.Compose(AddressingMode.Direct0), 0x7f,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.RamBank0[0x7f] = 0xfd;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xfe, cpu.RamBank0[0x7f]);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xff, cpu.RamBank0[0x7f]);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.RamBank0[0x7f]);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x01, cpu.RamBank0[0x7f]);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    // TODO: test inc indirect

    // TODO: test an edge case for Ov with SUBC, when the underflow only occurs due to the carry
}
