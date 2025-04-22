using VEmu.Core;

namespace VEmu.Tests;

public class JumpTests
{
    [Fact]
    public void JMP_Example1()
    {
        // VMC-197
        var cpu = new Cpu();

        // starting at 0x0FFB
        scoped ReadOnlySpan<byte> instructions = [
            (byte)Opcode.NOP,
            (byte)Opcode.NOP,
            0b0011_1111, 0b0000_1110, // JMP to 0xF0E (0b001a_1aaa aaaa_aaaa)
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xFFB));

        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0, // acc
            (byte)Opcode.ROR,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xF0E));

        cpu.SFRs.Acc = 0;
        cpu.Pc = 0xFFB;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xFFD, cpu.Pc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xF0E, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x80, cpu.SFRs.Acc);
    }

    [Fact]
    public void JMP_Example2()
    {
        // VMC-197
        var cpu = new Cpu();

        // starting at 0x0FFC
        scoped ReadOnlySpan<byte> instructions = [
            (byte)Opcode.NOP,
            (byte)Opcode.NOP,
            0b0011_1111, 0b0000_1110, // JMP to 0xF0E (0b001a_1aaa aaaa_aaaa). Encoding: 0x3F0E
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0x0FFC));

        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0, // acc
            (byte)Opcode.ROR,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0x1F0E));

        cpu.SFRs.Acc = 0;
        cpu.Pc = 0xFFC;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x0FFE, cpu.Pc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x1F0E, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x80, cpu.SFRs.Acc);
    }
    [Fact]
    public void JMPF_Example1()
    {
        // VMC-198
        var cpu = new Cpu();

        // starting at 0x0FFA
        scoped ReadOnlySpan<byte> instructions = [
            (byte)Opcode.NOP,
            (byte)Opcode.NOP,
            (byte)Opcode.JMPF, 0x0f, 0x0e, // JMPF to 0x0F0E
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xFFA));

        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0, // acc
            (byte)Opcode.ROR,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xF0E));

        cpu.SFRs.Acc = 0;
        cpu.Pc = 0xFFA;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xFFC, cpu.Pc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xF0E, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x80, cpu.SFRs.Acc);
    }

    [Fact]
    public void JMPF_Example2()
    {
        // VMC-198
        var cpu = new Cpu();

        // starting at 0x0FFC
        scoped ReadOnlySpan<byte> instructions = [
            (byte)Opcode.NOP,
            (byte)Opcode.NOP,
            (byte)Opcode.JMPF, 0x0f, 0x0e,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0x0FFC));

        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0, // acc
            (byte)Opcode.ROR,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xF0E));

        cpu.SFRs.Acc = 0;
        cpu.Pc = 0xFFC;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x0FFE, cpu.Pc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x0F0E, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x80, cpu.SFRs.Acc);
    }

    [Fact]
    public void BR_Example1()
    {
        // VMC-199
        var cpu = new Cpu();

        // starting at 0x0F1C
        scoped ReadOnlySpan<byte> instructions = [
            (byte)Opcode.NOP,
            (byte)Opcode.NOP,
            (byte)Opcode.BR, 0x3f,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0x0F1C));

        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0, // acc
            (byte)Opcode.ROR,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0x0F5F));

        cpu.SFRs.Acc = 0;
        cpu.Pc = 0x0F1C;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x0F1E, cpu.Pc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x0F5F, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x80, cpu.SFRs.Acc);
    }

    [Fact]
    public void BRF_Example2()
    {
        // VMC-200
        var cpu = new Cpu();

        // The example was a little unclear, but, I think the NOPs are supposed to
        // directly precede the INC, because otherwise it is unspecified how to get from the NOPs to the first INC.

        ReadOnlySpan<byte> instructions = [
            (byte)Opcode.NOP,
            (byte)Opcode.NOP,
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x00, // acc,
            (byte)Opcode.ROR,
            (byte)Opcode.NOP,
            (byte)Opcode.NOP,
            (byte)Opcode.BRF, 0xf8, 0xff,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0x1F0C));

        cpu.SFRs.Acc = 0;
        cpu.Pc = 0x1F0C;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x1F0E, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x80, cpu.SFRs.Acc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x1F13, cpu.Pc);

        // brf backwards
        Assert.Equal(4, cpu.Step());
        Assert.Equal(0x1F0E, cpu.Pc); // TODO: Pc is off by one here. Need to test Example1 as well.

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x81, cpu.SFRs.Acc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xB0, cpu.SFRs.Acc);
    }
}