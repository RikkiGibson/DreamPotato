using DreamPotato.Core;

namespace DreamPotato.Tests;

public class SubroutineTests
{
    [Fact]
    public void CALL_Example1()
    {
        // VMC-214
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodeMask.MOV | AddressModeMask.Direct1, 0x06, 0x1f, // sp
            0x1f, 0x0e, // CALL LA
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xffa));

        // LA
        instructions = [
            OpcodeMask.INC | AddressModeMask.Direct1, 0x00, // acc
            OpcodeMask.RET,
            OpcodeMask.NOP,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf0e));

        cpu.SFRs.Acc = 0;
        cpu.Pc = 0xffa;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x1f, cpu.SFRs.Sp);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xf0e, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xfff, cpu.Pc);
    }

    [Fact]
    public void CALLF_Example2()
    {
        // VMC-215
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodeMask.MOV | AddressModeMask.Direct1, 0x06, 0x1f, // sp
            0x20, 0x0f, 0x0e, // CALLF LA
            OpcodeMask.INC | AddressModeMask.Direct1, 0x00, // acc
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xff9));

        // LA
        instructions = [
            OpcodeMask.INC | AddressModeMask.Direct1, 0x00, // acc
            OpcodeMask.RET,
            OpcodeMask.NOP,
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0xf0e));

        cpu.SFRs.Acc = 0;
        cpu.Pc = 0xff9;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x1f, cpu.SFRs.Sp);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xf0e, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xfff, cpu.Pc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(2, cpu.SFRs.Acc);
    }

    [Fact]
    public void CALLR_Example2()
    {
        // VMC-216
        var cpu = new Cpu();

        // I think the address math was off in the example.

        ReadOnlySpan<byte> instructions = [
            OpcodeMask.MOV | AddressModeMask.Direct1, 0x06, 0x1f, // sp
            0x10, 0x01, 0x01, // CALLR LA
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0x0FFA));

        instructions = [
            OpcodeMask.INC | AddressModeMask.Direct1, 0x00, // acc
            OpcodeMask.RET,
            OpcodeMask.INC | AddressModeMask.Direct1, 0x00, // acc
        ];
        instructions.CopyTo(cpu.ROM.AsSpan(startIndex: 0x01100));

        cpu.SFRs.Acc = 0;
        cpu.Pc = 0x0FFA;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(4, cpu.Step());
        Assert.Equal(0x1100, cpu.Pc);
        Assert.Equal(0x21, cpu.SFRs.Sp);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(1, cpu.SFRs.Acc);
        Assert.Equal(0x1102, cpu.Pc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x1000, cpu.Pc);
    }

    // RET is tested by the other methods in this file

    [Fact]
    public void RETI_Example1()
    {
        // VMC-217
        var cpu = new Cpu();
        // TODO: not going to be able to test this meaningfully, until interrupts are handled.
    }
}