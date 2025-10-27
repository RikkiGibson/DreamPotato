using System.Buffers.Binary;
using System.Diagnostics;

using DreamPotato.Core.SFRs;

// note that these also work on unix despite the name
using Microsoft.Win32.SafeHandles;

namespace DreamPotato.Core;

public class Cpu
{
    public Logger Logger { get; }

    // VMD-35: Accumulator and all registers are mapped to RAM.
    // VMD-38: Memory

    internal const int InstructionBankSize = 64 * 1024;
    internal const int FlashBankSize = InstructionBankSize;
    internal const int FlashSize = FlashBankSize * 2;

    // !! NOTE !!
    // All state which is added needs to be handled appropriately in Reset, SaveState, LoadState, and similar methods.

    /// <summary>Read-only memory space.</summary>
    public readonly byte[] ROM = new byte[InstructionBankSize];
    public readonly byte[] Flash = new byte[FlashSize];
    internal Span<byte> FlashBank0 => Flash.AsSpan(0, FlashBankSize);
    internal Span<byte> FlashBank1 => Flash.AsSpan(FlashBankSize, FlashBankSize);

#if DEBUG
    internal readonly InstructionMap InstructionMap = new();
#endif

    internal event Action? UnsavedChangesDetected;
    internal bool HasUnsavedChanges
    {
        get;
        set
        {
            var isNewUnsavedChanges = !field && value;
            field = value;
            if (isNewUnsavedChanges)
                UnsavedChangesDetected?.Invoke();
        }
    }

    internal FileStream? VmuFileWriteStream
    {
        private get;
        set
        {
            field?.Dispose();
            field = value;
        }
    }

    /// <summary>
    /// Allows reading/writing to the VMU file in a thread-safe manner.
    /// </summary>
    internal SafeFileHandle? VmuFileHandle
        => VmuFileWriteStream?.SafeFileHandle;

    /// <summary>
    /// May point to either ROM (BIOS), flash memory bank 0 or bank 1.
    /// </summary>
    /// <remarks>
    /// Note that we need an extra bit of state here. We can't just look at the value of <see cref="SpecialFunctionRegisters.Ext"/>.
    /// The bank is only actually switched when using a jmpf instruction.
    /// </remarks>
    public Span<byte> CurrentROMBank => InstructionBank switch
    {
        InstructionBank.ROM => ROM,
        InstructionBank.FlashBank0 => FlashBank0,
        InstructionBank.FlashBank1 => FlashBank1,
        _ => throw new InvalidOperationException()
    };

    internal InstructionBank InstructionBank { get; private set; }

    public readonly Memory Memory;
    public readonly Audio Audio;
    public readonly Display Display;

    /// <summary>NOTE: only 'TryReceiveMessage' and 'SendMessage' methods are safe to call from here.</summary>
    public readonly MapleMessageBroker MapleMessageBroker;

    /// <summary>Decrements each frame and clears IO icon when reaching 0.</summary>
    /// <remarks>Irrelevant for save states because only used while docked.</remarks>
    private int MapleIOIconTimeout;

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

    /// <summary>
    /// This field tracks how far we are into the T0 prescaler count.
    /// The T0 prescaler is pretty much just a timer that sits in front of T0 and sends T0 a tick when it overflows.
    /// </summary>
    internal byte T0Scale;

    internal Interrupts RequestedInterrupts;
    private InterruptServicingState _interruptServicingState;

    /// <summary>
    /// Maximum number of interrupts which can be consecutively serviced.
    /// When this limit is reached, further interrupts are not serviced
    /// until returning from the current interrupt service routine.
    /// </summary>
    private const int InterruptsCountMax = 3;

    // Note that even though Interrupts is a flags enum, we keep this as an array
    // because priority settings can make picking out the most/least recently serviced interrupt tricky
    internal readonly Interrupts[] _servicingInterrupts = new Interrupts[3];
    internal int _interruptsCount;

    internal int _flashWriteUnlockSequence;

    /// <summary>The CPU of another VMU, connected for serial I/O.</summary>
    private Cpu? _otherCpu;

    // TODO: update save state format
    // Now, loading state can change the used expansion slots, in addition to changing whether we are docked.
    internal DreamcastSlot DreamcastSlot
    {
        get;
        set
        {
            if (value is not (DreamcastSlot.Slot1 or DreamcastSlot.Slot2))
                throw new InvalidOperationException("VMU must be associated with a single slot");

            if (SFRs.P7.DreamcastConnected)
                throw new InvalidOperationException("VMU must be ejected when changing slots");

            field = value;
        }
    }

    public Cpu(MapleMessageBroker? mapleMessageBroker = null)
    {
        var categories = LogCategories.General;
        Logger = new Logger(LogLevel.Default, categories, this);
        Memory = new Memory(this, Logger);
        Audio = new Audio(this, Logger);
        Display = new Display(this);
        MapleMessageBroker = mapleMessageBroker ?? new MapleMessageBroker(LogLevel.Default);
        SetInstructionBank(InstructionBank.ROM);
    }

    public void Reset()
    {
        Pc = 0;
        TicksOverrun = 0;
        StepCycleTicksPerSecondRemainder = 0;
        BaseTimer = 0;
        BaseTimerTicksRemaining = 0;
        RequestedInterrupts = Interrupts.None;
        Array.Clear(_servicingInterrupts);
        _interruptsCount = 0;
        _interruptServicingState = InterruptServicingState.Ready;
        _flashWriteUnlockSequence = 0;
        Memory.Reset();
#if DEBUG
        InstructionMap.Clear();
#endif
        SyncInstructionBank();
    }

    internal void SaveState(Stream writeStream)
    {
        if (SFRs.P7.DreamcastConnected)
            ResyncMapleInbound();

        // NOTE: both save and load operations should write/read the fields in declaration order.
        writeStream.Write(ROM);
        writeStream.Write(Flash);
        writeStream.WriteByte((byte)InstructionBank);
        Memory.SaveState(writeStream);

        Span<byte> buffer = [0, 0, 0, 0, 0, 0, 0, 0];
        writeUInt16(buffer, Pc);
        writeInt64(buffer, TicksOverrun);
        writeInt64(buffer, StepCycleTicksPerSecondRemainder);
        writeUInt16(buffer, BaseTimer);
        writeInt64(buffer, BaseTimerTicksRemaining);
        writeStream.WriteByte(T0Scale);
        writeUInt16(buffer, (ushort)RequestedInterrupts);
        writeStream.WriteByte((byte)_interruptServicingState);
        for (int i = 0; i < _servicingInterrupts.Length; i++)
        {
            writeUInt16(buffer, (ushort)_servicingInterrupts[i]);
        }
        writeInt32(buffer, _interruptsCount);
        writeInt32(buffer, _flashWriteUnlockSequence);

        void writeUInt16(Span<byte> bytes, ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
            writeStream.Write(bytes[0..2]);
        }

        void writeInt32(Span<byte> bytes, int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
            writeStream.Write(bytes[0..4]);
        }

        void writeInt64(Span<byte> bytes, long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
            writeStream.Write(bytes[0..8]);
        }
    }

    internal void LoadState(Stream readStream)
    {
        // TODO: do some kind of search of InstructionMap
        // to remove instructions which are no longer present in the binary?

        // TODO: We can't restore the "VMUs connected" state in general.
        // This is because a save state is local to one VMU, but the "connection" involves them both.

        readStream.ReadExactly(ROM);
        readStream.ReadExactly(Flash);
        if (VmuFileHandle is not null)
            RandomAccess.Write(VmuFileHandle, Flash, fileOffset: 0);

        InstructionBank = (InstructionBank)readStream.ReadByte();
        Memory.LoadState(readStream);

        Span<byte> buffer = [0, 0, 0, 0, 0, 0, 0, 0];
        Pc = readUInt16(buffer);
        TicksOverrun = readInt64(buffer);
        StepCycleTicksPerSecondRemainder = readInt64(buffer);
        BaseTimer = readUInt16(buffer);
        BaseTimerTicksRemaining = readInt64(buffer);
        T0Scale = (byte)readStream.ReadByte();
        RequestedInterrupts = (Interrupts)readUInt16(buffer);
        _interruptServicingState = (InterruptServicingState)readStream.ReadByte();
        for (int i = 0; i < _servicingInterrupts.Length; i++)
        {
            _servicingInterrupts[i] = (Interrupts)readUInt16(buffer);
        }
        _interruptsCount = readInt32(buffer);
        _flashWriteUnlockSequence = readInt32(buffer);

        ResyncMapleOutbound();

        ushort readUInt16(Span<byte> bytes)
        {
            readStream.ReadExactly(bytes[0..2]);
            return BinaryPrimitives.ReadUInt16LittleEndian(bytes);
        }

        int readInt32(Span<byte> bytes)
        {
            readStream.ReadExactly(bytes[0..4]);
            return BinaryPrimitives.ReadInt32LittleEndian(bytes);
        }

        long readInt64(Span<byte> bytes)
        {
            readStream.ReadExactly(bytes[0..8]);
            return BinaryPrimitives.ReadInt64LittleEndian(bytes);
        }
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
        if (SFRs.P7.VmuConnected && _otherCpu is null)
        {
            // If this errors when there isn't a real connection scenario taking place, then, there might be a need to distinguish
            // the pin signal from latch, in case some software out there manually writes a high value into the latch.
            Logger.LogError($"Cannot run this Cpu directly because it is the client in a VMU-to-VMU connection.");
            return -1;
        }

        HandleMapleMessages();

        if (SFRs.P7.DreamcastConnected)
        {
            // While connected to Dreamcast, we do not execute any VMU instructions.
            // Instead HandleMapleMessages handles everything that should be happening while in this state.
            // TODO: we should at least keep ticking the base timer in this state.
            return ticksToRun;
        }
        else if (_otherCpu is not null)
        {
            return runBoth(this, _otherCpu, ticksToRun);
        }

        // Reduce the number of ticks we were asked to run, by the amount we overran last frame.
        ticksToRun -= TicksOverrun;

        long ticksSoFar = 0;
        while (ticksSoFar < ticksToRun)
        {
            ticksSoFar += StepTicks();
        }
        TicksOverrun = ticksSoFar - ticksToRun;

        return ticksSoFar;

        static long runBoth(Cpu @this, Cpu other, long inputTicksToRun)
        {
            var thisTicksToRun = inputTicksToRun - @this.TicksOverrun;
            var otherTicksToRun = inputTicksToRun - other.TicksOverrun;
            while (thisTicksToRun > 0 || otherTicksToRun > 0)
            {
                // Execute one instruction for whichever VMU is further behind in time.
                if (thisTicksToRun > otherTicksToRun)
                    thisTicksToRun -= @this.StepTicks();
                else
                    otherTicksToRun -= other.StepTicks();
            }

            @this.TicksOverrun = -thisTicksToRun;
            other.TicksOverrun = -otherTicksToRun;

            // note: We don't rely on the return value of 'Run()' for precision.
            // Possibly it should be changed to return void.
            return inputTicksToRun - thisTicksToRun;
        }
    }

    /// <summary>If VMU just became docked, update Maple flash from Cpu; otherwise, if VMU just became undocked, update Cpu flash from Maple.</summary>
    internal void ResyncMaple()
    {
        bool vmuDocked = SFRs.P7.DreamcastConnected;
        if (vmuDocked)
        {
            // When the VMU transitions from non-docked to docked,
            // let the CPU run for one second of emulated time.
            // This does 2 things:
            // 1) clears the screen.
            // 2) puts the BIOS into a state where it is able to detect the docked to non-docked transition later on.
            // Alternatively we could just let the CPU continue to run while docked,
            // but that has significant negative impact on host CPU utilization.
            for (long ticks = 0; ticks < TimeSpan.TicksPerSecond;)
                ticks += StepTicks();
        }

        MapleMessageBroker.Resync(DreamcastSlot, vmuDocked, writeToMapleFlash: vmuDocked, Flash, VmuFileHandle);
    }

    /// <summary>Update the <see cref="MapleMessageBroker"/> from <see cref="Flash"/> regardless of whether VMU is currently docked.</summary>
    internal void ResyncMapleOutbound()
    {
        bool vmuDocked = SFRs.P7.DreamcastConnected;
        MapleMessageBroker.Resync(DreamcastSlot, vmuDocked, writeToMapleFlash: true, Flash, VmuFileHandle);
    }

    /// <summary>Update <see cref="Flash"/> from <see cref="MapleMessageBroker"/> regardless of whether VMU is currently docked.</summary>
    internal void ResyncMapleInbound()
    {
        bool vmuDocked = SFRs.P7.DreamcastConnected;
        MapleMessageBroker.Resync(DreamcastSlot, vmuDocked, writeToMapleFlash: false, Flash, VmuFileHandle);
    }

    private void HandleMapleMessages()
    {
        if (MapleIOIconTimeout > 0)
        {
            MapleIOIconTimeout--;
            if (MapleIOIconTimeout == 0)
                Memory.Direct_AccessXram2()[Display.FlashIconOffset] = 0;
        }

        while (MapleMessageBroker.TryReceiveCpuMessage(DreamcastSlot, out var message))
        {
            if (!SFRs.P7.DreamcastConnected)
            {
                Logger.LogWarning($"Ignoring Maple message while undocked: '({message.Type}, {message.Function})'", category: LogCategories.Maple);
                continue;
            }

            // Note that only message types which handle immediately user-facing components (e.g. LCD, buzzer) are handled here.
            // Other message types are handled in MapleMessageBroker directly.
            switch (message.Type, message.Function)
            {
                case (MapleMessageType.WriteBlock, MapleFunction.LCD):
                    handleWriteBlockLcd(message);
                    break;
                case (MapleMessageType.WriteBlock, MapleFunction.Storage):
                    // Indicate flash write
                    MapleIOIconTimeout = 5;
                    Memory.Direct_AccessXram2()[Display.FlashIconOffset] = (byte)Icons.Flash;
                    if (VmuFileHandle is null)
                        HasUnsavedChanges = true;

                    break;
                case (MapleMessageType.CompleteWrite, MapleFunction.Storage):
                    // Do nothing, allow timeout counter to turn off flash icon.
                    break;
                default:
                    Debug.Fail($"Unhandled Maple message '({message.Type}, {message.Function})'");
                    Logger.LogError($"Unhandled Maple message '({message.Type}, {message.Function})'", category: LogCategories.Maple);
                    break;
            }
        }

        void handleWriteBlockLcd(MapleMessage message)
        {
            var lcdWords = message.AdditionalWords.AsSpan(startIndex: 2);

            var xram0 = Memory.Direct_AccessXram0();
            int index = 0;
            for (int left = 0; left < Memory.XramBank01Size; left += 0x10)
            {
                // skip 4 dead display bytes
                for (int right = 0; right < 0xc; right++)
                    xram0[left | right] = getAdditionalByte(lcdWords, index++);
            }

            var xram1 = Memory.Direct_AccessXram1();
            for (int left = 0; left < Memory.XramBank01Size; left += 0x10)
            {
                for (int right = 0; right < 0xc; right++)
                    xram1[left | right] = getAdditionalByte(lcdWords, index++);
            }

            static byte getAdditionalByte(ReadOnlySpan<int> additionalWords, int pos)
            {
                var i32 = additionalWords[pos / 4];
                var @byte = i32 >> (pos % 4 * 8) & 0xff;
                return (byte)@byte;
            }
        }
    }

    #region External interrupt triggers
    /// <summary>
    /// Simulate INT0 (connecting VMU to Dreamcast)
    /// </summary>
    /// <param name="connect">true if connecting, false if disconnecting</param>
    internal void ConnectDreamcast(bool connect = true)
    {
        // We don't do these checks in the internal read/write because user code which writes these is treated as writing a latch.
        // An external interrupt is only supposed to be generated by an external signal.
        var oldP7 = SFRs.P7;
        SFRs.P7 = oldP7 with { DreamcastConnected = connect };

        // Peripheral connection state changed.
        if (oldP7.DreamcastConnected != connect)
            ResyncMaple();

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

    internal void ConnectVmu(Cpu otherCpu)
    {
        if (otherCpu == this)
            throw new InvalidOperationException();

        var thisP7 = SFRs.P7;
        var otherP7 = otherCpu.SFRs.P7;
        if (thisP7.VmuConnected || otherP7.VmuConnected)
            throw new InvalidOperationException("Already connected to a VMU");

        if (thisP7.DreamcastConnected || otherP7.DreamcastConnected)
            throw new InvalidOperationException("Already connected to a Dreamcast controller");

        SFRs.P7 = thisP7 with { VmuConnected = true };
        otherCpu.SFRs.P7 = otherP7 with { VmuConnected = true };
        _otherCpu = otherCpu;
    }

    internal void DisconnectVmu()
    {
        if (_otherCpu is null)
            throw new InvalidOperationException("Not connected to another VMU (note that the object reference is one-way)");

        SFRs.P7 = SFRs.P7 with { VmuConnected = false };
        _otherCpu.SFRs.P7 = _otherCpu.SFRs.P7 with { VmuConnected = false };
        _otherCpu = null;
    }

    /// <summary>
    /// Simulate INT1 (low voltage)
    /// </summary>
    /// <param name="voltageLevel">true if voltage is high; false if voltage is low</param>
    internal void ReportVoltage(bool voltageLevel = false)
    {
        var oldP7 = SFRs.P7;
        SFRs.P7 = oldP7 with { LowVoltage = voltageLevel };

        var i01cr = SFRs.I01Cr;
        var isLevelTriggered = i01cr.Int1LevelTriggered;
        var isHighTriggered = i01cr.Int1HighTriggered;

        // level trigger: just need to be at the right level to trigger it
        if (isLevelTriggered && (isHighTriggered == voltageLevel))
        {
            i01cr.Int1Source = true;
            if (i01cr.Int1Enable)
                RequestedInterrupts |= Interrupts.INT1;
        }

        // edge trigger: need to transition to the desired level to trigger it
        if (!isLevelTriggered && (oldP7.LowVoltage != voltageLevel) && (isHighTriggered == voltageLevel))
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

    internal void ResetInterruptState()
    {
        // TODO this is kind of a hack to ensure that interrupts are not serviced
        // until one instruction after master interrupt enable is set.
        // TODO write a test program specifically for investigating how this works properly.
        _interruptServicingState = InterruptServicingState.ReturnedFromInterrupt;
    }

    private void AdvanceInterruptState()
    {
        if (_interruptServicingState == InterruptServicingState.ReturnedFromInterrupt)
            _interruptServicingState = InterruptServicingState.Ready;
    }

    /// <summary>Execute a single instruction and tick the base timer.</summary>
    /// <returns>Number of ticks consumed by running the instruction.</returns>
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
                // Using the T0Prescaler is unsupported. Setting that mode on Isl will log a warning.
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
            }

            BaseTimer = (ushort)(newBtTicks % BaseTimerMax);
            SFRs.Btcr = btcr;
        }
    }

    /// <returns>Number of CPU cycles consumed by the instruction.</returns>
    internal int Step()
        => StepInstruction().Cycles;

    internal Instruction StepInstruction()
    {
        ServiceInterruptIfNeeded();
        AdvanceInterruptState();

        // TODO: hold mode doesn't even tick timers. only external interrupts wake the VMU.
        if (SFRs.Pcon.HaltMode)
        {
            tickCpuClockedTimers(1);
            requestLevelDrivenInterrupts();
            // TODO: we didn't really execute this (PC not incremented), but, we did consume 1 cycle.
            // We probably want a different type here or a HALT "pseudo-instruction".
            return new Instruction(Pc, Operations.NOP);
        }

        var inst = InstructionDecoder.Decode(CurrentROMBank, Pc);
#if DEBUG
        InstructionMap[InstructionBank, Pc] = inst;
#endif
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
        requestLevelDrivenInterrupts();
        return inst;

        static void Throw(Instruction inst) => throw new InvalidOperationException($"Unknown operation '{inst}'");


        void tickCpuClockedTimers(byte cycles)
        {
            tickTimer0();
            tickTimer1();

            void tickTimer0()
            {
                var t0cnt = SFRs.T0Cnt;
                if (t0cnt is { T0lRun: false, T0hRun: false })
                    return;

                var t0Ticks = 0;
                for (int i = 0; i < cycles; i++)
                {
                    T0Scale++;
                    if (T0Scale == 0) // overflow
                    {
                        t0Ticks++;
                        T0Scale = SFRs.T0Prr;
                    }
                }

                var t0l = SFRs.T0L;
                var t0h = SFRs.T0H;
                for (var i = 0; i < t0Ticks; i++)
                {
                    // tick t0l
                    bool t0lOverflow = false;
                    if (t0cnt.T0lRun)
                    {
                        t0l++;
                        if (t0l == 0)
                        {
                            t0lOverflow = true;
                            t0l = SFRs.T0Lr;
                            if (!t0cnt.T0Long) // interrupt for overflow only in 8-bit mode
                            {
                                t0cnt.T0lOvf = true;
                                if (t0cnt.T0lIe)
                                    RequestedInterrupts |= Interrupts.INT2_T0L;
                            }
                        }
                    }

                    // tick t0h
                    if (t0cnt.T0hRun)
                    {
                        // Apply the tick either if in 8-bit mode, or in 16-bit mode and low byte overflowed
                        if (!t0cnt.T0Long || t0lOverflow)
                            t0h++;

                        if (t0h == 0)
                        {
                            t0h = SFRs.T0Hr;
                            t0cnt.T0hOvf = true;
                            if (t0cnt.T0hIe)
                                RequestedInterrupts |= Interrupts.T0H;
                        }
                    }
                }
                SFRs.T0L = t0l;
                SFRs.T0H = t0h;
                SFRs.T0Cnt = t0cnt;
            }

            void tickTimer1()
            {
                var t1cnt = SFRs.T1Cnt;
                if (t1cnt is { T1lRun: false, T1hRun: false })
                    return;

                for (var i = 0; i < cycles; i++)
                {
                    if (t1cnt.T1lRun)
                    {
                        var t1l = SFRs.T1L;
                        if (t1cnt.ELDT1C)
                        {
                            var cpuClockHz = SFRs.Ocr.CpuClockHz;
                            var t1lc = SFRs.T1Lc;
                            var p1 = SFRs.P1 with { PulseOutput = t1lc <= t1l };
                            Audio.AddPulse(cpuClockHz, p1.PulseOutput);
                            SFRs.P1 = p1;
                        }

                        t1l++;
                        if (t1l == 0)
                        {
                            t1l = SFRs.T1Lr;
                            t1cnt.T1lOvf = true;
                            if (t1cnt.T1lIe)
                                RequestedInterrupts |= Interrupts.T1;
                        }

                        SFRs.T1L = t1l;
                    }

                    if (t1cnt.T1hRun)
                    {
                        // Tick T1h only if in 8-bit mode or in 16-bit mode and lower overflowed
                        if (!t1cnt.T1Long || t1cnt.T1lOvf)
                        {
                            var t1h = (byte)(SFRs.T1H + 1);
                            if (t1h == 0)
                            {
                                t1h = SFRs.T1Hr;
                                t1cnt.T1hOvf = true;
                                if (t1cnt.T1hIe)
                                    RequestedInterrupts |= Interrupts.T1;
                            }

                            SFRs.T1H = t1h;
                        }
                    }
                }

                SFRs.T1Cnt = t1cnt;
            }
        }

        void requestLevelDrivenInterrupts()
        {
            // TODO: I suspect master interrupt disable should have an effect here
            // Level triggered interrupts keep getting triggered each cycle
            // Edge triggered interrupts are triggered in SFRs when the value changes
            var i01cr = SFRs.I01Cr;
            if (i01cr.Int0Enable && i01cr.Int0LevelTriggered && i01cr.Int0Source == i01cr.Int0HighTriggered && !currentlyServicing(Interrupts.INT0))
                RequestedInterrupts |= Interrupts.INT0;

            if (i01cr.Int1Enable && i01cr.Int1LevelTriggered && i01cr.Int0Source == i01cr.Int1HighTriggered && !currentlyServicing(Interrupts.INT1))
                RequestedInterrupts |= Interrupts.INT1;

            // Empirical testing shows that timer interrupts are "level driven"
            // in that if the source flag is not cleared, the interrupt is generated continuously.
            var t0cnt = SFRs.T0Cnt;
            if (t0cnt.T0lIe && t0cnt.T0lOvf && !currentlyServicing(Interrupts.INT2_T0L))
                RequestedInterrupts |= Interrupts.INT2_T0L;

            if (t0cnt.T0hIe && t0cnt.T0hOvf && !currentlyServicing(Interrupts.T0H))
                RequestedInterrupts |= Interrupts.T0H;

            var t1cnt = SFRs.T1Cnt;
            if (t1cnt.T1lIe && t1cnt.T1lOvf && !currentlyServicing(Interrupts.T1))
                RequestedInterrupts |= Interrupts.T1;

            if (t1cnt.T1hIe && t1cnt.T1hOvf && !currentlyServicing(Interrupts.T1))
                RequestedInterrupts |= Interrupts.T1;

            var btcr = SFRs.Btcr;
            if (btcr.Int0Enable && btcr.Int0Source && !currentlyServicing(Interrupts.INT3_BT))
                RequestedInterrupts |= Interrupts.INT3_BT;

            if (btcr.Int1Enable && btcr.Int1Source && !currentlyServicing(Interrupts.INT3_BT))
                RequestedInterrupts |= Interrupts.INT3_BT;

            var p3int = SFRs.P3Int;
            // NB: non-continuous interrupts are generated in SFRs.P3.set (i.e. only when P3 changes)
            // TODO: empirical test if holding a button continuously just keeps setting the source flag.
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

            // TODO: it might be cleaner to just wait to clear RequestedInterrupts until RETI and the interrupt is popped off of _servicingInterrupts.
            bool currentlyServicing(Interrupts interrupt)
            {
                Debug.Assert(BitHelpers.IsPowerOfTwo((int)interrupt));
                for (int i = 0; i < _interruptsCount; i++)
                {
                    if (_servicingInterrupts[i] == interrupt)
                        return true;
                }

                return false;
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
        Debug.Assert(BitHelpers.IsPowerOfTwo(InstructionBankSize));
        var a17 = SFRs.Trl | (SFRs.Trh << 8) | (SFRs.FPR.FlashAddressBank ? InstructionBankSize : 0);
        SFRs.Acc = Flash[a17];
        Pc += inst.Size;
    }

    /// <summary>Store the accumulator to flash memory. Intended for use only by BIOS. Undocumented.</summary>
    private void Op_STF(Instruction inst)
    {
        if (InstructionBank != InstructionBank.ROM)
            Logger.LogWarning("Executing STF outside of ROM!");

        var a16 = SFRs.Trl | (SFRs.Trh << 8);
        var value = SFRs.Acc;

        // Sequence number when flash is first unlocked for writing
        const int flashFirstUnlockSeq = 3;
        // Size of the aligned page that we expect the BIOS to write in a sequence
        const int flashPageSize = 128;

        if (SFRs.FPR.FlashWriteUnlock)
        {
            switch (_flashWriteUnlockSequence, a16, value)
            {
                case (0, 0x5555, 0xAA):
                    _flashWriteUnlockSequence = 1;
                    break;
                case (1, 0x2AAA, 0x55):
                    _flashWriteUnlockSequence = 2;
                    break;
                case (2, 0x5555, 0xA0):
                    _flashWriteUnlockSequence = flashFirstUnlockSeq;
                    break;
                default:
                    _flashWriteUnlockSequence = 0;
                    break;
            }

            Pc += inst.Size;
            return;
        }

        if (_flashWriteUnlockSequence == flashFirstUnlockSeq && (a16 & 0x127) != 0)
            Logger.LogWarning($"Starting unaligned flash write: {a16}", LogCategories.Instructions);

        if (_flashWriteUnlockSequence >= flashFirstUnlockSeq)
        {
            var a17 = a16 | (SFRs.FPR.FlashAddressBank ? InstructionBankSize : 0);
            Flash[a17] = value;
#if DEBUG
            InstructionMap[InstructionBank, (ushort)a16] = default;
#endif
            if (VmuFileHandle is not null)
            {
                var absoluteAddress = (SFRs.FPR.FlashAddressBank ? (1 << 16) : 0) | a16;
                RandomAccess.Write(VmuFileHandle, [value], absoluteAddress);
            }
            else
            {
                HasUnsavedChanges = true;
            }

            _flashWriteUnlockSequence++;
        }
        else
        {
            Logger.LogWarning($"Failed flash write due to bad sequence number {_flashWriteUnlockSequence}");
        }

        if (_flashWriteUnlockSequence == flashFirstUnlockSeq + flashPageSize)
                _flashWriteUnlockSequence = 0;

        Pc += inst.Size;
    }

    /// <summary>No operation</summary>
    private void Op_NOP(Instruction inst)
    {
        Pc += inst.Size;
    }
}
