using DreamPotato.Core.SFRs;

namespace DreamPotato.Core;

// TODO: ideally we would use this map to maintain info on which regions of ROM are executable or not
// we could then feed this info back into reverse engineering tools
class InstructionMap(Logger _logger)
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
            if (!value.HasValue)
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
                if (instructions[i].HasValue)
                    _logger.LogError($"Error storing instruction '{value}'. Map already contains overlapping '{instructions[i]}'", LogCategories.Instructions);
            }

            // Ensure not overlapping with an earlier instruction
            for (var i = Math.Max(0, offset - 3); i < offset; i++)
            {
                var inst = instructions[i];
                if (inst.HasValue && i + inst.Size > offset)
                    _logger.LogError($"Error storing instruction '{value}'. Map already contains overlapping '{instructions[i]}'", LogCategories.Instructions);
            }

            instructions[offset] = value;
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