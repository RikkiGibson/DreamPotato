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
    public void BNZ_BranchBackwards()
    {
        // VMC-202
        var cpu = new Cpu();

        // take difference between address of dest instruction, and address of instruction after BNZ.
        var offset = 0xf64 - 0xf1f;
        var twoC = ~offset + 1;
        Assert.Equal(0, twoC + offset);

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x00, 0x01, // acc
            (byte)Opcode.BNZ, (byte)twoC,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf5f));

        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x00, // acc
            (byte)Opcode.ROR,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf1f));

        cpu.SFRs.Acc = 1;
        cpu.Pc = 0xf5f;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);
        Assert.Equal(0xf1f, cpu.Pc);

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

    [Fact]
    public void BPC_Example1()
    {
        // VMC-204
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x02, 0x01, // b
            0x58, 0x02, 0x3f, // BPC B,0,LA
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
        Assert.Equal(0, cpu.SFRs.B);
        Assert.Equal(0xf5f, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.B);
        Assert.Equal(0xf61, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xf62, cpu.Pc);
    }

    [Fact]
    public void BPC_Example1_Bit2()
    {
        // VMC-204
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x02, 0x04, // b
            0x5a, 0x02, 0x3f, // BPC B,2,LA
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf1a));

        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x02, // b
            (byte)Opcode.NOP,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf5f));

        cpu.Pc = 0xf1a;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(4, cpu.SFRs.B);
        Assert.Equal(0xf1d, cpu.Pc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0, cpu.SFRs.B);
        Assert.Equal(0xf5f, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.B);
        Assert.Equal(0xf61, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xf62, cpu.Pc);
    }

    [Fact]
    public void BN_Example1()
    {
        // VMC-205
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x02, 0xfe, // b
            0x98, 0x02, 0x3f, // BN B,0,LA
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf1a));

        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x02, // b
            (byte)Opcode.NOP,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf5f));

        cpu.Pc = 0xf1a;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xfe, cpu.SFRs.B);
        Assert.Equal(0xf1d, cpu.Pc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xfe, cpu.SFRs.B);
        Assert.Equal(0xf5f, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xff, cpu.SFRs.B);
        Assert.Equal(0xf61, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xf62, cpu.Pc);
    }

    [Fact]
    public void DBNZ_Direct_Example1()
    {
        // VMC-206
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x02, 0x02, // b
            0x53, 0x02, 0x3f, // DBNZ B,LA
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf1a));

        // LA
        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x02, // b
            (byte)Opcode.NOP,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf5f));

        cpu.Pc = 0xf1a;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x02, cpu.SFRs.B);
        Assert.Equal(0xf1d, cpu.Pc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x01, cpu.SFRs.B);
        Assert.Equal(0xf5f, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x2, cpu.SFRs.B);
        Assert.Equal(0xf61, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xf62, cpu.Pc);
    }

    [Fact]
    public void DBNZ_Direct_Example2()
    {
        // VMC-206
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x00, 0x01, // acc
            0x53, 0x00, 0x3f, // DBNZ ACC,LA
            OpcodePrefix.DEC.Compose(AddressingMode.Direct1), 0x00, // acc
            (byte)Opcode.ROR,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf1a));

        // LA
        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x00, // acc
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf5f));

        cpu.Pc = 0xf1a;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x01, cpu.SFRs.Acc);
        Assert.Equal(0xf1d, cpu.Pc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x00, cpu.SFRs.Acc);
        Assert.Equal(0xf20, cpu.Pc);

        // branch not taken
        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0xf22, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0xf23, cpu.Pc);
    }

    [Fact]
    public void DBNZ_Indirect_Example1()
    {
        // VMC-207
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x02, 0x02, // b
            OpcodePrefix.MOV.Compose(AddressingMode.Direct0), 0x03, 0x02, // R3
            0x57, 0x3f, // DBNZ @R3,LA
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf18));

        // LA
        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x02, // b
            (byte)Opcode.NOP,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf5f));

        cpu.Pc = 0xf18;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x02, cpu.SFRs.B);
        Assert.Equal(0x02, cpu.IndirectAddressRegisters[3]);
        Assert.Equal(0xf1e, cpu.Pc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x01, cpu.SFRs.B);
        Assert.Equal(0xf5f, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x02, cpu.SFRs.B);
        Assert.Equal(0xf61, cpu.Pc);
    }

    [Fact]
    public void BE_Immediate_Example1()
    {
        // VMC-208
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x00, 0x02, // acc
            OpcodePrefix.BE.Compose(AddressingMode.Immediate), 0x02, 0x3f, // BE 0x02,LA
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf1a));

        // LA
        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x00, // acc
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf5f));

        cpu.Pc = 0xf1a;
        cpu.SFRs.Cy = true;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x02, cpu.SFRs.Acc);
        Assert.Equal(0xf1d, cpu.Pc);

        Assert.Equal(2, cpu.Step());
        Assert.False(cpu.SFRs.Cy);
        Assert.Equal(0xf5f, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x03, cpu.SFRs.Acc);
        Assert.Equal(0xf61, cpu.Pc);
    }

    [Fact]
    public void BE_Direct_Example2()
    {
        // VMC-209
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x00, 0x03, // acc
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x02, 0xf2, // b
            OpcodePrefix.BE.Compose(AddressingMode.Direct1), 0x02, 0x3f, // BE B,LA
            OpcodePrefix.DEC.Compose(AddressingMode.Direct1), 0x00, // acc
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf17));

        // LA
        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x00, // acc
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf5f));

        cpu.Pc = 0xf17;
        cpu.SFRs.Cy = false;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x03, cpu.SFRs.Acc);
        Assert.Equal(0xf2, cpu.SFRs.B);

        Assert.Equal(2, cpu.Step());
        Assert.True(cpu.SFRs.Cy);
        Assert.Equal(0xf20, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x02, cpu.SFRs.Acc);
        Assert.Equal(0xf22, cpu.Pc);
    }

    [Fact]
    public void BE_Indirect_Example1()
    {
        // VMC-210
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x02, 0x05, // b
            OpcodePrefix.MOV.Compose(AddressingMode.Direct0), 0x03, 0x02, // r3
            OpcodePrefix.BE.Compose(AddressingMode.Indirect3), 0x05, 0x3f, // BE @R3,#5H,LA
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf17));

        // LA
        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x02, // b
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf5f));

        cpu.Pc = 0xf17;
        cpu.SFRs.Cy = true;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x05, cpu.SFRs.B);
        Assert.Equal(0x02, cpu.IndirectAddressRegisters[3]);

        Assert.Equal(2, cpu.Step());
        Assert.False(cpu.SFRs.Cy);
        Assert.Equal(0xf5f, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x06, cpu.SFRs.B);
        Assert.Equal(0xf61, cpu.Pc);
    }

    [Fact]
    public void BNE_Direct_Example2()
    {
        // VMC-210
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x00, 0xf2, // acc
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x02, 0xf2, // b
            OpcodePrefix.BNE.Compose(AddressingMode.Direct1), 0x02, 0x3f, // BNE B,LA
            OpcodePrefix.DEC.Compose(AddressingMode.Direct1), 0x00, // acc
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf17));

        // LA
        instructions = [
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), 0x00, // acc
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf5f));

        cpu.Pc = 0xf17;
        cpu.SFRs.Cy = true;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xf2, cpu.SFRs.Acc);
        Assert.Equal(0xf2, cpu.SFRs.B);

        // branch not taken
        Assert.Equal(2, cpu.Step());
        Assert.False(cpu.SFRs.Cy);
        Assert.Equal(0xf20, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xf1, cpu.SFRs.Acc);
        Assert.Equal(0xf22, cpu.Pc);
    }
}