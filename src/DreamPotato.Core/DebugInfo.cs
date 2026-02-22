using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

using DreamPotato.Core.SFRs;

namespace DreamPotato.Core;

public readonly struct InstructionDebugInfo : IComparable<InstructionDebugInfo>
{
    internal InstructionDebugInfo(Instruction instruction, bool executed)
    {
        Instruction = instruction;
        Executed = executed;
    }

    internal Instruction Instruction { get; }
    public bool Executed { get; }

    public bool HasInstruction => Instruction.HasValue;
    internal Operation Operation => Instruction.Operation;
    public byte Size => Instruction.Size;
    public ushort Offset => Instruction.Offset;

    public ushort EndOffset => (ushort)(Offset + Size);

    public string DisplayInstruction() => Instruction.ToString();

    public string DisplayArgumentValues(Cpu cpu)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < Instruction.Parameters.Length; i++)
        {
            var param = Instruction.Parameters[i];
            if (param.Kind is ParameterKind.D9 or ParameterKind.Ri)
            {
                var arg = Instruction.GetArgument(i);
                builder.Append('[');
                Instruction.DisplayArgument(builder, param, arg);
                builder.AppendFormat("] = {0:X2}H", cpu.FetchOperand(param, arg));
                builder.AppendLine();
            }
        }
        return builder.ToString();
    }

    public int CompareTo(InstructionDebugInfo other)
    {
        return Offset.CompareTo(other.Offset);
    }

    public override string ToString()
        => $"({Instruction}, Executed={Executed})";
}

public class BreakpointInfo
{
    public required bool Enabled;
    public required ushort Offset;
}

public class WatchInfo
{
    // TODO: watch items need to include any necessary bank flags, etc. to precisely identify a memory location
    public required ushort Offset;

    public override string ToString()
    {
        if ((Offset & 0x100) != 0 && SpecialFunctionRegisterIds.GetName((byte)Offset) is { } name)
            return name;

        return $"{Offset:X4}H";
    }
}

public class BankDebugInfo(Cpu cpu, InstructionBank bankId)
{
    public InstructionBank BankId => bankId;

    /// <summary>NOTE: must be sorted by Offset.</summary>
    public IReadOnlyList<InstructionDebugInfo> Instructions => _instructions;
    private readonly List<InstructionDebugInfo> _instructions = [];

    /// <summary>Not necessarily sorted.</summary>
    public readonly List<BreakpointInfo> Breakpoints = [];

    public int BinarySearchInstructions(ushort offset)
    {
        var item = new InstructionDebugInfo(new Instruction() { Offset = offset }, executed: false);
        return _instructions.BinarySearch(item);
    }

    public InstructionDebugInfo GetOrLoadInstruction(ushort offset)
    {
        var instruction = GetInstruction(offset);
        if (!instruction.HasInstruction)
        {
            Load([offset]);
            instruction = GetInstruction(offset);
            Debug.Assert(instruction.HasInstruction);
        }

        return instruction;
    }

    public InstructionDebugInfo GetInstruction(ushort offset)
    {
        var index = BinarySearchInstructions(offset);
        return index < 0 ? default : Instructions[index];
    }

    public void SetInstruction(InstructionDebugInfo value)
    {
        if (!value.HasInstruction)
            throw new InvalidOperationException();

        var bank = _instructions;
        var offset = value.Offset;
        var endOffset = value.EndOffset;
        var index = BinarySearchInstructions(offset);

        // Instructions take up a range of the 'offset' space,
        // but only a single value of the 'index' space:
        //        |NOP| JMPF 1234 | LD 42 |
        // offset | 0 | 1 | 2 | 3 | 4 | 5 |
        // index  | 0 | 1         | 2     |

        // Already had an inst at this offset, replace it
        if (index >= 0)
        {
            // Before: |NOP|NOP|NOP|
            // After:  | JMPF 1234 |
            // offset  | 0 | 1 | 2 |

            // Replace the instruction at 'index'
            bank[index] = value;
            index++;
            // Delete any instructions which are now overlapping with the inserted instruction
            while (index < bank.Count && bank[index].Offset < endOffset)
                bank.RemoveAt(index);

            return;
        }

        // Before: | LD 42 |NOP| ST 42 |
        // After:  |   | JMPF 1234 |   |
        // offset  | 0 | 1 | 2 | 3 | 4 |

        // Did not have an existing inst with the same offset.
        // BinarySearch gave us the index of the next instruction if any, or 'bank.Count'.
        // Insert it and delete any instructions that overlap
        index = ~index;
        Debug.Assert(index >= 0);

        // Remove later instructions that overlap with 'value'
        // e.g. above sample: 'NOP', 'ST 42'
        while (index < bank.Count && bank[index].Offset < endOffset)
            bank.RemoveAt(index);

        bank.Insert(index, value);

        // Remove earlier instructions that overlap with 'value'
        // e.g. above sample: 'LD 42'
        for (var prevIndex = index - 1; prevIndex >= 0 && bank[prevIndex].EndOffset > value.Offset; prevIndex--)
            bank.RemoveAt(prevIndex);
    }

    // TODO: figure out where is appropriate to check this in debug mode
    private void VerifyOffsets()
    {
        // Invariants:
        // 1. Instructions are sorted by Offset
        // 2. Offset must *always* increase from one to the next entry
        // Note: The offset space doesn't need to be "saturated", i.e. there can be potentially a large gap from one to the next Offset
        var offset = -1;
        foreach (var inst in _instructions)
        {
            if (offset >= inst.Offset)
                throw new InvalidOperationException();

            offset = inst.Offset;
        }
    }

    /// <summary>Search the default set of entry points for executable instructions.</summary>
    public void Load()
    {
        Load([
            0, // Main entry point
            InterruptVectors.INT0,
            InterruptVectors.INT1,
            InterruptVectors.INT2_T0L,
            InterruptVectors.INT3_BT,
            InterruptVectors.T0H,
            InterruptVectors.T1,
            InterruptVectors.SIO0,
            InterruptVectors.SIO1,
            InterruptVectors.Maple,
            InterruptVectors.P3,
            // ROM external programs
            .. bankId == InstructionBank.ROM ? (ReadOnlySpan<ushort>)[
                BuiltInCodeSymbols.BIOSWriteFlash,
                BuiltInCodeSymbols.BIOSVerifyFlash,
                BuiltInCodeSymbols.BIOSExit,
                BuiltInCodeSymbols.BIOSReadFlash,
                BuiltInCodeSymbols.BIOSClockTick
            ] : []
        ]);
    }

    /// <summary>Search a specific list of entry points for instructions.</summary>
    public void Load(ushort[] entryPoints)
    {
        var content = cpu.GetRomBank(bankId);
        // Walk all reachable executable code paths and populate the instruction map
        var pendingBranches = new Stack<ushort>(entryPoints);

        while (pendingBranches.Count != 0)
        {
            ushort offset = pendingBranches.Pop();
            if (offset >= content.Length)
                continue;

            // Already visited
            if (this.GetInstruction(offset).HasInstruction)
                continue;

            var inst = InstructionDecoder.Decode(content, offset);
            this.SetInstruction(new(inst, executed: false));

            // Process unconditional branches
            switch (inst.Kind)
            {
                case OperationKind.RET or OperationKind.RETI:
                    // End of the branch
                    continue;

                case OperationKind.JMP:
                {
                    var dest = offset;
                    dest += 2;
                    dest &= 0b1111_0000__0000_0000;
                    dest |= inst.Arg0;
                    pendingBranches.Push(dest);
                    continue;
                }

                case OperationKind.JMPF:
                {
                    // Note: this could jump between ROM/flash depending on cpu state.
                    // Hopefully, the ordinary visit pass within a bank, makes the destination reachable anyway.
                    var dest = inst.Arg0;
                    pendingBranches.Push(dest);
                    continue;
                }

                case OperationKind.BR:
                {
                    var dest = (ushort)(offset + inst.Size + (sbyte)inst.Arg0);
                    pendingBranches.Push(dest);
                    continue;
                }

                case OperationKind.BRF:
                {
                    var dest = (ushort)(offset + inst.Size - 1 + inst.Arg0);
                    pendingBranches.Push(dest);
                    continue;
                }
            }

            // Next instruction is reachable
            pendingBranches.Push((ushort)(offset + inst.Size));

            // Process conditional branches
            switch (inst.Kind)
            {
                case OperationKind.BZ or OperationKind.BNZ:
                {
                    var dest = (ushort)(offset + inst.Size + (sbyte)inst.Arg0);
                    pendingBranches.Push(dest);
                    break;
                }

                case OperationKind.BP or OperationKind.BPC or OperationKind.BN:
                {
                    var dest = (ushort)(offset + inst.Size + (sbyte)inst.Arg2);
                    pendingBranches.Push(dest);
                    break;
                }

                case OperationKind.DBNZ:
                {
                    var dest = (ushort)(offset + inst.Size + (sbyte)inst.Arg1);
                    pendingBranches.Push(dest);
                    break;
                }

                case OperationKind.BE or OperationKind.BNE:
                {
                    var param0 = inst.Parameters[0];
                    var indirectMode = param0.Kind == ParameterKind.Ri;
                    var r8 = indirectMode ? (sbyte)inst.Arg2 : (sbyte)inst.Arg1;
                    var dest = (ushort)(offset + inst.Size + r8);
                    pendingBranches.Push(dest);
                    break;
                }

                case OperationKind.CALL:
                {
                    // Similar to JMP except the next instruction is reachable
                    var dest = offset;
                    dest += 2;
                    dest &= 0b1111_0000__0000_0000;
                    dest |= inst.Arg0;
                    pendingBranches.Push(dest);
                    break;
                }

                case OperationKind.CALLF:
                {
                    // Similar to JMPF except the next instruction is reachable
                    var dest = inst.Arg0;
                    pendingBranches.Push(dest);
                    break;
                }

                case OperationKind.CALLR:
                {
                    var dest = (ushort)(offset + inst.Size - 1 + inst.Arg0);
                    pendingBranches.Push(dest);
                    break;
                }
            }
        }
    }

    internal void Clear()
    {
        _instructions.Clear();
        Breakpoints.Clear();
    }

    internal void ClearInstruction(ushort offset)
    {
        var index = BinarySearchInstructions(offset);
        if (index >= 0)
        {
            _instructions.RemoveAt(index);
            return;
        }

        // Remove a preceding instr if it overlaps with 'offset'
        index = ~index;
        if (index == 0)
            return;

        index--;
        if (_instructions[index].EndOffset > offset)
            _instructions.RemoveAt(index);
    }
}

public class DebugInfo(Cpu cpu)
{
    /// <summary>Sorted lists of instructions for each bank.</summary>
    private readonly ImmutableArray<BankDebugInfo> _bankInfos = [
        new(cpu, InstructionBank.ROM),
        new(cpu, InstructionBank.FlashBank0),
        new(cpu, InstructionBank.FlashBank1),
    ];

    public DebuggingState DebuggingState { get; private set; } = DebuggingState.Run;
    /// <summary>Stack offset of the related return address of a 'step over/step out' command.</summary>
    public ushort StepOutOffset { get; private set; }

    public readonly List<WatchInfo> Watches = [
        new() { Offset = 0x100 | SpecialFunctionRegisterIds.Acc },
        new() { Offset = 0x100 | SpecialFunctionRegisterIds.Psw },
        new() { Offset = 0x100 | SpecialFunctionRegisterIds.B },
        new() { Offset = 0x100 | SpecialFunctionRegisterIds.C },
    ];

    public BankDebugInfo CurrentBankInfo => GetBankInfo(cpu.CurrentInstructionBankId);

    public event Action<InstructionDebugInfo>? DebugBreak;
    public void FireDebugBreak()
    {
        DebuggingState = DebuggingState.Break;
        var bankInfo = CurrentBankInfo;
        var instructionInfo = bankInfo.GetOrLoadInstruction(cpu.ProgramCounter);
        DebugBreak?.Invoke(instructionInfo);
    }

    public void ToggleDebugBreak()
    {
        if (DebuggingState == DebuggingState.Break)
        {
            DebuggingState = DebuggingState.Run;
            return;
        }

        FireDebugBreak();
    }

    public void StepIn()
    {
        DebuggingState = DebuggingState.StepIn;
    }

    public void StepOut()
    {
        // Can only step out, if there is something to step out of
        var lastCall = cpu.StackData.LastOrDefault(entry => entry.Kind is StackValueKind.CallReturn or StackValueKind.InterruptReturn);
        if (lastCall.Kind == StackValueKind.Unknown)
            return;

        DebuggingState = DebuggingState.StepOut;
        StepOutOffset = lastCall.Offset;
    }

    public BankDebugInfo GetBankInfo(InstructionBank bankId)
    {
        var info = _bankInfos[(int)bankId];
        if (info.Instructions.Count == 0)
            info.Load();

        return info;
    }

    /// <summary>Call when loading a new VMU file or similar to keep stale instructions from appearing in debugger.</summary>
    public void ClearFlash()
    {
        _bankInfos[(int)InstructionBank.FlashBank0].Clear();
        _bankInfos[(int)InstructionBank.FlashBank1].Clear();
    }

    internal void MarkExecutable(InstructionBank bankId, Instruction inst)
    {
        var bankInfo = GetBankInfo(bankId);
        // Ensure code path is loaded
        _ = bankInfo.GetOrLoadInstruction(inst.Offset);

        // TODO: visualize which code has been executed or not
        bankInfo.SetInstruction(new InstructionDebugInfo(inst, executed: true));
    }
}
