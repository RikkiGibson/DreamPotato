// Throwaway CLI utility for comparing save states.
// Using this to figure out how to "programmatically" skip the startup date setting routine.

using DreamPotato.Core;

if (args.Length != 2)
{
    Console.Error.WriteLine($"Usage: DreamPotato.SaveStateDiff <save-state-1> <save-state-2>");
    return 1;
}

var vmu1 = new Vmu();
vmu1.LoadStateFromPath(args[0]);
var cpu1 = vmu1._cpu;

var vmu2 = new Vmu();
vmu2.LoadStateFromPath(args[1]);
var cpu2 = vmu2._cpu;

// ROM
if (!cpu1.ROM.SequenceEqual(cpu2.ROM))
{
    Console.WriteLine("ROM content differs");
}

// Flash
for (int i = 0; i < Cpu.FlashSize; i++)
{
    if (cpu1.Flash[i] != cpu2.Flash[i])
        Console.WriteLine($"Flash[0x{i:X}] difference: 0x{cpu1.Flash[i]:X} <-> 0x{cpu2.Flash[i]:X}");
}

// InstructionBank
if (cpu1.InstructionBank != cpu2.InstructionBank)
    Console.WriteLine($"InstructionBank difference: 0x{cpu1.InstructionBank:X} <-> 0x{cpu2.InstructionBank:X}");

// Pc
if (cpu1.Pc != cpu2.Pc)
    Console.WriteLine($"Pc difference: 0x{cpu1.Pc:X} <-> 0x{cpu2.Pc:X}");

// TicksOverrun
if (cpu1.TicksOverrun != cpu2.TicksOverrun)
    Console.WriteLine($"TicksOverrun difference: 0x{cpu1.TicksOverrun:X} <-> 0x{cpu2.TicksOverrun:X}");

// StepCycleTicksPerSecondRemainder
if (cpu1.StepCycleTicksPerSecondRemainder != cpu2.StepCycleTicksPerSecondRemainder)
    Console.WriteLine($"StepCycleTicksPerSecondRemainder difference: 0x{cpu1.StepCycleTicksPerSecondRemainder:X} <-> 0x{cpu2.StepCycleTicksPerSecondRemainder:X}");

// BaseTimer
if (cpu1.BaseTimer != cpu2.BaseTimer)
    Console.WriteLine($"BaseTimer difference: 0x{cpu1.BaseTimer:X} <-> 0x{cpu2.BaseTimer:X}");

// BaseTimerTicksRemaining
if (cpu1.BaseTimerTicksRemaining != cpu2.BaseTimerTicksRemaining)
    Console.WriteLine($"BaseTimerTicksRemaining difference: 0x{cpu1.BaseTimerTicksRemaining:X} <-> 0x{cpu2.BaseTimerTicksRemaining:X}");

// RequestedInterrupts
if (cpu1.RequestedInterrupts != cpu2.RequestedInterrupts)
    Console.WriteLine($"RequestedInterrupts difference: 0x{cpu1.RequestedInterrupts:X} <-> 0x{cpu2.RequestedInterrupts:X}");

// _interruptServicingState
var servicingState1 = typeof(DreamPotato.Core.Cpu).GetField("_interruptServicingState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(cpu1);
var servicingState2 = typeof(DreamPotato.Core.Cpu).GetField("_interruptServicingState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(cpu2);
if (!Equals(servicingState1, servicingState2))
    Console.WriteLine($"_interruptServicingState difference: {servicingState1} <-> {servicingState2}");

// _servicingInterrupts
var servicingInterrupts1 = (DreamPotato.Core.Interrupts[])typeof(DreamPotato.Core.Cpu).GetField("_servicingInterrupts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(cpu1)!;
var servicingInterrupts2 = (DreamPotato.Core.Interrupts[])typeof(DreamPotato.Core.Cpu).GetField("_servicingInterrupts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(cpu2)!;
for (int i = 0; i < servicingInterrupts1.Length; i++)
{
    if (servicingInterrupts1[i] != servicingInterrupts2[i])
        Console.WriteLine($"_servicingInterrupts[{i}] difference: {servicingInterrupts1[i]} <-> {servicingInterrupts2[i]}");
}

// _interruptsCount
var interruptsCount1 = (int)typeof(DreamPotato.Core.Cpu).GetField("_interruptsCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(cpu1)!;
var interruptsCount2 = (int)typeof(DreamPotato.Core.Cpu).GetField("_interruptsCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(cpu2)!;
if (interruptsCount1 != interruptsCount2)
    Console.WriteLine($"_interruptsCount difference: {interruptsCount1} <-> {interruptsCount2}");

// _flashWriteUnlockSequence
var flashWriteUnlockSequence1 = (int)typeof(DreamPotato.Core.Cpu).GetField("_flashWriteUnlockSequence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(cpu1)!;
var flashWriteUnlockSequence2 = (int)typeof(DreamPotato.Core.Cpu).GetField("_flashWriteUnlockSequence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(cpu2)!;
if (flashWriteUnlockSequence1 != flashWriteUnlockSequence2)
    Console.WriteLine($"_flashWriteUnlockSequence difference: {flashWriteUnlockSequence1} <-> {flashWriteUnlockSequence2}");

// Memory deep comparison
var mem1 = cpu1.Memory;
var mem2 = cpu2.Memory;

// Compare Work RAM
var workRam1 = mem1.Direct_AccessWorkRam();
var workRam2 = mem2.Direct_AccessWorkRam();
for (int i = 0; i < workRam1.Length; i++)
{
    if (workRam1[i] != workRam2[i])
        Console.WriteLine($"WorkRAM[0x{i:X}] difference: {workRam1[i]} <-> {workRam2[i]}");
}

// Compare XRAM0
var xram0_1 = mem1.Direct_AccessXram0();
var xram0_2 = mem2.Direct_AccessXram0();
for (int i = 0; i < xram0_1.Length; i++)
{
    if (xram0_1[i] != xram0_2[i])
        Console.WriteLine($"XRAM0[0x{i:X}] difference: {xram0_1[i]} <-> {xram0_2[i]}");
}

// Compare XRAM1
var xram1_1 = mem1.Direct_AccessXram1();
var xram1_2 = mem2.Direct_AccessXram1();
for (int i = 0; i < xram1_1.Length; i++)
{
    if (xram1_1[i] != xram1_2[i])
        Console.WriteLine($"XRAM1[0x{i:X}] difference: {xram1_1[i]} <-> {xram1_2[i]}");
}

// Compare XRAM2
var xram2_1 = mem1.Direct_AccessXram2();
var xram2_2 = mem2.Direct_AccessXram2();
for (int i = 0; i < xram2_1.Length; i++)
{
    if (xram2_1[i] != xram2_2[i])
        Console.WriteLine($"XRAM2[0x{i:X}] difference: {xram2_1[i]} <-> {xram2_2[i]}");
}

// Compare SFRs (Special Function Registers) at byte level

var sfrType = typeof(DreamPotato.Core.SpecialFunctionRegisters);
var rawMemoryField = sfrType.GetField("_rawMemory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var t1hrField = sfrType.GetField("_t1hr", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var t1lrField = sfrType.GetField("_t1lr", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

var rawMemory1 = (byte[])rawMemoryField?.GetValue(mem1.SFRs)!;
var rawMemory2 = (byte[])rawMemoryField?.GetValue(mem2.SFRs)!;
for (int i = 0; i < rawMemory1.Length; i++)
{
    if (rawMemory1[i] != rawMemory2[i])
        Console.WriteLine($"SFRs[{SpecialFunctionRegisterIds.GetName((byte)i) ?? $"0x{i:X}"}] difference: 0x{rawMemory1[i]:X} <-> 0x{rawMemory2[i]:X}");
}

var t1hr1 = (byte)t1hrField?.GetValue(mem1.SFRs)!;
var t1hr2 = (byte)t1hrField?.GetValue(mem2.SFRs)!;
if (t1hr1 != t1hr2)
    Console.WriteLine($"SFRs._t1hr difference: 0x{t1hr1:X} <-> 0x{t1hr2:X}");

var t1lr1 = (byte)t1lrField?.GetValue(mem1.SFRs)!;
var t1lr2 = (byte)t1lrField?.GetValue(mem2.SFRs)!;
if (t1lr1 != t1lr2)
    Console.WriteLine($"SFRs._t1lr difference: 0x{t1lr1:X} <-> 0x{t1lr2:X}");

// so that loose if-statements stop highlighting the whole file about a missing return
{
}

return 0;
