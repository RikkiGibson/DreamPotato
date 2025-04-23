using System.Diagnostics;

using VEmu.Core;

// Interesting facts about instructions:
// - encoding
// - size
// - cycles
// - flags affected
// - interrupts enabled

/// <summary>
/// Note that a well formed instruction from this group will always have a <see cref="AddressingMode"/> OR'd into it.
/// TODO: this isn't always true, addressing modes and address bits are not packed into these instructions all in the same way.
/// </summary>
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

    // Data Transfer
    LD =	0b0000_0000,
    ST =	0b0001_0000,
    MOV =	0b0010_0000,
    PUSH =	0b0110_0000,
    POP =	0b0111_0000,
    XCH =	0b1100_0000,

    // Jump
    JMP =	0b0010_1000,

    // Conditional Branch
    BP =	0b0110_1000,
}

enum Opcode : byte
{
	ROL =	0b11100000,
	ROLC =	0b11110000,
	ROR =	0b11000000,
	RORC =	0b11010000,
    MUL =	0b0011_0000,
    DIV =	0b0100_0000,

    // Data Transfer
    LDC =	0b1100_0001,

    // Jump
    JMPF =	0b0010_0001,
    BR =	0b0000_0001,
    BRF =	0b0001_0001,
    BZ =	0b1000_0000,
    BNZ =	0b1001_0000,

    // Misc
    NOP =	0b0000_0000,
}

static class OpcodePrefixExtensions
{
    public static OpcodePrefix GetOpcodePrefix(this byte b)
    {
        byte leftNybble = (byte)(b & 0b1111_1000);
        Debug.Assert(Enum.GetValues<OpcodePrefix>().Contains((OpcodePrefix)leftNybble));
        return (OpcodePrefix)leftNybble;
    }

    public static byte Compose(this OpcodePrefix prefix, AddressingMode mode)
    {
        return (byte)((byte)prefix | (byte)mode);
    }

    public static (byte first, byte second) ComposeJMP(ushort address)
    {
        // TODO: adopt a pattern for assembling/disassembling instructions which accommodates lots of different ways of encoding the operands
        // address must fit within 12 bits
        Debug.Assert((address & 0xf0_00) == 0);

        // turn 0000_aaaa of upper byte of address into 001a_1aaa of instruction
        bool bit11 = (address & 0x8_00) != 0;
        var first = (byte)((bit11 ? 0x08 : 0) | address >> 8 & 0x7 | (byte)OpcodePrefix.JMP);
        var second = (byte)address;
        return (first, second);
    }

    public static bool SupportsAddressingMode(this OpcodePrefix prefix, AddressingMode mode)
    {
        // TODO: introduce this for verification once more instructions are implemented
        return (prefix, mode) switch
        {
            _ => throw new NotImplementedException()
        };
    }
}