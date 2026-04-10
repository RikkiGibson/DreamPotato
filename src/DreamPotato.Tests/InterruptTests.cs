using DreamPotato.Core;
using DreamPotato.Core.SFRs;

namespace DreamPotato.Tests;

public class InterruptTests
{
    const string INT0SkipReason = "These tests need to be rewritten to usefully exercise the new handling of the dreamcast connected state.";

    [Fact(Skip = INT0SkipReason)]
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

    [Theory(Skip = INT0SkipReason)]
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

    [Fact(Skip = INT0SkipReason)]
    public void INT0_INT1_Simultaneous_1()
    {
        var cpu = new Cpu();
        cpu.Reset();
        cpu.SFRs.Ie = new() { MasterInterruptEnable = true };
        cpu.SFRs.I01Cr = new()
        {
            Int0Enable = true,
            Int1Enable = true,
            Int0LevelTriggered = false,
            Int0HighTriggered = true,
            Int1LevelTriggered = false,
            Int1HighTriggered = false
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

        Assert.Equal("JMPF 280H", cpu.StepInstruction().ToString());
        Assert.Equal(0x280, cpu.Pc);

        cpu.ReportVoltage(); // trigger int1
        // service int1
        Assert.Equal("JMPF 480H", cpu.StepInstruction().ToString());
        Assert.Equal(0x480, cpu.Pc);

        cpu.ConnectDreamcast(); // trigger int0
        Assert.Equal("JMPF 380H", cpu.StepInstruction().ToString());
        Assert.Equal(0x380, cpu.Pc);

        Assert.Equal("INC B", cpu.StepInstruction().ToString());
        Assert.Equal(1, cpu.SFRs.B);
        Assert.Equal("INC B", cpu.StepInstruction().ToString());
        Assert.Equal(2, cpu.SFRs.B);

        Assert.Equal("RETI", cpu.StepInstruction().ToString());
        Assert.Equal(0x480, cpu.Pc);

        Assert.Equal("INC C", cpu.StepInstruction().ToString());
        Assert.Equal(1, cpu.SFRs.C);
        Assert.Equal("INC C", cpu.StepInstruction().ToString());
        Assert.Equal(2, cpu.SFRs.C);
        Assert.Equal("RETI", cpu.StepInstruction().ToString());
        Assert.Equal(0x280, cpu.Pc);
        Assert.Equal(0, cpu._interruptsCount);

        Assert.Equal("INC Acc", cpu.StepInstruction().ToString());
        Assert.Equal(1, cpu.SFRs.Acc);
        Assert.Equal(0x282, cpu.Pc);

        Assert.Equal("INC Acc", cpu.StepInstruction().ToString());
        Assert.Equal(2, cpu.SFRs.Acc);
        Assert.Equal(0x284, cpu.Pc);
    }

    [Fact]
    public void P3Continuous_1()
    {
        var cpu = new Cpu();

        // jump past interrupt vectors etc
        ReadOnlySpan<byte> instructions = [
            OpcodeMask.JMPF, 0x02, 0x00, // JMPF 0x200,
        ];
        instructions.CopyTo(cpu.CurrentInstructionBank);

        // setup p3 interrupt vector
        instructions = [
            OpcodeMask.NOP,
            OpcodeMask.RETI
        ];
        instructions.CopyTo(cpu.CurrentInstructionBank[InterruptVectors.P3..]);

        cpu.Reset();
        Assert.True(cpu.SFRs.Ie.MasterInterruptEnable);
        Assert.True(cpu.SFRs.P3Int.Continuous);
        Assert.True(cpu.SFRs.P3Int.Enable);

        cpu.SFRs.P3 = new P3(0b1111_0111);
        Assert.False(cpu.SFRs.P3Int.Source);
        Assert.Equal(0, cpu._interruptsCount);
        Assert.Equal(0, cpu.Pc);

        // Note that continuous P3 interrupts are only generated in cpu.Step()
        // (We don't do it in both places to avoid generating the same interrupt twice in one cycle)

        Assert.Equal("JMPF 200H", cpu.StepInstruction().ToString());
        Assert.True(cpu.SFRs.P3Int.Source);
        Assert.Equal(0, cpu._interruptsCount);
        Assert.Equal(0x200, cpu.Pc);

        // Runs the NOP at 0x200 itself
        Assert.Equal("NOP", cpu.StepInstruction().ToString());
        Assert.True(cpu.SFRs.P3Int.Source);
        Assert.Equal(1, cpu._interruptsCount);
        Assert.Equal(InterruptVectors.P3, cpu.Pc);

        // Run the NOP at start of p3 handler
        Assert.Equal("NOP", cpu.StepInstruction().ToString());
        Assert.True(cpu.SFRs.P3Int.Source);
        Assert.Equal(1, cpu._interruptsCount);
        Assert.Equal(InterruptVectors.P3 + 1, cpu.Pc);

        Assert.Equal("RETI", cpu.StepInstruction().ToString());
        Assert.True(cpu.SFRs.P3Int.Source);
        Assert.Equal(0, cpu._interruptsCount);
        Assert.Equal(0x201, cpu.Pc);

        // Stop the interrupt-generating signal
        cpu.SFRs.P3 = new P3(0b1111_1111);

        // Run the NOP at 0x201
        Assert.Equal("NOP", cpu.StepInstruction().ToString());
        // Since we RETI'd, another instruction needs to be executed before we can service the pending P3 interrupt
        Assert.True(cpu.SFRs.P3Int.Source);
        Assert.Equal(1, cpu._interruptsCount);
        Assert.Equal(InterruptVectors.P3, cpu.Pc);

        // Run the NOP at start of p3 handler
        Assert.Equal("NOP", cpu.StepInstruction().ToString());
        Assert.True(cpu.SFRs.P3Int.Source); // Note: nothing in the user code is actually disabling this flag. The system doesn't automatically disable it.
        Assert.Equal(1, cpu._interruptsCount);
        Assert.Equal(InterruptVectors.P3 + 1, cpu.Pc);

        Assert.Equal("RETI", cpu.StepInstruction().ToString());
        Assert.True(cpu.SFRs.P3Int.Source);
        Assert.Equal(0, cpu._interruptsCount);
        Assert.Equal(0x202, cpu.Pc);

        cpu.SFRs.P3 = new P3(0b1111_0111);
    }

    [Fact]
    public void IntDelays()
    {
        // Execute the assembled version of 'IntDelays.s'
        if (!File.Exists("Data/american_v1.05.bin"))
            Assert.Skip("Test requires a BIOS");

        var vmu = new Vmu();
        var cpu = vmu._cpu;
        cpu.DreamcastSlot = DreamcastSlot.Slot1;
        vmu.LoadRom();
        vmu.LoadGameVms("TestSource/IntDelays.vms", date: DateTimeOffset.Parse("09/09/1999"), autoInitializeRTCDate: true);

        cpu.Run(TimeSpan.TicksPerSecond);
        pressButtonAndWait(cpu, new P3(0xff) { ButtonMode = false });
        pressButtonAndWait(cpu, new P3(0xff) { ButtonA = false });

        // Real HW result:
        // 1100
        // 0000
        // 00 0
        // 10
        // Emu result (matching):
        // 1100
        // 0000
        // 00 0
        // 10
        Assert.Equal<object>("""
            |  ▄██     ▄██   ▄██▀▀█▄ ▄██▀▀█▄
            |   ██      ██   ██   ██ ██   ██
            |   ██      ██   ██  ▄██ ██  ▄██
            |  ▀▀▀▀    ▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀
            |▄██▀▀█▄ ▄██▀▀█▄ ▄██▀▀█▄ ▄██▀▀█▄
            |██   ██ ██   ██ ██   ██ ██   ██
            |██  ▄██ ██  ▄██ ██  ▄██ ██  ▄██
            | ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀
            |▄██▀▀█▄ ▄██▀▀█▄         ▄██▀▀█▄
            |██   ██ ██   ██         ██   ██
            |██  ▄██ ██  ▄██         ██  ▄██
            | ▀▀▀▀▀   ▀▀▀▀▀           ▀▀▀▀▀
            |  ▄██   ▄██▀▀█▄
            |   ██   ██   ██
            |   ██   ██  ▄██
            |  ▀▀▀▀   ▀▀▀▀▀
            """,
            cpu.Display.ToTestDisplayString());

        static void pressButtonAndWait(Cpu cpu, P3 pressedState)
        {
            const long halfSecond = TimeSpan.TicksPerSecond / 2;
            cpu.SFRs.P3 = pressedState;
            cpu.Run(halfSecond);
            cpu.SFRs.P3 = new P3(0xff);
            cpu.Run(TimeSpan.TicksPerSecond * 2);
        }
    }

    [Fact]
    public void TimerIntSeq1()
    {
        // Execute the assembled version of 'TimerIntSeq1.s'
        if (!File.Exists("Data/american_v1.05.bin"))
            Assert.Skip("Test requires a BIOS");

        var vmu = new Vmu();
        var cpu = vmu._cpu;
        cpu.DreamcastSlot = DreamcastSlot.Slot1;
        vmu.LoadRom();
        vmu.LoadGameVms("TestSource/TimerIntSeq1.vms", date: DateTimeOffset.Parse("09/09/1999"), autoInitializeRTCDate: true);

        cpu.Run(TimeSpan.TicksPerSecond);
        pressButtonAndWait(cpu, new P3(0xff) { ButtonMode = false });
        pressButtonAndWait(cpu, new P3(0xff) { ButtonA = false });

        // Real HW result:
        // 238855
        // 234877
        // 234877
        // 238855
        // Emu result (matching):
        // 238855
        // 234877
        // 234877
        // 238855
        Assert.Equal<object>("""
            |▄█▀▀▀█▄ ▄██▀▀█▄ ▄█▀▀▀█▄ ▄█▀▀▀█▄ ██▀▀▀▀▀ ██▀▀▀▀▀
            |▀▀  ▄█▀    ▄▄█▀ ▀█▄▄▄█▀ ▀█▄▄▄█▀ ▀▀▀▀▀█▄ ▀▀▀▀▀█▄
            | ▄█▀▀   ▄▄▄  ██ ██   ██ ██   ██ ▄▄   ██ ▄▄   ██
            |▀▀▀▀▀▀▀  ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀
            |▄█▀▀▀█▄ ▄██▀▀█▄    ▄██  ▄█▀▀▀█▄ ██▀▀▀██ ██▀▀▀██
            |▀▀  ▄█▀    ▄▄█▀  ▄█▀██  ▀█▄▄▄█▀     ▄█      ▄█
            | ▄█▀▀   ▄▄▄  ██ ██▄▄██▄ ██   ██    ██      ██
            |▀▀▀▀▀▀▀  ▀▀▀▀▀      ▀▀   ▀▀▀▀▀    ▀▀▀     ▀▀▀
            |▄█▀▀▀█▄ ▄██▀▀█▄    ▄██  ▄█▀▀▀█▄ ██▀▀▀██ ██▀▀▀██
            |▀▀  ▄█▀    ▄▄█▀  ▄█▀██  ▀█▄▄▄█▀     ▄█      ▄█
            | ▄█▀▀   ▄▄▄  ██ ██▄▄██▄ ██   ██    ██      ██
            |▀▀▀▀▀▀▀  ▀▀▀▀▀      ▀▀   ▀▀▀▀▀    ▀▀▀     ▀▀▀
            |▄█▀▀▀█▄ ▄██▀▀█▄ ▄█▀▀▀█▄ ▄█▀▀▀█▄ ██▀▀▀▀▀ ██▀▀▀▀▀
            |▀▀  ▄█▀    ▄▄█▀ ▀█▄▄▄█▀ ▀█▄▄▄█▀ ▀▀▀▀▀█▄ ▀▀▀▀▀█▄
            | ▄█▀▀   ▄▄▄  ██ ██   ██ ██   ██ ▄▄   ██ ▄▄   ██
            |▀▀▀▀▀▀▀  ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀
            """,
            cpu.Display.ToTestDisplayString());

        static void pressButtonAndWait(Cpu cpu, P3 pressedState)
        {
            const long halfSecond = TimeSpan.TicksPerSecond / 2;
            cpu.SFRs.P3 = pressedState;
            cpu.Run(halfSecond);
            cpu.SFRs.P3 = new P3(0xff);
            cpu.Run(TimeSpan.TicksPerSecond * 2);
        }
    }

    [Fact]
    public void TimerPreemption()
    {
        // Execute the assembled version of 'TimerPreemption.s'
        if (!File.Exists("Data/american_v1.05.bin"))
            Assert.Skip("Test requires a BIOS");

        var vmu = new Vmu();
        var cpu = vmu._cpu;
        cpu.DreamcastSlot = DreamcastSlot.Slot1;
        vmu.LoadRom();
        vmu.LoadGameVms("TestSource/TimerPreemption.vms", date: DateTimeOffset.Parse("09/09/1999"), autoInitializeRTCDate: true);

        cpu.Run(TimeSpan.TicksPerSecond);
        pressButtonAndWait(cpu, new P3(0xff) { ButtonMode = false });
        pressButtonAndWait(cpu, new P3(0xff) { ButtonA = false });
        cpu.Run(TimeSpan.TicksPerSecond * 10);

        // Real hardware result:
        // 000028
        // 081000
        // 118060
        // 024026
        // Emu result (not matching):
        // 000028
        // 081003
        // 118060
        // 024028
        Assert.Equal<object>("""
            |▄██▀▀█▄ ▄██▀▀█▄ ▄██▀▀█▄ ▄██▀▀█▄ ▄█▀▀▀█▄ ▄█▀▀▀█▄
            |██   ██ ██   ██ ██   ██ ██   ██ ▀▀  ▄█▀ ▀█▄▄▄█▀
            |██  ▄██ ██  ▄██ ██  ▄██ ██  ▄██  ▄█▀▀   ██   ██
            | ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀  ▀▀▀▀▀▀▀  ▀▀▀▀▀
            |▄██▀▀█▄ ▄█▀▀▀█▄   ▄██   ▄██▀▀█▄ ▄██▀▀█▄ ▄██▀▀█▄
            |██   ██ ▀█▄▄▄█▀    ██   ██   ██ ██   ██ ██   ██
            |██  ▄██ ██   ██    ██   ██  ▄██ ██  ▄██ ██  ▄██
            | ▀▀▀▀▀   ▀▀▀▀▀    ▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀
            |  ▄██     ▄██   ▄█▀▀▀█▄ ▄██▀▀█▄   ▄█▀▀  ▄██▀▀█▄
            |   ██      ██   ▀█▄▄▄█▀ ██   ██ ▄██▄▄▄  ██   ██
            |   ██      ██   ██   ██ ██  ▄██ ██   ██ ██  ▄██
            |  ▀▀▀▀    ▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀
            |▄██▀▀█▄ ▄█▀▀▀█▄    ▄██  ▄██▀▀█▄ ▄█▀▀▀█▄ ▄█▀▀▀█▄
            |██   ██ ▀▀  ▄█▀  ▄█▀██  ██   ██ ▀▀  ▄█▀ ▀█▄▄▄█▀
            |██  ▄██  ▄█▀▀   ██▄▄██▄ ██  ▄██  ▄█▀▀   ██   ██
            | ▀▀▀▀▀  ▀▀▀▀▀▀▀     ▀▀   ▀▀▀▀▀  ▀▀▀▀▀▀▀  ▀▀▀▀▀
            """,
            cpu.Display.ToTestDisplayString());

        static void pressButtonAndWait(Cpu cpu, P3 pressedState)
        {
            const long halfSecond = TimeSpan.TicksPerSecond / 2;
            cpu.SFRs.P3 = pressedState;
            cpu.Run(halfSecond);
            cpu.SFRs.P3 = new P3(0xff);
            cpu.Run(TimeSpan.TicksPerSecond * 2);
        }
    }

    [Fact]
    public void IntServicingAndHaltDelays()
    {
        // Execute the assembled version of 'IntServicingAndHaltDelays.s'
        if (!File.Exists("Data/american_v1.05.bin"))
            Assert.Skip("Test requires a BIOS");

        var vmu = new Vmu();
        var cpu = vmu._cpu;
        cpu.DreamcastSlot = DreamcastSlot.Slot1;
        vmu.LoadRom();
        vmu.LoadGameVms("TestSource/IntServicingAndHaltDelays.vms", date: DateTimeOffset.Parse("09/09/1999"), autoInitializeRTCDate: true);

        cpu.Run(TimeSpan.TicksPerSecond);
        pressButtonAndWait(cpu, new P3(0xff) { ButtonMode = false });
        pressButtonAndWait(cpu, new P3(0xff) { ButtonA = false });
        cpu.Run(TimeSpan.TicksPerSecond * 10);

        // Real HW result:
        // 008009
        // 008017
        // 000009
        // 009067
        // Emu result (not matching):
        // 008000
        // 008017
        // 000009
        // 009000
        Assert.Equal<object>("""
            |▄██▀▀█▄ ▄██▀▀█▄ ▄█▀▀▀█▄ ▄██▀▀█▄ ▄██▀▀█▄ ▄██▀▀█▄
            |██   ██ ██   ██ ▀█▄▄▄█▀ ██   ██ ██   ██ ██   ██
            |██  ▄██ ██  ▄██ ██   ██ ██  ▄██ ██  ▄██ ██  ▄██
            | ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀
            |▄██▀▀█▄ ▄██▀▀█▄ ▄█▀▀▀█▄ ▄██▀▀█▄   ▄██   ██▀▀▀██
            |██   ██ ██   ██ ▀█▄▄▄█▀ ██   ██    ██       ▄█
            |██  ▄██ ██  ▄██ ██   ██ ██  ▄██    ██      ██
            | ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀    ▀▀▀▀    ▀▀▀
            |▄██▀▀█▄ ▄██▀▀█▄ ▄██▀▀█▄ ▄██▀▀█▄ ▄██▀▀█▄ ▄█▀▀▀█▄
            |██   ██ ██   ██ ██   ██ ██   ██ ██   ██ ▀█▄▄▄██
            |██  ▄██ ██  ▄██ ██  ▄██ ██  ▄██ ██  ▄██     ▄█▀
            | ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀
            |▄██▀▀█▄ ▄██▀▀█▄ ▄█▀▀▀█▄ ▄██▀▀█▄ ▄██▀▀█▄ ▄██▀▀█▄
            |██   ██ ██   ██ ▀█▄▄▄██ ██   ██ ██   ██ ██   ██
            |██  ▄██ ██  ▄██     ▄█▀ ██  ▄██ ██  ▄██ ██  ▄██
            | ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀    ▀▀▀▀▀   ▀▀▀▀▀   ▀▀▀▀▀
            """,
            cpu.Display.ToTestDisplayString());

        static void pressButtonAndWait(Cpu cpu, P3 pressedState)
        {
            const long halfSecond = TimeSpan.TicksPerSecond / 2;
            cpu.SFRs.P3 = pressedState;
            cpu.Run(halfSecond);
            cpu.SFRs.P3 = new P3(0xff);
            cpu.Run(TimeSpan.TicksPerSecond * 2);
        }
    }
}