using System.Diagnostics;

using DreamPotato.Core.SFRs;

namespace DreamPotato.Core;

internal readonly record struct InstructionDebugInfo(Instruction Instruction, bool Executed)
{
    public bool HasInstruction => Instruction.HasValue;
    public Operation Operation => Instruction.Operation;
    public byte Size => Instruction.Size;
    public ushort Offset => Instruction.Offset;
}

// TODO: ideally we would use this map to maintain info on which regions of ROM are executable or not
// we could then feed this info back into reverse engineering tools
public class DebugInfo(Logger _logger)
{
    private readonly InstructionDebugInfo[][] _instructionBanks = [
        new InstructionDebugInfo[64 * 1024], // ROM
        new InstructionDebugInfo[64 * 1024], // FlashBank0
        new InstructionDebugInfo[64 * 1024], // FlashBank1
    ];

    internal InstructionDebugInfo this[InstructionBank bank, ushort offset]
    {
        get
        {
            var inst = _instructionBanks[(int)bank][offset];
            return inst;
        }
        set
        {
            var instructions = _instructionBanks[(int)bank];
            if (!value.HasInstruction)
            {
                // clearing a single instruction because associated byte was changed.
                // Note that realistically byte sequences are written to ascending addresses but we can't bet on that.
                // For example, if an instruction starts before the start of the page currently being written.
                // So, deciding if this assignment is invalidating existing region of instructions is tricky.
                //
                // Possibly we just want to replace this map with just marking certain addresses as executable based on the fact that we executed them.
                // Also marking certain addresses as data because we read them with LDC/LDF.
                // No need to cache the instruction here, we could instead expose an on-demand view of the executable instructions based on what we know in that moment.
                instructions[offset] = value;
                return;
            }

            // Ensure not overlapping with a later instruction
            var end = offset + value.Operation.Size;
            for (var i = offset + 1; i < end; i++)
            {
                if (instructions[i].HasInstruction)
                    _logger.LogError($"Error storing instruction '{value}'. Map already contains overlapping '{instructions[i]}'", LogCategories.Instructions);
            }

            // Ensure not overlapping with an earlier instruction
            for (var i = Math.Max(0, offset - 3); i < offset; i++)
            {
                var inst = instructions[i];
                if (inst.HasInstruction && i + inst.Size > offset)
                    _logger.LogError($"Error storing instruction '{value}'. Map already contains overlapping '{instructions[i]}'", LogCategories.Instructions);
            }

            instructions[offset] = value;
        }
    }

    // Search 'content' for executable instructions.
    // Note: VMU code doesn't have "data" vs "code" segments. Everything is just in the binary.
    // i.e. pushing an address to stack and returning to it is not accounted for yet.
    public void Load(InstructionBank bankId, ReadOnlySpan<byte> content)
    {
        var bank = _instructionBanks[(int)bankId];
        Debug.Assert(bank.Length >= content.Length);

        // Walk all reachable executable code paths and populate the instruction map
        var pendingBranches = new Stack<ushort>([
            0,
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

            this[bankId, offset] = new(inst, Executed: false);

            // Process branches
            switch (inst.Kind)
            {
                case OperationKind.RET or OperationKind.RETI:
                    // End of the branch
                    continue;

                // Unconditional branches: JMP, JMPF, BR, BRF
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
                    // Problem: which bank this jumps to depends on cpu state. Have to figure that out to ensure e.g. clock tick routines are reachable
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

                // Conditional branches: BZ, BNZ, BP, BPC, BN, DBNZ, BE, BNE, CALL, CALLF
                // These *break* to exit so that the "non-branching" path is also hit.
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
                    // (PC) <- (PC) + 3, if (ACC) == #i8 then (PC) <- (PC) + r8
                    // (PC) <- (PC) + 3, if (ACC) == d9 then (PC) <- (PC) + r8
                    // (PC) <- (PC) + 3, if ((Ri)) == #i8 then (PC) <- (PC) + r8
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
                    // (PC) <- (PC) + 3, (SP) <- (SP) + 1, ((SP)) <- (PC7 to 0),
                    // (SP) <- (SP) + 1, ((SP)) <- (PC15 to 8), (PC) <- a16
                    var dest = inst.Arg0;
                    pendingBranches.Push(dest);
                    break;
                }

                case OperationKind.CALLR:
                {
                    // (PC) <- (PC) + 3, (SP) <- (SP) + 1, ((SP)) <- (PC7 to 0),
                    // (SP) <- (SP) + 1, ((SP)) <- (PC15 to 8), (PC) <- (PC) - 1 + r16
                    var dest = (ushort)(offset + inst.Size - 1 + inst.Arg0);
                    pendingBranches.Push(dest);
                    break;
                }
            }

            pendingBranches.Push((ushort)(offset + inst.Size));
        }
    }

    public IEnumerable<(ushort offset, string disasm)> GetDisassembly()
    {
        foreach (var item in _instructionBanks[(int)InstructionBank.ROM])
        {
            if (!item.HasInstruction)
                continue;

            yield return (item.Offset, item.Instruction.ToString());
        }
    }

    public void Clear()
    {
        foreach (var bank in _instructionBanks)
        {
            Array.Clear(bank);
        }
    }

    public string Dump(InstructionBank bank)
    {
        // TODO: dump contents of instruction bank to text
        // print offsets and instruction data
        return "";
    }
}