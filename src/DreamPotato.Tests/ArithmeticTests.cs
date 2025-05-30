using DreamPotato.Core;

namespace DreamPotato.Tests;

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
        Assert.Equal(0, (byte)cpu.SFRs.Psw);
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
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(4, cpu.Pc);
        Assert.Equal(144, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);
        Assert.Equal(0b1100_0000, (byte)cpu.SFRs.Psw);
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
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(4, cpu.Pc);
        Assert.Equal(unchecked((byte)-254), cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.True(cpu.SFRs.Psw.Ov);
        Assert.Equal(0b1000_0100, (byte)cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Direct_Bank0()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0010, 0b1000];
        instructions.CopyTo(cpu.ROM);

        cpu.Memory.Write(0b1000, 42);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);

        cpu.Step();
        Assert.Equal(2, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Direct_Bank1()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0010, 0b1000];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Psw = new() { Rambk0 = true };
        cpu.Memory.Write(0b1000, 42);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0b10, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(2, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0b10, (byte)cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R0_Bank0()
    {
        // Reads 0x00-0xff of bank 0
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0100];
        instructions.CopyTo(cpu.ROM);

        cpu.Memory.Write(0, 0b1000);
        cpu.Memory.Write(0b1000, 42);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R1_Bank0()
    {
        // Reads 0x00-0xff of bank 0
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0101];
        instructions.CopyTo(cpu.ROM);

        cpu.Memory.Write(1, 0b1000);
        cpu.Memory.Write(0b1000, 42);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R2_Bank0()
    {
        // Reads 0x100-0x1ff of bank 0 (i.e. SFRs and XRAM)
        // XRAM (easier to write since it won't stomp on SFRs such as Psw) starts at 0x180
        // Therefore an interesting address to read from would be 0x184
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0110];
        instructions.CopyTo(cpu.ROM);

        cpu.Memory.Write(2, 0x84);
        cpu.Memory.Write(0x184, 42);
        Assert.Equal(42, cpu.Memory.Read(0x184));

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R3_Bank0()
    {
        // Reads 0x100-0x1ff of bank 0 (i.e. SFRs and XRAM)
        // XRAM (easier to write since it won't stomp on SFRs such as Psw) starts at 0x180
        // Therefore an interesting address to read from would be 0x184
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [OpcodeMask.ADD | AddressModeMask.Indirect3];
        instructions.CopyTo(cpu.ROM);

        cpu.Memory.Write(3, 0x84);
        cpu.Memory.Write(0x184, 42);
        Assert.Equal(42, cpu.Memory.Read(0x184));

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R0_Bank1()
    {
        // Reads 0x0-0xff of bank 1 (user/application ram)
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0100, 0b1000_0100];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Psw = new() { Irbk0 = true };
        cpu.Memory.Write(4, 0b1000);
        cpu.Memory.Write(0b1000, 42);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0b1000, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0b1000, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(2, cpu.Pc);
        Assert.Equal(84, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.Equal(0b1001000, (byte)cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R1_Bank1()
    {
        // Reads 0x0-0xff of bank 1 (user/application ram)
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0101, 0b1000_0101];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Psw = new() { Irbk0 = true };
        cpu.Memory.Write(5, 0b1000);
        cpu.Memory.Write(0b1000, 42);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0b1000, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0b1000, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(2, cpu.Pc);
        Assert.Equal(84, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.Equal(0b1001000, (byte)cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R2_Bank1()
    {
        // Reads 0x100-0x1ff of bank 1 (lower half xram being in 0x180-1xff)
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [0b1000_0110, 0b1000_0110];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Psw = new() { Irbk0 = true };
        cpu.Memory.Write(6, 0x84);
        cpu.Memory.Write(0x184, 42);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0b1000, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0b1000, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(2, cpu.Pc);
        Assert.Equal(84, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.Equal(0b1001000, (byte)cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R0_Bank2()
    {
        // R0 in Bank2 means use address 8
        var cpu = new Cpu() { };
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.ADD | AddressModeMask.Indirect0,
            OpcodeMask.ADD | AddressModeMask.Indirect0
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Psw = new() { Irbk1 = true };
        cpu.Memory.Write(8, 0xf);
        cpu.Memory.Write(0xf, 42);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0b1_0000, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0b1_0000, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(2, cpu.Pc);
        Assert.Equal(84, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.Equal(0b101_0000, (byte)cpu.SFRs.Psw);
    }

    [Fact]
    public void ADD_Indirect_R0_Bank3()
    {
        // R0 in Bank3 means use address 12
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.ADD | AddressModeMask.Indirect0,
            OpcodeMask.ADD | AddressModeMask.Indirect0
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Psw = new() { Irbk0 = true, Irbk1 = true };
        cpu.Memory.Write(12, 0xf);
        cpu.Memory.Write(0xf, 42);

        Assert.Equal(0, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0b1_1000, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(1, cpu.Pc);
        Assert.Equal(42, cpu.SFRs.Acc);
        Assert.Equal(0b1_1000, (byte)cpu.SFRs.Psw);

        cpu.Step();
        Assert.Equal(2, cpu.Pc);
        Assert.Equal(84, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.Equal(0b101_1000, (byte)cpu.SFRs.Psw);
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
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0x72, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0x81, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.True(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0x01, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.True(cpu.SFRs.Psw.Ov);
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
        cpu.Memory.Write(0x023, 0x68);

        cpu.Step();
        Assert.Equal(0x61, cpu.SFRs.Acc);
        Assert.Equal(0x68, cpu.Memory.Read(0x023));
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0xc9, cpu.SFRs.Acc);
        Assert.Equal(0x68, cpu.Memory.Read(0x023));
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.True(cpu.SFRs.Psw.Ov);
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
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0x07, cpu.SFRs.Acc);
        Assert.Equal(0x95, cpu.SFRs.B);
        Assert.True(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);
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

        cpu.SFRs.Acc = 0x55;
        cpu.Memory.Write(0, 0x68);
        cpu.Memory.Write(0x68, 0x10);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x6a, cpu.SFRs.Acc);
        Assert.Equal(0x68, cpu.Memory.Read(0));
        Assert.Equal(0x10, cpu.Memory.Read(0x68));
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x7a, cpu.SFRs.Acc);
        Assert.Equal(0x68, cpu.Memory.Read(0));
        Assert.Equal(0x10, cpu.Memory.Read(0x68));
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);
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

        cpu.SFRs.Acc = 0xaa;
        cpu.Memory.Write(2, 0x04);
        cpu.Memory.Write(0x104, 0x55);

        cpu.Step();
        Assert.Equal(0xab, cpu.SFRs.Acc);
        Assert.Equal(0x04, cpu.Memory.Read(2));
        Assert.Equal(0x55, cpu.Memory.Read(0x104));
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0x00, cpu.SFRs.Acc);
        Assert.Equal(0x04, cpu.Memory.Read(2));
        Assert.Equal(0x55, cpu.Memory.Read(0x104));
        Assert.True(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);
    }

    [Fact]
    public void ADDC_Immediate_Example()
    {
        // Based on example in VMC-159
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.ADD | AddressModeMask.Immediate, 0x13,
            OpcodeMask.ADDC | AddressModeMask.Immediate, 0x0a,
            OpcodeMask.ADDC | AddressModeMask.Immediate, 0x0f,
            OpcodeMask.ADDC | AddressModeMask.Immediate, 0x80,
            OpcodeMask.ADDC | AddressModeMask.Immediate, 0x01,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x55;

        cpu.Step();
        Assert.Equal(0x68, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0x72, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0x81, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.True(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0x01, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.True(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0x03, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);
    }

    [Fact]
    public void ADDC_AuxiliaryCarry_01()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.ADD | AddressModeMask.Immediate, 0x08,
            OpcodeMask.ADDC | AddressModeMask.Immediate, 0x08,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xff;

        cpu.Step();
        Assert.Equal(0x7, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0x10, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);
    }

    [Fact]
    public void ADDC_AuxiliaryCarry_02()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.ADD | AddressModeMask.Immediate, 0x10,
            OpcodeMask.ADDC | AddressModeMask.Immediate, 0x01,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xff;

        cpu.Step();
        Assert.Equal(0x0f, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0x11, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);
    }

    [Fact]
    public void SUB_Immediate_Example()
    {
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.SUB | AddressModeMask.Immediate, 0x13,
            OpcodeMask.SUB | AddressModeMask.Immediate, 0x03,
            OpcodeMask.SUB | AddressModeMask.Immediate, 0x3f,
            OpcodeMask.SUB | AddressModeMask.Immediate, 0x02,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x55;

        cpu.Step();
        Assert.Equal(0x42, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0x3f, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0xfe, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);
    }

    [Fact]
    public void SUB_Direct_Example2()
    {
        // VMC-162
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.SUB | AddressModeMask.Immediate, 0x02,
            OpcodeMask.SUB | AddressModeMask.Direct1, 0x02, // B
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x80;
        cpu.SFRs.B = 0x95;

        cpu.Step();
        Assert.Equal(0x7e, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.True(cpu.SFRs.Psw.Ov);

        cpu.Step();
        Assert.Equal(0xe9, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.True(cpu.SFRs.Psw.Ov);
    }

    [Fact]
    public void SUBC_Direct_Example2()
    {
        // VMC-162
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.SUB | AddressModeMask.Immediate, 0x02,
            OpcodeMask.SUBC | AddressModeMask.Direct1, 0x02, // B
            OpcodeMask.SUBC | AddressModeMask.Direct1, 0x02, // B
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x80;
        cpu.SFRs.B = 0x95;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x7e, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.True(cpu.SFRs.Psw.Ov);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xe9, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.True(cpu.SFRs.Psw.Ov);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x53, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        // VMC says this should be True, but we are effectively doing (decimal) (-23)-(-107)=83.
        // Doesn't seem like an overflow. Could be a misprint.
        Assert.False(cpu.SFRs.Psw.Ov);
    }

    [Fact]
    public void SUBC_Indirect_Example1()
    {
        // VMC-166
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.SUB | AddressModeMask.Immediate, 0x16,
            OpcodeMask.SUBC | AddressModeMask.Indirect0,
            OpcodeMask.SUBC | AddressModeMask.Indirect0,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x55;
        cpu.Memory.Write(0, 0x68);
        cpu.Memory.Write(0x68, 0x40);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x3f, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xbe, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.False(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);
    }

    [Fact]
    public void INC_Direct_Example1()
    {
        // VMC-167
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.INC | AddressModeMask.Direct1, 0x0, // ACC
            OpcodeMask.INC | AddressModeMask.Direct1, 0x0, // ACC
            OpcodeMask.INC | AddressModeMask.Direct1, 0x0, // ACC
            OpcodeMask.INC | AddressModeMask.Direct1, 0x0, // ACC
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xfd;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xfe, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x01, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);
    }

    [Fact]
    public void INC_Direct_Example2()
    {
        // VMC-167
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.INC | AddressModeMask.Direct0, 0x7f,
            OpcodeMask.INC | AddressModeMask.Direct0, 0x7f,
            OpcodeMask.INC | AddressModeMask.Direct0, 0x7f,
            OpcodeMask.INC | AddressModeMask.Direct0, 0x7f,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.Memory.Write(0x7f, 0xfd);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xfe, cpu.Memory.Read(0x7f));
        Assert.Equal(0, (byte)cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xff, cpu.Memory.Read(0x7f));
        Assert.Equal(0, (byte)cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.Memory.Read(0x7f));
        Assert.Equal(0, (byte)cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x01, cpu.Memory.Read(0x7f));
        Assert.Equal(0, (byte)cpu.SFRs.Psw);
    }

    [Fact]
    public void DEC_Indirect_Example1()
    {
        // VMC-170
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.DEC | AddressModeMask.Indirect2, // DEC @R2
            OpcodeMask.DEC | AddressModeMask.Indirect2,
            OpcodeMask.DEC | AddressModeMask.Indirect2,
            OpcodeMask.DEC | AddressModeMask.Indirect2,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.Memory.Write(2, 0); // @R2 addresses 0x100-0x1ff range, so, this refers to ACC (0x100).
        cpu.Memory.Write(0x100, 2);
        Assert.Equal(2, cpu.SFRs.Acc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(255, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(254, cpu.SFRs.Acc);
        Assert.Equal(0, (byte)cpu.SFRs.Psw);
    }

    [Fact]
    public void MUL_Example1()
    {
        // VMC-171
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.MUL
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Psw = new() { Cy = true, Ac = true, Ov = true };

        // 0x1123 * 0x52 = 0x057D36
        cpu.SFRs.Acc = 0x11;
        cpu.SFRs.C = 0x23;
        cpu.SFRs.B = 0x52;

        Assert.Equal(7, cpu.Step());
        Assert.Equal(0x05, cpu.SFRs.B);
        Assert.Equal(0x7D, cpu.SFRs.Acc);
        Assert.Equal(0x36, cpu.SFRs.C);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.True(cpu.SFRs.Psw.Ov);
    }

    [Fact]
    public void MUL_Example2()
    {
        // VMC-171
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.MUL
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Psw = new() { Cy = true, Ac = true, Ov = true };

        // 0x0705 * 0x10 = 0x007050
        cpu.SFRs.Acc = 0x07;
        cpu.SFRs.C = 0x05;
        cpu.SFRs.B = 0x10;

        Assert.Equal(7, cpu.Step());
        Assert.Equal(0x00, cpu.SFRs.B);
        Assert.Equal(0x70, cpu.SFRs.Acc);
        Assert.Equal(0x50, cpu.SFRs.C);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);
    }

    [Fact]
    public void DIV_Example1()
    {
        // VMC-171
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.DIV
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Psw = new() { Cy = true, Ac = true, Ov = true };

        // 0x0705 * 0x10 = 0x007050
        cpu.SFRs.Acc = 0x79;
        cpu.SFRs.C = 0x05;
        cpu.SFRs.B = 0x07;

        Assert.Equal(7, cpu.Step());
        Assert.Equal(0x06, cpu.SFRs.B);
        Assert.Equal(0x11, cpu.SFRs.Acc);
        Assert.Equal(0x49, cpu.SFRs.C);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.False(cpu.SFRs.Psw.Ov);
    }

    [Fact]
    public void DIV_Example2()
    {
        // VMC-171
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.DIV
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Psw = new() { Cy = true, Ac = true, Ov = true };

        // 0x0705 * 0x10 = 0x007050
        cpu.SFRs.Acc = 0x07;
        cpu.SFRs.C = 0x10;
        cpu.SFRs.B = 0x00;

        Assert.Equal(7, cpu.Step());
        Assert.Equal(0x00, cpu.SFRs.B);
        Assert.Equal(0xFF, cpu.SFRs.Acc);
        Assert.Equal(0x10, cpu.SFRs.C);
        Assert.False(cpu.SFRs.Psw.Cy);
        Assert.True(cpu.SFRs.Psw.Ac);
        Assert.True(cpu.SFRs.Psw.Ov);
    }

    // TODO: test inc indirect

    // TODO: test an edge case for Ov with SUBC, when the underflow only occurs due to the carry
}
