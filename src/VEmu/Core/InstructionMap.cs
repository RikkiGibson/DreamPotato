namespace VEmu.Core;

readonly struct InstructionMap()
{
    private readonly Instruction[] _instructions = new Instruction[ushort.MaxValue];

    public ReadOnlyMemory<Instruction> this[Range range] => _instructions[range];

    public Instruction this[ushort offset]
    {
        get
        {
            var inst = _instructions[offset];
            return inst;
        }
        set
        {
            _instructions[offset] = value;
            var end = offset + value.Operation.Size;
            for (var i = offset + 1; i < end; i++)
            {
                if (_instructions[i].HasValue)
                    throw new InvalidOperationException($"Cannot store instruction '{value}' because map already contains overlapping '{_instructions[i]}'");
            }
        }
    }
}