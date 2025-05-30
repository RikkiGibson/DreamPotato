using DreamPotato.Core;

namespace DreamPotato.Tests;

public class BitManipulationTests
{
    [Fact]
    public void CLR1_Example1()
    {
        // VMC-219
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodeMask.MOV | AddressModeMask.Direct1, 0x00, 0x01, // MOV #001H,ACC
            (OpcodeMask.CLR1 | 0b1_0000 /*d8*/ | 0x0 /*b2-0*/), 0x00, // CLR1 ACC,0
        ];
        instructions.CopyTo(cpu.ROM.AsSpan());

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x01, cpu.SFRs.Acc);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.SFRs.Acc);
    }

    [Fact]
    public void SET1_Example2()
    {
        // VMC-220
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodeMask.MOV | AddressModeMask.Direct0, 0x7f, 0x01, // MOV #001H,07FH
            (OpcodeMask.SET1 | 0b0_0000 /*d8*/ | 0b110 /*b2-0*/), 0x7F, // SET1 07FH,6
        ];
        instructions.CopyTo(cpu.ROM.AsSpan());

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x01, cpu.ReadRam(0x7f));

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x41, cpu.ReadRam(0x7f));
    }

    [Fact]
    public void NOT1_Example2()
    {
        // VMC-220
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodeMask.MOV | AddressModeMask.Direct0, 0x7f, 0x01, // MOV #001H,07FH
            (OpcodeMask.NOT1 | 0b0_0000 /*d8*/ | 0b110 /*b2-0*/), 0x7F, // NOT1 07FH,6
        ];
        instructions.CopyTo(cpu.ROM.AsSpan());

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x01, cpu.ReadRam(0x7f));

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x41, cpu.ReadRam(0x7f));
    }
}