
struct Instruction
{
    public Opcode Opcode;
    public byte Size;
    public byte Cycles;

    static Instruction Decode(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}