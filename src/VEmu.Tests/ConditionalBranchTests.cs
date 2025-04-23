using VEmu.Core;

namespace VEmu.Tests;

public class ConditionalBranchTests
{
    [Fact]
    public void BZ_Example1()
    {
        // VMC-201
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x00, 0x00, // acc
            (byte)Opcode.BZ, 0x3f,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf1b));

        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x00, // acc
            (byte)Opcode.ROR,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf5f));

        cpu.SFRs.Acc = 1;
        cpu.Pc = 0xf1b;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0, cpu.SFRs.Acc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0xf5f, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x80, cpu.SFRs.Acc);
    }

    [Fact]
    public void BZ_Example2()
    {
        // VMC-201
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x00, 0x01, // acc
            (byte)Opcode.BZ, 0x3f,
            OpcodePrefix.DEC.Compose(AddressingMode.Direct1), 0x00, // acc
            (byte)Opcode.ROR,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf1b));

        cpu.SFRs.Acc = 1;
        cpu.Pc = 0xf1b;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);
        Assert.Equal(0xf1e, cpu.Pc);

        // branch not taken
        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xf20, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0, cpu.SFRs.Acc);
        Assert.Equal(0xf22, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0, cpu.SFRs.Acc);
    }

    [Fact]
    public void BNZ_Example2()
    {
        // VMC-202
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x00, 0x01, // acc
            (byte)Opcode.BNZ, 0x3f,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf1b));

        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x00, // acc
            (byte)Opcode.ROR,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf5f));

        cpu.SFRs.Acc = 1;
        cpu.Pc = 0xf1b;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);
        Assert.Equal(0xf5f, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(2, cpu.SFRs.Acc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);
    }

    [Fact]
    public void BP_Example1()
    {
        // VMC-203
        var cpu = new Cpu();

        // The example was a little unclear, but, I think the NOPs are supposed to
        // directly precede the INC, because otherwise it is unspecified how to get from the NOPs to the first INC.

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x02, 0x01, // b
            0x78, 0x02, 0x3f, // BP B,0,LA
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf1a));

        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x02, // b
            (byte)Opcode.NOP,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf5f));

        cpu.Pc = 0xf1a;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(1, cpu.SFRs.B);
        Assert.Equal(0xf1d, cpu.Pc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xf5f, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(2, cpu.SFRs.B);
        Assert.Equal(0xf61, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xf62, cpu.Pc);
    }
}