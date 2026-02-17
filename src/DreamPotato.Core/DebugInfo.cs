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

    public string DisplayInstruction() => Instruction.ToString();

    public int CompareTo(InstructionDebugInfo other)
    {
        return Offset.CompareTo(other.Offset);
    }
}

public class BreakpointInfo
{
    /// <summary>true if the breakpoint is created for a temporary purpose, and shouldn't appear in UI. e.g. breakpoints created for 'step' commands.</summary>
    public bool Implicit;
    public required bool Enabled;
    public required ushort Offset;
}

public class BankDebugInfo
{
    /// <summary>NOTE: must be sorted by Offset.</summary>
    public readonly List<InstructionDebugInfo> Instructions = [];

    /// <summary>Not necessarily sorted.</summary>
    public readonly List<BreakpointInfo> Breakpoints = [];

    public int BinarySearchInstructions(ushort offset)
    {
        var item = new InstructionDebugInfo(new Instruction() { Offset = offset }, executed: false);
        return Instructions.BinarySearch(item);
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

    internal void Clear()
    {
        Instructions.Clear();
        Breakpoints.Clear();
    }
}

public class DebugInfo(Cpu cpu)
{
    /// <summary>Sorted lists of instructions for each bank.</summary>
    private readonly ImmutableArray<BankDebugInfo> _bankInfos = [
        new(), // ROM
        new(), // FlashBank0
        new(), // FlashBank1
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

    public InstructionDebugInfo this[InstructionBank bankId, ushort offset]
    {
        get
        {
            var bankInfo = _bankInfos[(int)bankId];
            return bankInfo.BinarySearchInstructions(offset) is >= 0 and var index ? bankInfo.Instructions[index] : default;
        }
        set
        {
            var bank = _bankInfos[(int)bankId].Instructions;
            var index = bank.BinarySearch(new InstructionDebugInfo(new Instruction() { Offset = offset }, executed: false));

            // Already had an inst at this offset, replace it
            // TODO2: this seems wrong. Need to test this code and generalize within reason.
            // When we insert an instruction, we could overlap with N number of following instructions, which need to be removed.
            // Need to verify this invariant.
            // Note: When 'value.HasInstruction == false', this should be treated as a remove operation only
            if (index >= 0)
            {
                if (value.HasInstruction)
                    bank[index] = value;
                else
                    bank.RemoveAt(index);
            }

            var nextIndex = ~index;
            Debug.Assert(nextIndex >= 0);

            // Check next element for overlap
            if (nextIndex < bank.Count)
            {
                var nextOffset = bank[nextIndex].Offset;
                var size = value.HasInstruction ? value.Size : 0;
                if (offset + size > nextOffset)
                    bank.RemoveAt(nextIndex);
            }

            if (value.HasInstruction)
                bank.Insert(nextIndex, value);

            // Check previous element for overlap
            if (nextIndex > 0)
            {
                var prev = bank[nextIndex - 1];
                if (prev.Offset + prev.Size > offset)
                    bank.RemoveAt(nextIndex - 1);
            }
        }
    }

    // Search 'content' for executable instructions.
    // Note: VMU code doesn't have "data" vs "code" segments. Everything is just in the binary.
    // i.e. pushing an address to stack and returning to it is not accounted for yet.
    public void Load(InstructionBank bankId)
    {
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
            if (this[bankId, offset].HasInstruction)
                continue;

            this[bankId, offset] = new(inst, executed: false);

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

    public BankDebugInfo GetBankInfo(InstructionBank bankId)
    {
        if (_bankInfos[(int)bankId].Instructions.Count == 0)
            Load(bankId);

        return _bankInfos[(int)bankId];
    }

    /// <summary>Call when loading a new VMU file or similar to keep stale instructions from appearing in debugger.</summary>
    public void ClearFlash()
    {
        _bankInfos[(int)InstructionBank.FlashBank0].Clear();
        _bankInfos[(int)InstructionBank.FlashBank1].Clear();
    }

    internal void MarkExecutable(InstructionBank bankId, Instruction inst)
    {
        // TODO2: If this is a new code path, Load() it
        // Also mark branches that can reach this inst via 'push/return'.
        // persist such code paths in symbol files.
        if (_bankInfos[(int)bankId].Instructions.Count == 0)
            Load(bankId);

        this[bankId, inst.Offset] = new InstructionDebugInfo(inst, executed: true);
    }
}
