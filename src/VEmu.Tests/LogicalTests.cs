using VEmu.Core;

namespace VEmu.Tests;

public class LogicalTests
{
    [Fact]
    public void AND_Immediate_Example1()
    {
        // VMC-173
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xfa,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xaf,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0x0f,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xf0,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xff;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xfa, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xaa, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x0a, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void AND_Immediate_Example2()
    {
        // VMC-173
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xfe,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xfd,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xfb,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xf7,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xef,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xdf,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xbf,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0x7f,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xff;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xfe, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xfc, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xf8, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xf0, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xe0, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xc0, cpu.SFRs.Acc); // The book says 0xc9, but that doesn't seem to make sense
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x80, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void OR_Direct_Example1()
    {
        // TODO: this one will also be good to return to once MOV is implemented

        // VMC-173
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.OR.Compose(AddressingMode.Direct0), 0x23,
            OpcodePrefix.OR.Compose(AddressingMode.Direct0), 0x23,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x0;
        cpu.RamBank0[0x23] = 0x55;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x55, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        cpu.RamBank0[0x23] = 0xaa;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void ROL_Example1()
    {
        // VMC-173
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            (byte)Opcode.ROL,
            (byte)Opcode.ROL,
            (byte)Opcode.ROL,
            (byte)Opcode.ROL,
            (byte)Opcode.ROL,
            (byte)Opcode.ROL,
            (byte)Opcode.ROL,
            (byte)Opcode.ROL,
            (byte)Opcode.ROL,
            (byte)Opcode.ROL,
            (byte)Opcode.ROL,
            (byte)Opcode.ROL,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x01;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x02, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x04, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x08, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x10, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x20, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x40, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x80, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x01, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        cpu.SFRs.Acc = 0x55;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xaa, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x55, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void ROLC_Example1()
    {
        // VMC-173
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            (byte)Opcode.ROLC,
            (byte)Opcode.ROLC,
            (byte)Opcode.ROLC,
            (byte)Opcode.ROLC,
            (byte)Opcode.ROLC,
            (byte)Opcode.ROLC,
            (byte)Opcode.ROLC,
            (byte)Opcode.ROLC,
            (byte)Opcode.ROLC,
            (byte)Opcode.ROLC,
            (byte)Opcode.ROLC,
            (byte)Opcode.ROLC,
            (byte)Opcode.ROLC,
            (byte)Opcode.ROLC,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 1;
        cpu.SFRs.Cy = true;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b11, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b110, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b1100, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b1_1000, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b11_0000, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b110_0000, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b1100_0000, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b1000_0000, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Cy);

        cpu.SFRs.Acc = 0b0101_0101; // 0x55

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b1010_1011, cpu.SFRs.Acc); // 0xaa
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b0101_0110, cpu.SFRs.Acc); // 0x56
        Assert.True(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b1010_1101, cpu.SFRs.Acc); // 0xad
        Assert.False(cpu.SFRs.Cy);
    }

    [Fact]
    public void ROR_Example1()
    {
        // VMC-184
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            (byte)Opcode.ROR,
            (byte)Opcode.ROR,
            (byte)Opcode.ROR,
            (byte)Opcode.ROR,
            (byte)Opcode.ROR,
            (byte)Opcode.ROR,
            (byte)Opcode.ROR,
            (byte)Opcode.ROR,
            (byte)Opcode.ROR,
            (byte)Opcode.ROR,

            (byte)Opcode.ROR,
            (byte)Opcode.ROR,
            (byte)Opcode.ROR,
            (byte)Opcode.ROR,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x01;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x80, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x40, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x20, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x10, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x08, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x04, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x02, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x01, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        cpu.SFRs.Acc = 0b0101_0001; //0x51;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b1010_1000, cpu.SFRs.Acc); // 0xa8
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b0101_0100, cpu.SFRs.Acc); // 0x54
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b0010_1010, cpu.SFRs.Acc); // 0x2a
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b0001_0101, cpu.SFRs.Acc); // 0x15
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void RORC_Example1()
    {
        // VMC-184
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            (byte)Opcode.RORC,
            (byte)Opcode.RORC,
            (byte)Opcode.RORC,
            (byte)Opcode.RORC,
            (byte)Opcode.RORC,
            (byte)Opcode.RORC,
            (byte)Opcode.RORC,
            (byte)Opcode.RORC,
            (byte)Opcode.RORC,

            (byte)Opcode.RORC,
            (byte)Opcode.RORC,
            (byte)Opcode.RORC,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x01;
        cpu.SFRs.Cy = true;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b1000_0000, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b1100_0000, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b0110_0000, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b0011_0000, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b0001_1000, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b0000_1100, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b0000_0110, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b0000_0011, cpu.SFRs.Acc);
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b0000_0001, cpu.SFRs.Acc);
        Assert.True(cpu.SFRs.Cy);

        cpu.SFRs.Acc = 0b0101_0101; // 0x55

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b1010_1010, cpu.SFRs.Acc); // 0xaa
        Assert.True(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b1101_0101, cpu.SFRs.Acc); // 0xd5
        Assert.False(cpu.SFRs.Cy);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0b0110_1010, cpu.SFRs.Acc); // 0x6a
        Assert.True(cpu.SFRs.Cy);
    }
}