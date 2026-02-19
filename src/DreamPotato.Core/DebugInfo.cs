using System.Collections.Immutable;
using System.Diagnostics;

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

    public int CompareTo(InstructionDebugInfo other)
    {
        return Offset.CompareTo(other.Offset);
    }

    public override string ToString()
        => $"({Instruction}, Executed={Executed})";
}

public class BreakpointInfo
{
    /// <summary>true if the breakpoint is created for a temporary purpose, and shouldn't appear in UI. e.g. breakpoints created for 'step' commands.</summary>
    public bool Implicit;
    public required bool Enabled;
    public required ushort Offset;
}

public class BankDebugInfo(Cpu cpu, InstructionBank bankId)
{
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

    void VerifyOffsets()
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

    public InstructionDebugInfo GetStepOverDest(ushort offset)
    {
        // Step Over goal: skip calls, etc.
        if (BinarySearchInstructions(offset) is >= 0 and var index)
        {
            var inst = Instructions[index];
            // Stepping over a call means breaking on the RET after the call.
            // TODO: Oops, this doesn't handle reentrancy. Need stack info first..
            if (inst.Operation.Kind == OperationKind.CALL)
            {
                if (index + 1 < Instructions.Count)
                    return Instructions[index + 1];
            }
        }

        return default;
    }

    /// <summary>
    /// Search 'content' for executable instructions.
    /// Note: VMU code doesn't have "data" vs "code" segments. Everything is just in the binary.
    /// i.e. pushing an address to stack and returning to it is not accounted for yet.
    /// </summary>
    public void Load()
    {
        // TODO2: need a version to take an address for loading a specific code path
        // e.g. for push+ret scenario

        var content = cpu.GetRomBank(bankId);
        // Walk all reachable executable code paths and populate the instruction map
        var pendingBranches = new Stack<ushort>([
            0, // Entry point
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
        ]);

        while (pendingBranches.Count != 0)
        {
            ushort offset = pendingBranches.Pop();
            if (offset >= content.Length)
                continue;

            var inst = InstructionDecoder.Decode(content, offset);

            // Already visited
            if (this.GetInstruction(offset).HasInstruction)
                continue;

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
        // TODO2: this should delete any instr which overlaps with 'offset'
        var index = BinarySearchInstructions(offset);
        if (index >= 0)
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

    public DebuggingState DebuggingState => cpu.DebuggingState;
    public BankDebugInfo CurrentBankInfo => GetBankInfo(cpu.CurrentInstructionBankId);

    public event Action<InstructionDebugInfo>? DebugBreak;
    public void FireDebugBreak()
    {
        cpu.DebuggingState = DebuggingState.Break;
        var bankInfo = CurrentBankInfo;
        var index = bankInfo.BinarySearchInstructions(cpu.ProgramCounter);
        Debug.Assert(index >= 0 && index < bankInfo.Instructions.Count);
        DebugBreak?.Invoke(bankInfo.Instructions[index]);
    }

    public void ToggleDebugBreak()
    {
        if (cpu.DebuggingState == DebuggingState.Break)
        {
            cpu.DebuggingState = DebuggingState.Run;
            return;
        }

        FireDebugBreak();
    }

    public void StepIn()
    {
        cpu.DebuggingState = DebuggingState.StepIn;
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
        // TODO2: If this is a new code path, Load() it
        // Also mark branches that can reach this inst via 'push/return'.
        // persist such code paths in symbol files.
        if (bankInfo.Instructions.Count == 0)
            bankInfo.Load();

        bankInfo.SetInstruction(new InstructionDebugInfo(inst, executed: true));
    }
}
