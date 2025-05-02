using VEmu.Core;

namespace VEmu.Tests;

public class InterruptTests
{
    [Fact]
    public void INT0_P70_ConnectedToDreamcast_1()
    {
        var cpu = new Cpu();

        ReadOnlySpan<byte> instructions = [
            OpcodeMask.JMPF, 0x02, 0x80,
            // INT0
            OpcodeMask.JMPF, 0x03, 0x80,
        ];

        ReadOnlySpan<byte> inst280 = [
            OpcodeMask.INC | AddressModeMask.Direct1, SpecialFunctionRegisterIds.Acc,
        ];

        // A real interrupt handler should probably jump back into BIOS.
        // We will just inc a register then 'reti'
        ReadOnlySpan<byte> inst380 = [
            OpcodeMask.INC | AddressModeMask.Direct1, SpecialFunctionRegisterIds.B,
            OpcodeMask.RETI,
        ];

        instructions.CopyTo(cpu.ROM);
        inst280.CopyTo(cpu.ROM.AsSpan(0x280));
        inst380.CopyTo(cpu.ROM.AsSpan(0x380));

        cpu.Step(); // JMPF 0x280
        Assert.Equal(0x280, cpu.Pc);

        cpu.SFRs.P7 = 0b1; // trigger int0
        cpu.Step(); // JMPF 0x380
        Assert.Equal(0x380, cpu.Pc);

        cpu.Step(); // INC B
        Assert.Equal(1, cpu.SFRs.B);
        Assert.Equal(0x382, cpu.Pc);

        Assert.Equal(2, cpu.Step()); // RETI
        Assert.Equal(0x280, cpu.Pc);
        Assert.Equal(0, cpu.SFRs.Acc);
        cpu.Step(); // INC ACC
        Assert.Equal(0x282, cpu.Pc);
        Assert.Equal(1, cpu.SFRs.Acc);
    }
}