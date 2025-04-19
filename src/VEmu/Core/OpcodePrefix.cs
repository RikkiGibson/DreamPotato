using System.Diagnostics;

// Interesting facts about instructions:
// - encoding
// - size
// - cycles
// - flags affected
// - interrupts enabled

enum OpcodePrefix : byte
{
    // Arithmetic
    ADD =	0b1000_0000,
    ADDC =	0b1001_0000,
    SUB =	0b1010_0000,
    SUBC =	0b1011_0000,
    INC =	0b0110_0000,
    DEC =	0b0111_0000,

    // Logical
    AND =	0b1110_0000,
    OR =	0b1101_0000,
	XOR =	0b1111_0000,
}

// Following opcodes do not take arguments, they simply modify ACC.
enum Opcode : byte
{
	ROL =	0b11100000,
	ROLC =	0b11110000,
	ROR =	0b11000000,
	RORC =	0b11010000,
    MUL =	0b0011_0000,
    DIV =	0b0100_0000,
}

static class OpcodePrefixExtensions
{
    public static OpcodePrefix GetPrefix(byte b)
    {
        byte leftNybble = (byte)(b & 0b1111_1000);
        Debug.Assert(Enum.GetValues<OpcodePrefix>().Contains((OpcodePrefix)leftNybble));
        return (OpcodePrefix)leftNybble;
    }
}