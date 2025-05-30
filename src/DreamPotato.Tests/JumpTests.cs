using DreamPotato.Core;

namespace DreamPotato.Tests;

public class JumpTests
{
    [Fact]
    public void JMP_Example1()
    {
        // VMC-197
        var cpu = new Cpu();

        // starting at 0x0FFB
        scoped ReadOnlySpan<byte> instructions = [
            OpcodeMask.NOP,
            OpcodeMask.NOP,
            0b0011_1111, 0b0000_1110, // JMP to 0xF0E (0b001a_1aaa aaaa_aaaa)
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xFFB));

        instructions = [
            OpcodeMask.INC | AddressModeMask.Direct1, 0, // acc
            OpcodeMask.ROR,
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
            OpcodeMask.NOP,
            OpcodeMask.NOP,
            0b0011_1111, 0b0000_1110, // JMP to 0xF0E (0b001a_1aaa aaaa_aaaa). Encoding: 0x3F0E
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0x0FFC));

        instructions = [
            OpcodeMask.INC | AddressModeMask.Direct1, 0, // acc
            OpcodeMask.ROR,
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
        cpu.Reset();

        // starting at 0x0FFA
        scoped ReadOnlySpan<byte> instructions = [
            OpcodeMask.NOP,
            OpcodeMask.NOP,
            OpcodeMask.JMPF, 0x0f, 0x0e, // JMPF to 0x0F0E
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xFFA));

        instructions = [
            OpcodeMask.INC | AddressModeMask.Direct1, 0, // acc
            OpcodeMask.ROR,
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
        cpu.Reset();

        // starting at 0x0FFC
        scoped ReadOnlySpan<byte> instructions = [
            OpcodeMask.NOP,
            OpcodeMask.NOP,
            OpcodeMask.JMPF, 0x0f, 0x0e,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0x0FFC));

        instructions = [
            OpcodeMask.INC | AddressModeMask.Direct1, 0, // acc
            OpcodeMask.ROR,
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
            OpcodeMask.NOP,
            OpcodeMask.NOP,
            OpcodeMask.BR, 0x3f,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0x0F1C));

        instructions = [
            OpcodeMask.INC | AddressModeMask.Direct1, 0, // acc
            OpcodeMask.ROR,
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
    public void BRF_Example1()
    {
        // VMC-200
        var cpu = new Cpu();

        // The example was a little unclear, but, I think the NOPs are supposed to
        // directly precede the INC, because otherwise it is unspecified how to get from the NOPs to the first INC.
        scoped ReadOnlySpan<byte> instructions = [
            OpcodeMask.NOP,
            OpcodeMask.NOP,
            OpcodeMask.BRF, 0x3f, 0x01,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0x0F1C));

        instructions = [
            OpcodeMask.INC | AddressModeMask.Direct1, 0x00, // acc,
            OpcodeMask.ROR,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0x105F));

        cpu.SFRs.Acc = 0;
        cpu.Pc = 0xF1C;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xF1E, cpu.Pc);

        Assert.Equal(4, cpu.Step());
        Assert.Equal(0x105F, cpu.Pc);

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
            OpcodeMask.NOP,
            OpcodeMask.NOP,
            OpcodeMask.INC | AddressModeMask.Direct1, 0x00, // acc,
            OpcodeMask.ROR,
            OpcodeMask.NOP,
            OpcodeMask.NOP,
            OpcodeMask.BRF, 0xf8, 0xff,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0x1F0C));

        cpu.SFRs.Acc = 0;
        cpu.Pc = 0x1F0C;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x1F0E, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);
        Assert.Equal(0x1F10, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x80, cpu.SFRs.Acc);
        Assert.Equal(0x1F11, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x1F13, cpu.Pc);

        // The sample seems to go to 1F0D instead of 1F0E as indicated, so we have to execute an extra NOP.
        Assert.Equal(4, cpu.Step());
        Assert.Equal(0x1F0D, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x80, cpu.SFRs.Acc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x81, cpu.SFRs.Acc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xC0, cpu.SFRs.Acc);
    }
}