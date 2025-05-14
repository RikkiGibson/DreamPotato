using VEmu.Core.SFRs;

namespace VEmu.Core;

// TODO: ideally we would use this map to maintain info on which regions of ROM are executable or not
// we could then feed this info back into reverse engineering tools
readonly struct InstructionMap()
{
    private readonly Instruction[][] _instructionBanks = [
        new Instruction[64 * 1024], // ROM
        new Instruction[64 * 1024], // FlashBank0
        new Instruction[64 * 1024], // FlashBank1
    ];

    public ReadOnlyMemory<Instruction> this[InstructionBank bank, Range range] => _instructionBanks[(int)bank][range];

    public Instruction this[InstructionBank bank, ushort offset]
    {
        get
        {
            var inst = _instructionBanks[(int)bank][offset];
            return inst;
        }
        set
        {
            var instructions = _instructionBanks[(int)bank];
            // Ensure not overlapping with a later instruction
            var end = offset + value.Operation.Size;
            for (var i = offset + 1; i < end; i++)
            {
                if (instructions[i].HasValue)
                    throw new InvalidOperationException($"Cannot store instruction '{value}' because map already contains overlapping '{instructions[i]}'");
            }

            // Ensure not overlapping with an earlier instruction
            for (var i = Math.Max(0, offset - 3); i < offset; i++)
            {
                var inst = instructions[i];
                if (inst.HasValue && i + inst.Size > offset)
                    throw new InvalidOperationException($"Cannot store instruction '{value}' because map already contains overlapping '{instructions[i]}'");
            }

            instructions[offset] = value;
        }
    }
}