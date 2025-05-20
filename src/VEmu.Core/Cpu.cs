using System.Diagnostics;

using VEmu.Core.SFRs;

namespace VEmu.Core;

public class Cpu
{
    public Logger Logger { get; }

    // VMD-35: Accumulator and all registers are mapped to RAM.

    // VMD-38: Memory
    //

    internal const int InstructionBankSize = 64 * 1024;

    /// <summary>Read-only memory space.</summary>
    public readonly byte[] ROM = new byte[InstructionBankSize];
    public readonly byte[] FlashBank0 = new byte[InstructionBankSize];
    public readonly byte[] FlashBank1 = new byte[InstructionBankSize];

    // TODO: a separate instruction map is needed per bank, and it needs to be cleared when a given bank changes.
    internal readonly InstructionMap InstructionMap = new();

    /// <summary>
    /// May point to either ROM (BIOS), flash memory bank 0 or bank 1.
    /// </summary>
    /// <remarks>
    /// Note that we need an extra bit of state here. We can't just look at the value of <see cref="SpecialFunctionRegisters.Ext"/>.
    /// The bank is only actually switched when using a jmpf instruction.
    /// </remarks>
    public byte[] CurrentROMBank => InstructionBank switch
    {
        InstructionBank.ROM => ROM,
        InstructionBank.FlashBank0 => FlashBank0,
        InstructionBank.FlashBank1 => FlashBank1,
        _ => throw new InvalidOperationException()
    };

    internal InstructionBank InstructionBank { get; private set; }

    public readonly Memory Memory;

    internal ushort Pc;

    /// <summary>
    /// After <see cref="Run(long)"/> is called, stores how many more ticks were run than requested,
    /// to reduce the duration of the next frame execution.
    /// </summary>
    internal long TicksOverrun;

    /// <summary>
    /// After <see cref="StepTicks()"/> is called, stores the remainder of a tick, which elapsed partially during execution of a single instruction.
    /// </summary>
    // TODO: it's doubtful this is helpful. The remainder of a tick is less than 100ns.
    internal long StepCycleTicksPerSecondRemainder;

    /// <summary>
    /// 14-bit base timer.
    /// Overflow of the lower 8 bits sets <see cref="Btcr.Int1Source"/>.
    /// Overflow of the upper 6 bits sets <see cref="Btcr.Int0Source"/> and <see cref="Btcr.Int1Source"/>.
    /// </summary>
    internal ushort BaseTimer;
    internal const ushort BaseTimerMax = 1 << 14;
    internal long BaseTimerTicksRemaining;

    internal Interrupts RequestedInterrupts;
    private InterruptServicingState _interruptServicingState;

    /// <summary>
    /// Maximum number of interrupts which can be consecutively serviced.
    /// When this limit is reached, further interrupts are not serviced
    /// until returning from the current interrupt service routine.
    /// </summary>
    private const int InterruptsCountMax = 3;
    internal readonly Interrupts[] _servicingInterrupts = new Interrupts[3];
    internal int _interruptsCount;

    // TODO: LoggerOptions type? Logger can't really be passed in since it needs to hold 'this'.
    public Cpu(LogLevel logLevel = LogLevel.Trace)
    {
        var categories = LogCategories.General | LogCategories.SystemClock;
        Logger = new Logger(logLevel, categories, this);
        Memory = new Memory(this, Logger);
        SetInstructionBank(InstructionBank.ROM);
    }

    public void Reset()
    {
        Pc = 0;
        TicksOverrun = 0;
        StepCycleTicksPerSecondRemainder = 0;
        RequestedInterrupts = 0;
        BaseTimer = 0;
        BaseTimerTicksRemaining = 0;
        RequestedInterrupts = Interrupts.None;
        Array.Clear(_servicingInterrupts);
        _interruptsCount = 0;
        _interruptServicingState = InterruptServicingState.Ready;
        Memory.Reset();
        SyncInstructionBank();
    }

    /// <summary>
    /// Updates <see cref="InstructionBank"/> to match <see cref="Ext.InstructionBank"/>.
    /// </summary>
    private void SyncInstructionBank()
    {
        var newBank = SFRs.Ext.InstructionBank;
        if (newBank == InstructionBank.ROM)
        {
            if (Pc == BuiltInCodeSymbols.BIOSClockTick)
            {
                Logger.LogTrace($"Calling {nameof(BuiltInCodeSymbols.BIOSClockTick)}", LogCategories.Timers);
            }
        }

        InstructionBank = newBank;
    }

    /// <summary>
    /// Sets the current instruction bank both in the CPU itself and in <see cref="Ext.InstructionBank"/>.
    /// </summary>
    /// <param name="bank"></param>
    public void SetInstructionBank(InstructionBank bank)
    {
        SFRs.Ext = SFRs.Ext with { InstructionBank = bank };
        InstructionBank = bank;
    }

    public byte ReadRam(int address)
    {
        Debug.Assert(address < 0x200);
        return Memory.Read((ushort)address);
    }

    public void WriteRam(int address, byte value)
    {
        Debug.Assert(address < 0x200);
        Memory.Write((ushort)address, value);
    }

    public SpecialFunctionRegisters SFRs => Memory.SFRs;

    public long Run(long ticksToRun)
    {
        // Reduce the number of ticks we were asked to run, by the amount we overran last frame.
        ticksToRun -= TicksOverrun;

        long ticksSoFar = 0;
        while (ticksSoFar < ticksToRun)
        {
            ticksSoFar += StepTicks();
        }
        TicksOverrun = ticksSoFar - ticksToRun;

        return ticksSoFar;
    }

    #region External interrupt triggers
    /// <summary>
    /// Simulate INT0 (connecting VMU to Dreamcast)
    /// </summary>
    /// <param name="connect">true if connecting, false if disconnecting</param>
    internal void ConnectDreamcast(bool connect = true)
    {
        // TODO: interrupts are continuously generated in level triggered mode.
        // perhaps this can be simulated by adding checks during interrupt servicing?
        // can an interrupt be interrupted with itself? That doesn't seem right. Again it seems reasonable for only a higher pri interrupt to take over the current interrupt.

        // We don't do these checks in the internal read/write because user code which writes these is treated as writing a latch.
        // An external interrupt is only supposed to be generated by an external signal.
        var oldP7 = SFRs.P7;
        SFRs.P7 = oldP7 with { DreamcastConnected = connect };

        var i01cr = SFRs.I01Cr;
        var isLevelTriggered = i01cr.Int0LevelTriggered;
        var isHighTriggered = i01cr.Int0HighTriggered;

        // level trigger: just need to be at the right level to trigger it
        if (isLevelTriggered && (isHighTriggered == connect))
        {
            i01cr.Int0Source = true;
            if (i01cr.Int0Enable)
                RequestedInterrupts |= Interrupts.INT0;
        }

        // edge trigger: need to transition to the desired level to trigger it
        if (!isLevelTriggered && (oldP7.DreamcastConnected != connect) && (isHighTriggered == connect))
        {
            i01cr.Int0Source = true;
            if (i01cr.Int0Enable)
                RequestedInterrupts |= Interrupts.INT0;
        }

        SFRs.I01Cr = i01cr;
    }

    /// <summary>
    /// Simulate INT1 (low voltage)
    /// </summary>
    /// <param name="lowVoltage">true if entering low voltage mode, false if exiting low voltage mode</param>
    internal void ReportLowVoltage(bool lowVoltage = true)
    {
        var oldP7 = SFRs.P7;
        SFRs.P7 = oldP7 with { LowVoltage = lowVoltage };

        var i01cr = SFRs.I01Cr;
        var isLevelTriggered = i01cr.Int1LevelTriggered;
        var isHighTriggered = i01cr.Int1HighTriggered;

        // level trigger: just need to be at the right level to trigger it
        if (isLevelTriggered && (isHighTriggered == lowVoltage))
        {
            i01cr.Int1Source = true;
            if (i01cr.Int1Enable)
                RequestedInterrupts |= Interrupts.INT1;
        }

        // edge trigger: need to transition to the desired level to trigger it
        if (!isLevelTriggered && (oldP7.LowVoltage != lowVoltage) && (isHighTriggered == lowVoltage))
        {
            i01cr.Int1Source = true;
            if (i01cr.Int1Enable)
                RequestedInterrupts |= Interrupts.INT1;
        }
    }
    #endregion

    private void ServiceInterruptIfNeeded()
    {
        if (RequestedInterrupts == Interrupts.None)
            return;

        if (_interruptServicingState != InterruptServicingState.Ready)
            return;

        if (_interruptsCount >= InterruptsCountMax)
            return;

        var ie = SFRs.Ie;
        if (!ie.MasterInterruptEnable)
            return;

        var currentInterrupt = _interruptsCount == 0 ? Interrupts.None : _servicingInterrupts[_interruptsCount - 1];
        Debug.Assert(BitHelpers.IsPowerOfTwo((int)currentInterrupt));

        // Highest Priority
        if (ie.Int0Priority && shouldServiceInterrupt(Interrupts.INT0))
        {
            serviceInterrupt(Interrupts.INT0, InterruptVectors.INT0);
            return;
        }
        if (ie.Int1Priority && shouldServiceInterrupt(Interrupts.INT1))
        {
            serviceInterrupt(Interrupts.INT1, InterruptVectors.INT1);
            return;
        }

        // High Priority
        var interruptPriority = SFRs.Ip;
        if (tryServiceOneInterrupt(highPriority: true))
            return;

        tryServiceOneInterrupt(highPriority: false);

        bool tryServiceOneInterrupt(bool highPriority)
        {
            if ((!highPriority || interruptPriority.Int2_T0L) && shouldServiceInterrupt(Interrupts.INT2_T0L))
            {
                serviceInterrupt(Interrupts.INT2_T0L, InterruptVectors.INT2_T0L);
                return true;
            }

            if ((!highPriority || interruptPriority.Int3_BaseTimer) && shouldServiceInterrupt(Interrupts.INT3_BT))
            {
                serviceInterrupt(Interrupts.INT3_BT, InterruptVectors.INT3_BT);
                return true;
            }

            if ((!highPriority || interruptPriority.T0H) && shouldServiceInterrupt(Interrupts.T0H))
            {
                serviceInterrupt(Interrupts.T0H, InterruptVectors.T0H);
                return true;
            }

            if ((!highPriority || interruptPriority.T1) && shouldServiceInterrupt(Interrupts.T1))
            {
                serviceInterrupt(Interrupts.T1, InterruptVectors.T1);
                return true;
            }

            if ((!highPriority || interruptPriority.Sio0) && shouldServiceInterrupt(Interrupts.SIO0))
            {
                serviceInterrupt(Interrupts.SIO0, InterruptVectors.SIO0);
                return true;
            }

            if ((!highPriority || interruptPriority.Sio1) && shouldServiceInterrupt(Interrupts.SIO1))
            {
                serviceInterrupt(Interrupts.SIO1, InterruptVectors.SIO1);
                return true;
            }

            if ((!highPriority || interruptPriority.Maple) && shouldServiceInterrupt(Interrupts.Maple))
            {
                serviceInterrupt(Interrupts.Maple, InterruptVectors.Maple);
                return true;
            }

            if ((!highPriority || interruptPriority.Port3) && shouldServiceInterrupt(Interrupts.P3))
            {
                serviceInterrupt(Interrupts.P3, InterruptVectors.P3);
                return true;
            }

            return false;
        }

        bool shouldServiceInterrupt(Interrupts candidateInterrupt)
        {
            Debug.Assert(BitHelpers.IsPowerOfTwo((int)candidateInterrupt));

            // TODO: does priority factor in at all when interrupting one interrupt with another?
            // It feels like if so, the "priority bits" in the interrupt control registers need to factor in.
            // return (RequestedInterrupts & candidateInterrupt) != 0;
            return (RequestedInterrupts & candidateInterrupt) != 0
                && candidateInterrupt.IsHigherPriorityThan(currentInterrupt);
        }

        void serviceInterrupt(Interrupts interrupt, ushort routineAddress)
        {
            Debug.Assert(BitHelpers.IsPowerOfTwo((int)interrupt));

            _servicingInterrupts[_interruptsCount] = interrupt;
            _interruptsCount++;
            Logger.LogDebug($"Servicing interrupt '{interrupt}'. Count: '{_interruptsCount}'.", LogCategories.Interrupts);
            if (_interruptsCount > 1)
            { // breakpoint holder
            }
            RequestedInterrupts &= ~interrupt;
            SFRs.Pcon = SFRs.Pcon with { HaltMode = false };

            Memory.PushStack((byte)Pc);
            Memory.PushStack((byte)(Pc >> 8));
            Pc = routineAddress;
        }
    }

    private void AdvanceInterruptState()
    {
        if (_interruptServicingState == InterruptServicingState.ReturnedFromInterrupt)
            _interruptServicingState = InterruptServicingState.Ready;
    }

    internal long StepTicks()
    {
        // Note that any instruction which modifies OCR, etc, is presumed to only affect the speed starting on the next instruction.
        var cpuClockHz = SFRs.Ocr.CpuClockHz;

        var cpuCycles = Step();
        // Compute a quantity which, when divided by cpuClockHz, yields the number of ticks elapsed by the instruction.
        var cpuCycleTicksPerSecond = cpuCycles * TimeSpan.TicksPerSecond + StepCycleTicksPerSecondRemainder;
        var currentStepTicksElapsed = cpuCycleTicksPerSecond / cpuClockHz;
        // Note: CycleTicksPerSecond is basically a "sub-tick" unit.
        StepCycleTicksPerSecondRemainder = cpuCycleTicksPerSecond % cpuClockHz;
        tickBaseTimer();

        return currentStepTicksElapsed;

        // Base timer is 14 bits. Its value is not accessible in the data memory space.
        // Doc seems to imply that the top 6-bits can be driven from something besides 8-bit counter overflow.
        // However, it's not clear how to do that.
        void tickBaseTimer()
        {
            var btcr = SFRs.Btcr;
            var isl = SFRs.Isl;

            var cyclesPerSecond = isl.BaseTimerClock switch
            {
                BaseTimerClock.QuartzOscillator => OscillatorHz.Quartz,
                // TODO: not supported right now. Possibly never; well formed software should not use the below modes.
                // though, all software really needs to do is ensure the bios tick function is called every 0.5s. there are probably different ways to accomplish that. So who knows.
                BaseTimerClock.T0Prescaler => OscillatorHz.Quartz,
                BaseTimerClock.CycleClock => cpuClockHz,
                _ => throw new InvalidOperationException()
            };

            BaseTimerTicksRemaining += currentStepTicksElapsed;
            var ticksPerCycle = TimeSpan.TicksPerSecond / cyclesPerSecond;
            var timerCyclesElapsed = BaseTimerTicksRemaining / ticksPerCycle;
            BaseTimerTicksRemaining = BaseTimerTicksRemaining % ticksPerCycle;

            var currentBtTicks = BaseTimer;
            var newBtTicks = (ushort)(currentBtTicks + timerCyclesElapsed);

            var int1Rate = btcr.Int1CycleRate;
            Debug.Assert(BitHelpers.IsPowerOfTwo(int1Rate));

            // If the new ticks caused us to divide int1Rate an additional time, Int1 is generated (if enabled).
            // TODO: dividing all the time seems like a funky way to do this.
            if ((currentBtTicks / int1Rate) < (newBtTicks / int1Rate))
            {
                btcr.Int1Source = true;
                if (btcr.Int1Enable)
                {
                    Logger.LogDebug("Requesting BTInt1", LogCategories.Interrupts);
                    RequestedInterrupts |= Interrupts.INT3_BT;
                }
            }

            var int0Rate = btcr.Int0CycleRate;
            Debug.Assert(BitHelpers.IsPowerOfTwo(int0Rate));
            // If the new ticks caused us to divide int0Rate an additional time, Int0 is generated (if enabled).
            if ((currentBtTicks / int0Rate) < (newBtTicks / int0Rate))
            {
                btcr.Int0Source = true;
                if (btcr.Int0Enable)
                {
                    Logger.LogDebug("Requesting BTInt0", LogCategories.Interrupts);
                    RequestedInterrupts |= Interrupts.INT3_BT;
                }

                // Hardware manual mentions that both Int0 and Int1 are generated in this case.
                // This might just be because int0Rate is evenly divisible by int1Rate.
                // so, it seems like we shouldn't have to generate Int1 here.
            }

            BaseTimer = (ushort)(newBtTicks % BaseTimerMax);
            SFRs.Btcr = btcr;
        }
    }

    /// <returns>Number of CPU cycles consumed by the instruction.</returns>
    internal int Step()
    {
        ServiceInterruptIfNeeded();
        AdvanceInterruptState();

        // TODO: hold mode doesn't even tick timers. only external interrupts wake the VMU.
        if (SFRs.Pcon.HaltMode)
        {
            tickCpuClockedTimers(1);
            checkContinuousSignals();
            return 1;
        }

        var inst = InstructionDecoder.Decode(CurrentROMBank, Pc);
        InstructionMap[InstructionBank, Pc] = inst;
        Logger.LogTrace($"{inst} Acc={SFRs.Acc:X} B={SFRs.B:X} C={SFRs.C:X} R2={ReadRam(2)}");
        switch (inst.Kind)
        {
            case OperationKind.ADD: Op_ADD(inst); break;
            case OperationKind.ADDC: Op_ADDC(inst); break;
            case OperationKind.SUB: Op_SUB(inst); break;
            case OperationKind.SUBC: Op_SUBC(inst); break;
            case OperationKind.INC: Op_INC(inst); break;
            case OperationKind.DEC: Op_DEC(inst); break;
            case OperationKind.MUL: Op_MUL(inst); break;
            case OperationKind.DIV: Op_DIV(inst); break;
            case OperationKind.AND: Op_AND(inst); break;
            case OperationKind.OR: Op_OR(inst); break;
            case OperationKind.XOR: Op_XOR(inst); break;
            case OperationKind.ROL: Op_ROL(inst); break;
            case OperationKind.ROLC: Op_ROLC(inst); break;
            case OperationKind.ROR: Op_ROR(inst); break;
            case OperationKind.RORC: Op_RORC(inst); break;
            case OperationKind.LD: Op_LD(inst); break;
            case OperationKind.ST: Op_ST(inst); break;
            case OperationKind.MOV: Op_MOV(inst); break;
            case OperationKind.LDC: Op_LDC(inst); break;
            case OperationKind.PUSH: Op_PUSH(inst); break;
            case OperationKind.POP: Op_POP(inst); break;
            case OperationKind.XCH: Op_XCH(inst); break;
            case OperationKind.JMP: Op_JMP(inst); break;
            case OperationKind.JMPF: Op_JMPF(inst); break;
            case OperationKind.BR: Op_BR(inst); break;
            case OperationKind.BRF: Op_BRF(inst); break;
            case OperationKind.BZ: Op_BZ(inst); break;
            case OperationKind.BNZ: Op_BNZ(inst); break;
            case OperationKind.BP: Op_BP(inst); break;
            case OperationKind.BPC: Op_BPC(inst); break;
            case OperationKind.BN: Op_BN(inst); break;
            case OperationKind.DBNZ: Op_DBNZ(inst); break;
            case OperationKind.BE: Op_BE(inst); break;
            case OperationKind.BNE: Op_BNE(inst); break;
            case OperationKind.CALL: Op_CALL(inst); break;
            case OperationKind.CALLF: Op_CALLF(inst); break;
            case OperationKind.CALLR: Op_CALLR(inst); break;
            case OperationKind.RET: Op_RET(inst); break;
            case OperationKind.RETI: Op_RETI(inst); break;
            case OperationKind.CLR1: Op_CLR1(inst); break;
            case OperationKind.SET1: Op_SET1(inst); break;
            case OperationKind.NOT1: Op_NOT1(inst); break;
            case OperationKind.LDF: Op_LDF(inst); break;
            case OperationKind.STF: Op_STF(inst); break;
            case OperationKind.NOP: Op_NOP(inst); break;
            default: Throw(inst); break;
        }

        tickCpuClockedTimers(inst.Cycles);
        checkContinuousSignals();
        return inst.Cycles;

        static void Throw(Instruction inst) => throw new InvalidOperationException($"Unknown operation '{inst}'");


        void tickCpuClockedTimers(byte cycles)
        {
            tickTimer0();
            tickTimer1();

            void tickTimer0()
            {
                var t0cnt = SFRs.T0Cnt;

                var scale = 0xff - SFRs.T0Prr;
                var ticks = (ushort)(cycles * scale);

                // tick t0l
                bool t0lOverflow = false;
                if (t0cnt.T0lRun)
                {
                    var t0l = (byte)(SFRs.T0L + ticks);
                    t0lOverflow = t0l < ticks;
                    if (t0lOverflow)
                    {
                        // TODO: I think we care about the remainder from the overflow
                        // but accounting for both that remainder and the reload data is not straightforward
                        // e.g. for a long instruction like mul we could probably end up reloading multiple times
                        t0l = SFRs.T0Lr; // reload
                        if (!t0cnt.T0Long) // track the overflow only in 8-bit mode
                        {
                            t0cnt.T0lOvf = true; // note: the hardware will not reset this flag. application needs to do it.
                            if (t0cnt.T0lIe)
                                RequestedInterrupts |= Interrupts.INT2_T0L;
                        }
                    }

                    SFRs.T0L = t0l;
                }

                // tick t0h
                if (t0cnt.T0hRun)
                {
                    var hticks = (t0cnt.T0Long, t0lOverflow) switch
                    {
                        (true, true) => 1,
                        (true, false) => 0,
                        _ => ticks
                    };
                    var t0h = (byte)(SFRs.T0H + hticks);
                    var t0hOverflow = t0h < hticks;
                    if (t0hOverflow)
                    {
                        t0h = SFRs.T0Hr;
                        t0cnt.T0hOvf = true; // note: the hardware will not reset this flag. application needs to do it.
                        if (t0cnt.T0hIe)
                            RequestedInterrupts |= Interrupts.T0H;
                    }

                    SFRs.T0H = t0h;
                }

                SFRs.T0Cnt = t0cnt;
            }

            void tickTimer1()
            {
                var t1cnt = SFRs.T1Cnt;
                var ticks = cycles; // TODO: how to tick at half the cycle clock accurately? break out a remainder bit?

                bool t1lOverflow = false;
                if (t1cnt.T1lRun)
                {
                    var t1l = (byte)(SFRs.T1L + ticks);
                    t1lOverflow = t1l < ticks;
                    if (t1lOverflow)
                    {
                        t1l = SFRs.T1Lr;
                        t1cnt.T1lOvf = true;
                        if (t1cnt.T1lIe)
                            RequestedInterrupts |= Interrupts.T1;
                    }

                    if (t1cnt.ELDT1C)
                    {
                        // TODO: update pulse generator?
                        // because we run the cycles in bursts for each frame we want to display,
                        // not really at the rate of original hardware, we likely can't/shouldn't emulate sound this way.
                        // we need to instead inspect the comparison and timer setup data and just fill in a PWM buffer or something.
                    }

                    SFRs.T1L = t1l;
                }

                if (t1cnt.T1hRun)
                {
                    var hticks = (t1cnt.T1Long, t1lOverflow) switch
                    {
                        (true, true) => 1,
                        (true, false) => 0,
                        _ => ticks
                    };
                    var t1h = (byte)(SFRs.T1H + hticks);
                    var t1hOverflow = t1h < hticks;
                    if (t1hOverflow)
                    {
                        t1h = SFRs.T1Hr;
                        t1cnt.T1hOvf = true; // note: the hardware will not reset this flag. application needs to do it.
                        if (t1cnt.T1hIe)
                            RequestedInterrupts |= Interrupts.T1;
                    }

                    SFRs.T1H = t1h;
                }
            }
        }

        void checkContinuousSignals()
        {
            var p3int = SFRs.P3Int;
            // NB: non-continuous interrupts are generated in SFRs.P3.set (i.e. only when P3 changes)
            if (p3int.Enable && p3int.Continuous)
            {
                var p3Raw = (byte)SFRs.P3;
                if (p3Raw != 0xff)
                {
                    Logger.LogDebug($"Requesting interrupt P3 Continuous={p3int.Continuous} Value=0b{p3Raw:b}", LogCategories.Interrupts);
                    p3int.Source = true;
                    RequestedInterrupts |= Interrupts.P3;
                }
                SFRs.P3Int = p3int;
            }
        }
    }

    private byte FetchOperand(Parameter param, ushort arg)
    {
        return param.Kind switch
        {
            ParameterKind.I8 => (byte)arg,
            ParameterKind.D9 => Memory.Read(arg),
            ParameterKind.Ri => Memory.ReadIndirect(arg),
            _ => Throw()
        };

        byte Throw() => throw new InvalidOperationException($"Cannot fetch operand for parameter '{param}'");
    }

    private ushort GetOperandAddress(Parameter param, ushort arg)
    {
        return param.Kind switch
        {
            ParameterKind.D9 => arg,
            ParameterKind.Ri => Memory.ReadIndirectAddressRegister(arg),
            _ => Throw()
        };

        byte Throw() => throw new InvalidOperationException($"Cannot fetch address for parameter '{param}'");
    }

    private void Op_ADD(Instruction inst)
    {
        // ACC <- ACC + operand
        var rhs = FetchOperand(inst.Parameters[0], inst.Arg0);
        var lhs = SFRs.Acc;
        var result = (byte)(lhs + rhs);
        SFRs.Acc = result;

        var psw = SFRs.Psw;
        psw.Cy = result < lhs;
        psw.Ac = (lhs & 0xf) + (rhs & 0xf) > 0xf;

        // Overflow occurs if either:
        // - both operands had MSB set (i.e. were two's complement negative), but the result has the MSB cleared.
        // - both operands had MSB cleared (i.e. were two's complement positive), but the result has the MSB set.
        psw.Ov = (BitHelpers.ReadBit(lhs, bit: 7), BitHelpers.ReadBit(rhs, bit: 7), BitHelpers.ReadBit(result, bit: 7)) switch
        {
            (true, true, false) => true,
            (false, false, true) => true,
            _ => false
        };

        SFRs.Psw = psw;
        Pc += inst.Size;
    }

    private void Op_ADDC(Instruction inst)
    {
        // ACC <- ACC + CY + operand
        var rhs = FetchOperand(inst.Parameters[0], inst.Arg0);
        var lhs = SFRs.Acc;
        var psw = SFRs.Psw;
        var carry = psw.Cy ? 1 : 0;
        var result = (byte)(lhs + carry + rhs);
        SFRs.Acc = result;

        psw.Cy = result < lhs;
        psw.Ac = (lhs & 0xf) + carry + (rhs & 0xf) > 0xf;

        // Overflow occurs if either:
        // - both operands had MSB set (i.e. were two's complement negative), but the result has the MSB cleared.
        // - both operands had MSB cleared (i.e. were two's complement positive), but the result has the MSB set.
        psw.Ov = (BitHelpers.ReadBit((byte)(lhs + carry), bit: 7), BitHelpers.ReadBit(rhs, bit: 7), BitHelpers.ReadBit(result, bit: 7)) switch
        {
            (true, true, false) => true,
            (false, false, true) => true,
            _ => false
        };

        SFRs.Psw = psw;
        Pc += inst.Size;
    }

    private void Op_SUB(Instruction inst)
    {
        // ACC <- ACC - operand
        var rhs = FetchOperand(inst.Parameters[0], inst.Arg0);
        var lhs = SFRs.Acc;
        var result = (byte)(lhs - rhs);
        SFRs.Acc = result;

        var psw = SFRs.Psw;
        psw.Cy = lhs < rhs;
        psw.Ac = (lhs & 0xf) < (rhs & 0xf);

        // Overflow occurs if either:
        // - first operand has MSB set (negative number), second operand has MSB cleared (positive number), and the result has the MSB cleared (positive number).
        // - first operand has MSB cleared (positive number), second operand has MSB set (negative number), and the result has the MSB set (negative number).
        psw.Ov = (BitHelpers.ReadBit(lhs, bit: 7), BitHelpers.ReadBit(rhs, bit: 7), BitHelpers.ReadBit(result, bit: 7)) switch
        {
            (true, false, false) => true,
            (false, true, true) => true,
            _ => false
        };

        SFRs.Psw = psw;
        Pc += inst.Size;
    }

    private void Op_SUBC(Instruction inst)
    {
        // ACC <- ACC - CY - operand
        var rhs = FetchOperand(inst.Parameters[0], inst.Arg0);
        var lhs = SFRs.Acc;
        var psw = SFRs.Psw;
        var carry = psw.Cy ? 1 : 0;
        var result = (byte)(lhs - carry - rhs);
        SFRs.Acc = result;

        // Carry is set when the subtraction yields a negative result.
        psw.Cy = lhs - carry - rhs < 0;
        psw.Ac = (lhs & 0xf) - carry - (rhs & 0xf) < 0;

        // Overflow occurs if either:
        // - subtracting a negative changes the sign from negative to positive
        // - first operand has MSB set (negative number), second operand has MSB cleared (positive number), and the result has the MSB cleared (positive number).
        // - first operand has MSB cleared (positive number), second operand has MSB set (negative number), and the result has the MSB set (negative number).
        psw.Ov = (BitHelpers.ReadBit((byte)(lhs + carry), bit: 7), BitHelpers.ReadBit(rhs, bit: 7), BitHelpers.ReadBit(result, bit: 7)) switch
        {
            (true, false, false) => true,
            (false, true, true) => true,
            _ => false
        };

        SFRs.Psw = psw;
        Pc += inst.Size;
    }

    private void Op_INC(Instruction inst)
    {
        // (operand) <- (operand) + 1
        // (could be either direct or indirect)
        var address = GetOperandAddress(inst.Parameters[0], inst.Arg0);
        var operand = ReadRam(address);
        operand++;
        WriteRam(address, operand);
        Pc += inst.Size;
    }

    private void Op_DEC(Instruction inst)
    {
        // (operand) <- (operand) - 1
        // (could be either direct or indirect)
        var address = GetOperandAddress(inst.Parameters[0], inst.Arg0);
        var operand = ReadRam(address);
        operand--;
        WriteRam(address, operand);
        Pc += inst.Size;
    }

    private void Op_MUL(Instruction inst)
    {
        // (B) (ACC) (C) <- (ACC) (C) * (B)
        int result = (SFRs.Acc << 0x8 | SFRs.C) * SFRs.B;
        SFRs.B = (byte)(result >> 0x10); // Casting to byte just takes the 8 least significant bits of the expression
        SFRs.Acc = (byte)(result >> 0x8);
        SFRs.C = (byte)result;

        // Overflow cleared indicates the result can fit into 16 bits, i.e. B is 0.
        SFRs.Psw = SFRs.Psw with { Ov = SFRs.B != 0, Cy = false };
        Pc += inst.Size;
    }

    private void Op_DIV(Instruction inst)
    {
        // (ACC) (C), mod(B) <- (ACC) (C) / (B)
        var psw = SFRs.Psw;
        if (SFRs.B == 0)
        {
            SFRs.Acc = 0xff;
            psw.Ov = true;
        }
        else
        {
            int lhs = SFRs.Acc << 0x8 | SFRs.C;
            int result = lhs / SFRs.B;
            int mod = lhs % SFRs.B;

            SFRs.Acc = (byte)(result >> 0x8);
            SFRs.C = (byte)result;
            SFRs.B = (byte)mod;
            psw.Ov = false;
        }
        psw.Cy = false;
        SFRs.Psw = psw;
        Pc += inst.Size;
    }

    private void Op_AND(Instruction inst)
    {
        // ACC <- ACC & operand
        var rhs = FetchOperand(inst.Parameters[0], inst.Arg0);
        SFRs.Acc &= rhs;
        Pc += inst.Size;
    }


    private void Op_OR(Instruction inst)
    {
        // ACC <- ACC | operand
        var rhs = FetchOperand(inst.Parameters[0], inst.Arg0);
        SFRs.Acc |= rhs;
        Pc += inst.Size;
    }
    private void Op_XOR(Instruction inst)
    {
        // ACC <- ACC ^ operand
        var rhs = FetchOperand(inst.Parameters[0], inst.Arg0);
        SFRs.Acc ^= rhs;
        Pc += inst.Size;
    }

    private void Op_ROL(Instruction inst)
    {
        // <-A7<-A6<-A5<-A4<-A3<-A2<-A1<-A0
        int shifted = SFRs.Acc << 1;
        bool bit0 = (shifted & 0x100) != 0;
        SFRs.Acc = (byte)(shifted | (bit0 ? 1 : 0));
        Pc += inst.Size;
    }

    private void Op_ROLC(Instruction inst)
    {
        // <-A7<-A6<-A5<-A4<-A3<-A2<-A1<-A0<-CY<- (A7)
        var psw = SFRs.Psw;
        int shifted = SFRs.Acc << 1 | (psw.Cy ? 1 : 0);
        psw.Cy = (shifted & 0x100) != 0;
        SFRs.Acc = (byte)shifted;
        SFRs.Psw = psw;
        Pc += inst.Size;
    }

    private void Op_ROR(Instruction inst)
    {
        // (A0) ->A7->A6->A5->A4->A3->A2->A1->A0
        bool bit7 = (SFRs.Acc & 1) != 0;
        SFRs.Acc = (byte)((SFRs.Acc >> 1) | (bit7 ? 0x80 : 0));
        Pc += inst.Size;
    }


    private void Op_RORC(Instruction inst)
    {
        // (A0) ->CY->A7->A6->A5->A4->A3->A2->A1->A0
        bool newCarry = (SFRs.Acc & 1) != 0;
        var psw = SFRs.Psw;
        SFRs.Acc = (byte)((psw.Cy ? 0x80 : 0) | SFRs.Acc >> 1);
        psw.Cy = newCarry;
        SFRs.Psw = psw;
        Pc += inst.Size;
    }

    private void Op_LD(Instruction inst)
    {
        // (ACC) <- (d9)
        SFRs.Acc = FetchOperand(inst.Parameters[0], inst.Arg0);
        Pc += inst.Size;
    }

    private void Op_ST(Instruction inst)
    {
        // (d9) <- (ACC)
        var address = GetOperandAddress(inst.Parameters[0], inst.Arg0);
        WriteRam(address, SFRs.Acc);
        Pc += inst.Size;
    }

    private void Op_MOV(Instruction inst)
    {
        // MOV i8,d9
        // (d9) <- #i8
        // ((Ri)) <- #i8
        var i8 = (byte)inst.Arg0;
        var address = GetOperandAddress(inst.Parameters[1], inst.Arg1);
        WriteRam(address, i8);
        Pc += inst.Size;
    }

    private void Op_LDC(Instruction inst)
    {
        // (ACC) <- (BNK)((TRR) + (ACC)) [ROM]
        // For a program running in ROM, ROM is accessed.
        // For a program running in flash memory, bank 0 of flash memory is accessed.
        // TODO: Cannot access bank 1 of flash memory. System BIOS function must be used instead.
        var address = ((SFRs.Trh << 8) | SFRs.Trl) + SFRs.Acc;
        SFRs.Acc = CurrentROMBank[address];
        Pc += inst.Size;
    }

    private void Op_PUSH(Instruction inst)
    {
        // (SP) <- (SP) + 1, ((SP)) <- (d9)
        Memory.PushStack(FetchOperand(inst.Parameters[0], inst.Arg0));
        Pc += inst.Size;
    }

    private void Op_POP(Instruction inst)
    {
        // (d9) <- ((SP)), (SP) <- (SP) - 1
        var dAddress = GetOperandAddress(inst.Parameters[0], inst.Arg0);
        WriteRam(dAddress, Memory.PopStack());

        Pc += inst.Size;
    }

    private void Op_XCH(Instruction inst)
    {
        // (ACC) <--> (d9)
        // (ACC) <--> ((Rj)) j = 0, 1, 2, 3
        var address = GetOperandAddress(inst.Parameters[0], inst.Arg0);
        var temp = ReadRam(address);
        WriteRam(address, SFRs.Acc);
        SFRs.Acc = temp;

        Pc += inst.Size;
    }

    /// <summary>Jump near absolute address</summary>
    private void Op_JMP(Instruction inst)
    {
        // (PC) <- (PC) + 2, (PC11 to 00) <- a12
        ushort a12 = inst.Arg0;
        Pc += 2;
        Pc &= 0b1111_0000__0000_0000;
        Pc |= a12;
    }

    /// <summary>Jump far absolute address</summary>
    private void Op_JMPF(Instruction inst)
    {
        // (PC) <- a16
        Pc = inst.Arg0;

        // Now update the instruction bank for real, which may have been initiated by a previous instruction
        SyncInstructionBank();
    }

    /// <summary>Branch near relative address</summary>
    private void Op_BR(Instruction inst)
    {
        // (PC) <- (PC) + 2, (PC) <- (PC) + r8
        var r8 = (sbyte)inst.Arg0;
        Pc = (ushort)(Pc + inst.Size + r8);
    }

    /// <summary>Branch far relative address</summary>
    private void Op_BRF(Instruction inst)
    {
        // (PC) <- (PC) + 3, (PC) <- (PC) - 1 + r16
        var r16 = inst.Arg0;
        Pc = (ushort)(Pc + inst.Size - 1 + r16);
    }

    /// <summary>Branch near relative address if accumulator is zero</summary>
    private void Op_BZ(Instruction inst)
    {
        // (PC) <- (PC) + 2, if (ACC) = 0 then (PC) <- PC + r8
        var r8 = (sbyte)inst.Arg0;
        var z = SFRs.Acc == 0;

        Pc += inst.Size;
        if (z)
            Pc = (ushort)(Pc + r8);
    }

    /// <summary>Branch near relative address if accumulator is not zero</summary>
    private void Op_BNZ(Instruction inst)
    {
        // (PC) <- (PC) + 2, if (ACC) != 0 then (PC) <- PC + r8
        var r8 = (sbyte)inst.Arg0;
        var nz = SFRs.Acc != 0;

        Pc += inst.Size;
        if (nz)
            Pc = (ushort)(Pc + r8);
    }

    /// <summary>Branch near relative address if direct bit is one ("positive")</summary>
    private void Op_BP(Instruction inst)
    {
        // (PC) <- (PC) + 3, if (d9, b3) = 1 then (PC) <- (PC) + r8
        var d9 = inst.Arg0;
        var b3 = (byte)inst.Arg1;
        var r8 = (sbyte)inst.Arg2;

        Pc += 3;
        if (BitHelpers.ReadBit(ReadRam(d9), b3))
            Pc = (ushort)(Pc + r8);
    }

    /// <summary>Branch near relative address if direct bit is one ("positive"), and clear</summary>
    private void Op_BPC(Instruction inst)
    {
        // When applied to port P1 and P3, the latch of each port is selected. The external signal is not selected.
        // When applied to port P7, there is no change in status.

        // (PC) <- (PC) + 3, if (d9, b3) = 1 then (PC) <- (PC) + r8, (d9, b3) = 0
        var d9 = inst.Arg0;
        var b3 = (byte)inst.Arg1;
        var r8 = (sbyte)inst.Arg2;

        var d_value = ReadRam(d9);
        var new_d_value = d_value;
        BitHelpers.WriteBit(ref new_d_value, bit: b3, value: false);

        Pc += inst.Size;
        if (d_value != new_d_value)
            Pc = (ushort)(Pc + r8);

        WriteRam(d9, new_d_value);
    }

    /// <summary>Branch near relative address if direct bit is zero ("negative")</summary>
    private void Op_BN(Instruction inst)
    {
        // (PC) <- (PC) + 3, if (d9, b3) = 0 then (PC) <- (PC) + r8
        var d9 = inst.Arg0;
        var b3 = (byte)inst.Arg1;
        var r8 = (sbyte)inst.Arg2;

        Pc += inst.Size;
        if (!BitHelpers.ReadBit(ReadRam(d9), b3))
            Pc = (ushort)(Pc + r8);
    }

    /// <summary>Decrement direct byte and branch near relative address if direct byte is nonzero</summary>
    private void Op_DBNZ(Instruction inst)
    {
        // (PC) <- (PC) + 3, (d9) = (d9)-1, if (d9) != 0 then (PC) <- (PC) + r8
        // (PC) <- (PC) + 3, ((Ri)) = ((Ri))-1, if ((Ri)) != 0 then (PC) <- (PC) + r8
        var address = GetOperandAddress(inst.Parameters[0], inst.Arg0);
        var value = ReadRam(address);
        var r8 = (sbyte)inst.Arg1;

        --value;
        WriteRam(address, value);

        Pc += inst.Size;
        if (value != 0)
            Pc = (ushort)(Pc + r8);
    }

    /// <summary>
    /// - Compare immediate data or direct byte to accumulator and branch near relative address if equal
    /// - Compare immediate data to indirect byte and branch near relative address if equal
    /// </summary>
    private void Op_BE(Instruction inst)
    {
        // (PC) <- (PC) + 3, if (ACC) == #i8 then (PC) <- (PC) + r8
        // (PC) <- (PC) + 3, if (ACC) == d9 then (PC) <- (PC) + r8
        // (PC) <- (PC) + 3, if ((Ri)) == #i8 then (PC) <- (PC) + r8
        var param0 = inst.Parameters[0];
        var indirectMode = param0.Kind == ParameterKind.Ri;
        var (lhs, rhs, r8) = indirectMode
            ? (lhs: Memory.ReadIndirect(inst.Arg0), rhs: inst.Arg1, r8: (sbyte)inst.Arg2)
            : (lhs: SFRs.Acc, rhs: FetchOperand(param0, inst.Arg0), r8: (sbyte)inst.Arg1);

        Pc += inst.Operation.Size;
        SFRs.Psw = SFRs.Psw with { Cy = lhs < rhs };
        if (lhs == rhs)
            Pc = (ushort)(Pc + r8);
    }

    /// <summary>
    /// - Compare immediate data or direct byte to accumulator and branch near relative address if not equal
    /// - Compare immediate data to indirect byte and branch near relative address if not equal
    /// </summary>
    private void Op_BNE(Instruction inst)
    {
        // (PC) <- (PC) + 3, if (ACC) != #i8 then (PC) <- (PC) + r8
        // (PC) <- (PC) + 3, if (ACC) != d9 then (PC) <- (PC) + r8
        // (PC) <- (PC) + 3, if ((Ri)) != #i8 then (PC) <- (PC) + r8
        var param0 = inst.Parameters[0];
        var indirectMode = param0.Kind == ParameterKind.Ri;
        var (lhs, rhs, r8) = indirectMode
            ? (lhs: Memory.ReadIndirect(inst.Arg0), rhs: inst.Arg1, r8: (sbyte)inst.Arg2)
            : (lhs: SFRs.Acc, rhs: FetchOperand(param0, inst.Arg0), r8: (sbyte)inst.Arg1);

        Pc += inst.Operation.Size;
        SFRs.Psw = SFRs.Psw with { Cy = lhs < rhs };
        if (lhs != rhs)
            Pc = (ushort)(Pc + r8);
    }

    /// <summary>Near absolute subroutine call</summary>
    private void Op_CALL(Instruction inst)
    {
        // similar to OP_JMP
        // (PC) <- (PC) + 2, (SP) <- (SP) + 1, ((SP)) <- (PC7 to 0), (SP) <- (SP) + 1, ((SP)) <- (PC15 to 8), (PC11 to 00) <- a12
        // 000a_1aaa aaaa_aaaa

        ushort a12 = inst.Arg0;

        Pc += inst.Size;
        Memory.PushStack((byte)Pc);
        Memory.PushStack((byte)(Pc >> 8));

        Pc &= 0b1111_0000__0000_0000;
        Pc |= a12;
    }

    /// <summary>Far absolute subroutine call</summary>
    private void Op_CALLF(Instruction inst)
    {
        // Similar to Op_JMPF
        // (PC) <- (PC) + 3, (SP) <- (SP) + 1, ((SP)) <- (PC7 to 0),
        // (SP) <- (SP) + 1, ((SP)) <- (PC15 to 8), (PC) <- a16
        var a16 = inst.Arg0;
        Pc += 3;
        Memory.PushStack((byte)Pc);
        Memory.PushStack((byte)(Pc >> 8));
        Pc = a16;
    }

    /// <summary>Far relative subroutine call</summary>
    private void Op_CALLR(Instruction inst)
    {
        // (PC) <- (PC) + 3, (SP) <- (SP) + 1, ((SP)) <- (PC7 to 0),
        // (SP) <- (SP) + 1, ((SP)) <- (PC15 to 8), (PC) <- (PC) - 1 + r16
        var r16 = inst.Arg0;
        Pc += inst.Size;
        Memory.PushStack((byte)Pc);
        Memory.PushStack((byte)(Pc >> 8));
        Pc = (ushort)(Pc - 1 + r16);
    }

    /// <summary>Return from subroutine</summary>
    private void Op_RET(Instruction inst)
    {
        // (PC15 to 8) <- ((SP)), (SP) <- (SP) - 1, (PC7 to 0) <- ((SP)), (SP) <- (SP) -1
        var Pc15_8 = Memory.PopStack();
        var Pc7_0 = Memory.PopStack();
        Pc = (ushort)(Pc15_8 << 8 | Pc7_0);
    }

    /// <summary>Return from interrupt</summary>
    private void Op_RETI(Instruction inst)
    {
        // (PC15 to 8) <- ((SP)), (SP) <- (SP) - 1, (PC7 to 0) <- ((SP)), (SP) <- (SP) -1
        _interruptServicingState = InterruptServicingState.ReturnedFromInterrupt;
        if (_interruptsCount > 0)
            _interruptsCount--;
        else
            Logger.LogError($"Returning from interrupt, but no interrupt was being serviced!", LogCategories.Interrupts);

        var Pc15_8 = Memory.PopStack();
        var Pc7_0 = Memory.PopStack();
        Pc = (ushort)(Pc15_8 << 8 | Pc7_0);
    }

    /// <summary>Clear direct bit</summary>
    private void Op_CLR1(Instruction inst)
    {
        // (d9, b3) <- 0
        var d9 = inst.Arg0;
        var b3 = (byte)inst.Arg1;
        var memory = ReadRam(d9);
        BitHelpers.WriteBit(ref memory, bit: b3, value: false);
        WriteRam(d9, memory);
        Pc += inst.Size;
    }

    /// <summary>Set direct bit</summary>
    private void Op_SET1(Instruction inst)
    {
        // (d9, b3) <- 1
        var d9 = inst.Arg0;
        var b3 = (byte)inst.Arg1;
        var memory = ReadRam(d9);
        BitHelpers.WriteBit(ref memory, bit: b3, value: true);
        WriteRam(d9, memory);
        Pc += inst.Size;
    }

    /// <summary>Not direct bit</summary>
    private void Op_NOT1(Instruction inst)
    {
        // (d9, b3) <- !(d9, b3)
        var d9 = inst.Arg0;
        var b3 = (byte)inst.Arg1;
        var memory = ReadRam(d9);
        var bit = BitHelpers.ReadBit(memory, b3);
        BitHelpers.WriteBit(ref memory, bit: b3, value: !bit);
        WriteRam(d9, memory);
        Pc += inst.Size;
    }

    /// <summary>Load a value from flash memory into accumulator. Undocumented.</summary>
    private void Op_LDF(Instruction inst)
    {
        var a16 = SFRs.Trl | (SFRs.Trh << 8);
        var bank = SFRs.FPR.FPR0 ? FlashBank1 : FlashBank0;
        SFRs.Acc = bank[a16];
        Pc += inst.Size;
    }

    /// <summary>Store the accumulator to flash memory. Intended for use only by BIOS. Undocumented.</summary>
    private void Op_STF(Instruction inst)
    {
        // TODO: emulate hardware unlock sequence
        if (InstructionBank != InstructionBank.ROM)
            Logger.LogWarning("Executing STF outside of ROM!");

        var a16 = SFRs.Trl | (SFRs.Trh << 8);
        var bank = SFRs.FPR.FPR0 ? FlashBank1 : FlashBank0;
        bank[a16] = SFRs.Acc;
        Pc += inst.Size;
    }

    // OP_STF

    /// <summary>No operation</summary>
    private void Op_NOP(Instruction inst)
    {
        Pc += inst.Size;
    }
}
