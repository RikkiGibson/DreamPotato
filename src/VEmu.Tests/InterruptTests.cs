using VEmu.Core;
using VEmu.Core.SFRs;

namespace VEmu.Tests;

public class InterruptTests
{
    [Fact]
    public void INT0_P70_ConnectedToDreamcast_1()
    {
        var cpu = new Cpu();
        cpu.Reset();
        cpu.SFRs.Ie = new() { MasterInterruptEnable = true };
        cpu.SFRs.I01Cr = new() { Int0Enable = true, Int0LevelTriggered = false, Int0HighTriggered = true };

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

        cpu.ConnectDreamcast(); // trigger int0
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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void INT0_InterruptDisabled(bool masterEnable, bool int0Enable)
    {
        var cpu = new Cpu();
        cpu.Reset();
        cpu.SFRs.Ie = new() { MasterInterruptEnable = masterEnable };
        cpu.SFRs.I01Cr = new() { Int0Enable = int0Enable, Int0LevelTriggered = false, Int0HighTriggered = true };

        ReadOnlySpan<byte> instructions = [
            OpcodeMask.JMPF, 0x02, 0x80,
            // INT0
            OpcodeMask.JMPF, 0x03, 0x80,
        ];

        ReadOnlySpan<byte> inst280 = [
            OpcodeMask.INC | AddressModeMask.Direct1, SpecialFunctionRegisterIds.Acc,
            OpcodeMask.INC | AddressModeMask.Direct1, SpecialFunctionRegisterIds.Acc,
        ];

        ReadOnlySpan<byte> inst380 = [
            OpcodeMask.INC | AddressModeMask.Direct1, SpecialFunctionRegisterIds.B,
            OpcodeMask.RETI,
        ];

        instructions.CopyTo(cpu.ROM);
        inst280.CopyTo(cpu.ROM.AsSpan(0x280));
        inst380.CopyTo(cpu.ROM.AsSpan(0x380));

        cpu.Step(); // JMPF 0x280
        Assert.Equal(0x280, cpu.Pc);

        cpu.ConnectDreamcast(); // trigger int0
        Assert.True(cpu.SFRs.I01Cr.Int0Source);

        cpu.Step(); // INC ACC
        Assert.Equal(0x282, cpu.Pc);
        Assert.Equal(1, cpu.SFRs.Acc);
        cpu.Step(); // INC ACC
        Assert.Equal(0x284, cpu.Pc);
        Assert.Equal(2, cpu.SFRs.Acc);
    }

    [Fact]
    public void INT0_INT1_Simultaneous_1()
    {
        // TODO: fix test. base timer interrupt needs to be disabled possibly?
        var cpu = new Cpu();
        cpu.SFRs.Ie = new() { MasterInterruptEnable = true };
        cpu.SFRs.I01Cr = new()
        {
            Int0Enable = true,
            Int1Enable = true,
            Int0LevelTriggered = false,
            Int0HighTriggered = true,
            Int1LevelTriggered = false,
            Int1HighTriggered = true
        };

        ReadOnlySpan<byte> instructions = [
            OpcodeMask.JMPF, 0x02, 0x80,
            // 0x03: INT0
            OpcodeMask.JMPF, 0x03, 0x80,
            0, 0, 0, 0, 0,
            // 0x0B: INT1
            OpcodeMask.JMPF, 0x04, 0x80,
        ];

        ReadOnlySpan<byte> inst280 = [
            OpcodeMask.INC | AddressModeMask.Direct1, SpecialFunctionRegisterIds.Acc,
            OpcodeMask.INC | AddressModeMask.Direct1, SpecialFunctionRegisterIds.Acc,
        ];

        // A real interrupt handler should probably jump back into BIOS.
        // We will just inc a register then 'reti'
        ReadOnlySpan<byte> inst380 = [
            OpcodeMask.INC | AddressModeMask.Direct1, SpecialFunctionRegisterIds.B,
            OpcodeMask.INC | AddressModeMask.Direct1, SpecialFunctionRegisterIds.B,
            OpcodeMask.RETI,
        ];

        // A real low voltage interrupt handler will cleanup state and warn user about low battery
        // We will just inc a register then 'reti'
        ReadOnlySpan<byte> inst480 = [
            OpcodeMask.INC | AddressModeMask.Direct1, SpecialFunctionRegisterIds.C,
            OpcodeMask.INC | AddressModeMask.Direct1, SpecialFunctionRegisterIds.C,
            OpcodeMask.RETI,
        ];

        instructions.CopyTo(cpu.ROM);
        inst280.CopyTo(cpu.ROM.AsSpan(0x280));
        inst380.CopyTo(cpu.ROM.AsSpan(0x380));
        inst480.CopyTo(cpu.ROM.AsSpan(0x480));

        cpu.Step(); // JMPF 0x280
        Assert.Equal(0x280, cpu.Pc);

        cpu.ReportLowVoltage(); // trigger int1
        // service int1
        cpu.Step(); // JMPF 0x480
        Assert.Equal(0x480, cpu.Pc);

        cpu.ConnectDreamcast(); // trigger int0
        cpu.Step(); // INC C
        Assert.Equal(1, cpu.SFRs.C);
        cpu.Step(); // INC C
        Assert.Equal(2, cpu.SFRs.C);
        cpu.Step(); // RETI
        Assert.Equal(0x280, cpu.Pc);

        // int0 is not serviced until current service routine finishes and one ordinary instruction is executed
        // TODO: VMD-145 states "Interrupt nesting is possible and can be up to 3 levels deep."
        // Does that mean that higher priority interrupts can interrupt lower priority ones?
        // Presumably lower pri ones do not interrupt higher pri ones, otherwise why have priority as a concept?
        cpu.Step(); // INC ACC
        Assert.Equal(1, cpu.SFRs.Acc);
        Assert.Equal(0x282, cpu.Pc);

        cpu.Step(); // JMPF 0x380
        Assert.Equal(0x380, cpu.Pc);

        cpu.Step(); // INC B
        Assert.Equal(1, cpu.SFRs.B);
        cpu.Step(); // INC B
        Assert.Equal(2, cpu.SFRs.B);

        cpu.Step(); // RETI
        Assert.Equal(0x282, cpu.Pc);

        cpu.Step(); // INC ACC
        Assert.Equal(2, cpu.SFRs.Acc);
        Assert.Equal(0x284, cpu.Pc);
    }

    [Fact]
    public void P3Continuous_1()
    {
        var cpu = new Cpu();
        cpu.Reset();
        Assert.True(cpu.SFRs.Ie.MasterInterruptEnable);
        Assert.True(cpu.SFRs.P3Int.Continuous);
        Assert.True(cpu.SFRs.P3Int.Enable);

        // TODO: finish writing this test
        // TODO: write several more interrupts tests showing how they stack or not
        cpu.SFRs.P3 = new P3(0b1111_0111);

        Assert.Equal(Interrupts.P3, cpu.RequestedInterrupts);
        Assert.Equal(0, cpu._interruptsCount);

        cpu.Step();
        Assert.Equal(Interrupts.None, cpu.RequestedInterrupts);
        Assert.Equal(1, cpu._interruptsCount);

        cpu.SFRs.P3 = new P3(0b1111_0111);
    }
}