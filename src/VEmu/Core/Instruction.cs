

using System.Diagnostics;

namespace VEmu.Core;

// what does an instruction need to do?
// - disassemble, dude.
// what does it consist of?
// - everything which is statically known from the code. i.e. state of status registers etc must not factor in.
// - things which are statically known from the code should factor in.
// - should be able to hand one off to a cpu and execute.

// can we code up a set of "valid instruction symbols?"
// e.g. show which modes are valid. think of this as doing a lookup.

/// <summary>
/// Definition of an instruction, including opcode and parameters.
/// TODO: consider dropping allocs by baking in the fact that instructions have at most 3 parameters.
/// </summary>
record Operation(OperationKind Opcode, Parameter[] Parameters, byte Size, byte Cycles)
{
    public override string ToString()
    {
        // e.g. ADD I8
        return $"{Opcode} {string.Join(',', Parameters.Select(p => p.Kind))}";
    }
}

static class Operations
{
    public static Operation ADD_i8 = new(OperationKind.ADD, [new(ParameterKind.I8)], Size: 2, Cycles: 1);
    public static Operation ADD_d9 = new(OperationKind.ADD, [new(ParameterKind.D9)], Size: 2, Cycles: 1);
    public static Operation ADD_Ri = new(OperationKind.ADD, [new(ParameterKind.Ri)], Size: 1, Cycles: 1);

    public static Operation ADDC_i8 = new(OperationKind.ADDC, [new(ParameterKind.I8)], Size: 2, Cycles: 1);
    public static Operation ADDC_d9 = new(OperationKind.ADDC, [new(ParameterKind.D9)], Size: 2, Cycles: 1);
    public static Operation ADDC_Ri = new(OperationKind.ADDC, [new(ParameterKind.Ri)], Size: 1, Cycles: 1);

    public static Operation SUB_i8 = new(OperationKind.SUB, [new(ParameterKind.I8)], Size: 2, Cycles: 1);
    public static Operation SUB_d9 = new(OperationKind.SUB, [new(ParameterKind.D9)], Size: 2, Cycles: 1);
    public static Operation SUB_Ri = new(OperationKind.SUB, [new(ParameterKind.Ri)], Size: 1, Cycles: 1);

    public static Operation SUBC_i8 = new(OperationKind.SUBC, [new(ParameterKind.I8)], Size: 2, Cycles: 1);
    public static Operation SUBC_d9 = new(OperationKind.SUBC, [new(ParameterKind.D9)], Size: 2, Cycles: 1);
    public static Operation SUBC_Ri = new(OperationKind.SUBC, [new(ParameterKind.Ri)], Size: 1, Cycles: 1);

    public static Operation INC_d9 = new(OperationKind.INC, [new(ParameterKind.D9)], Size: 2, Cycles: 1);
    public static Operation INC_Ri = new(OperationKind.INC, [new(ParameterKind.Ri)], Size: 1, Cycles: 1);

    public static Operation DEC_d9 = new(OperationKind.DEC, [new(ParameterKind.D9)], Size: 2, Cycles: 1);
    public static Operation DEC_Ri = new(OperationKind.DEC, [new(ParameterKind.Ri)], Size: 1, Cycles: 1);

    public static Operation MUL = new(OperationKind.MUL, [], Size: 1, Cycles: 7);
    public static Operation DIV = new(OperationKind.DIV, [], Size: 1, Cycles: 7);

    public static Operation AND_i8 = new(OperationKind.AND, [new(ParameterKind.I8)], Size: 2, Cycles: 1);
    public static Operation AND_d9 = new(OperationKind.AND, [new(ParameterKind.D9)], Size: 2, Cycles: 1);
    public static Operation AND_Ri = new(OperationKind.AND, [new(ParameterKind.Ri)], Size: 1, Cycles: 1);

    public static Operation OR_i8 = new(OperationKind.OR, [new(ParameterKind.I8)], Size: 2, Cycles: 1);
    public static Operation OR_d9 = new(OperationKind.OR, [new(ParameterKind.D9)], Size: 2, Cycles: 1);
    public static Operation OR_Ri = new(OperationKind.OR, [new(ParameterKind.Ri)], Size: 1, Cycles: 1);

    public static Operation XOR_i8 = new(OperationKind.XOR, [new(ParameterKind.I8)], Size: 2, Cycles: 1);
    public static Operation XOR_d9 = new(OperationKind.XOR, [new(ParameterKind.D9)], Size: 2, Cycles: 1);
    public static Operation XOR_Ri = new(OperationKind.XOR, [new(ParameterKind.Ri)], Size: 1, Cycles: 1);

    public static Operation ROL = new(OperationKind.ROL, [], Size: 1, Cycles: 1);
    public static Operation ROLC = new(OperationKind.ROLC, [], Size: 1, Cycles: 1);
    public static Operation ROR = new(OperationKind.ROR, [], Size: 1, Cycles: 1);
    public static Operation RORC = new(OperationKind.RORC, [], Size: 1, Cycles: 1);

    public static Operation LD_d9 = new(OperationKind.LD, [new(ParameterKind.D9)], Size: 2, Cycles: 1);
    public static Operation LD_Ri = new(OperationKind.LD, [new(ParameterKind.Ri)], Size: 1, Cycles: 1);

    public static Operation ST_d9 = new(OperationKind.ST, [new(ParameterKind.D9)], Size: 2, Cycles: 1);
    public static Operation ST_Ri = new(OperationKind.ST, [new(ParameterKind.Ri)], Size: 1, Cycles: 1);

    public static Operation MOV_i8_d9 = new(OperationKind.MOV, [new(ParameterKind.I8), new(ParameterKind.D9)], Size: 3, Cycles: 2);
    public static Operation MOV_i8_Rj = new(OperationKind.MOV, [new(ParameterKind.I8), new(ParameterKind.Ri)], Size: 2, Cycles: 1);

    public static Operation LDC = new(OperationKind.LDC, [], Size: 1, Cycles: 2);
    public static Operation LDF = new(OperationKind.LDF, [], Size: 1, Cycles: 2);
    public static Operation STF = new(OperationKind.STF, [], Size: 1, Cycles: 2);

    public static Operation PUSH_d9 = new(OperationKind.PUSH, [new(ParameterKind.D9)], Size: 2, Cycles: 2);
    public static Operation POP_d9 = new(OperationKind.PUSH, [new(ParameterKind.D9)], Size: 2, Cycles: 2);

    public static Operation XCH_d9 = new(OperationKind.XCH, [new(ParameterKind.D9)], Size: 2, Cycles: 1);
    public static Operation XCH_Ri = new(OperationKind.XCH, [new(ParameterKind.Ri)], Size: 1, Cycles: 1);

    public static Operation JMP_a12 = new(OperationKind.JMP, [new(ParameterKind.A12)], Size: 2, Cycles: 2);
    public static Operation JMPF_a16 = new(OperationKind.JMPF, [new(ParameterKind.A16)], Size: 3, Cycles: 2);
    public static Operation BR_r8 = new(OperationKind.BR, [new(ParameterKind.R8)], Size: 2, Cycles: 2);
    public static Operation BRF_r16 = new(OperationKind.BRF, [new(ParameterKind.R16)], Size: 3, Cycles: 4);

    public static Operation BZ_r8 = new(OperationKind.BZ, [new(ParameterKind.R8)], Size: 2, Cycles: 2);
    public static Operation BNZ_r8 = new(OperationKind.BNZ, [new(ParameterKind.R8)], Size: 2, Cycles: 2);

    public static Operation BP_d9_b3_r8 = new(OperationKind.BP, [new(ParameterKind.D9), new(ParameterKind.B3), new(ParameterKind.R8)], Size: 3, Cycles: 2);
    public static Operation BPC_d9_b3_r8 = new(OperationKind.BPC, [new(ParameterKind.D9), new(ParameterKind.B3), new(ParameterKind.R8)], Size: 3, Cycles: 2);
    public static Operation BN_d9_b3_r8 = new(OperationKind.BN, [new(ParameterKind.D9), new(ParameterKind.B3), new(ParameterKind.R8)], Size: 3, Cycles: 2);

    public static Operation DBNZ_d9_r8 = new(OperationKind.DBNZ, [new(ParameterKind.D9), new(ParameterKind.R8)], Size: 3, Cycles: 2);
    public static Operation DBNZ_Ri_r8 = new(OperationKind.DBNZ, [new(ParameterKind.Ri), new(ParameterKind.R8)], Size: 2, Cycles: 2);

    public static Operation BE_i8_r8 = new(OperationKind.BE, [new(ParameterKind.I8), new(ParameterKind.R8)], Size: 3, Cycles: 2);
    public static Operation BE_d9_r8 = new(OperationKind.BE, [new(ParameterKind.D9), new(ParameterKind.R8)], Size: 3, Cycles: 2);
    public static Operation BE_Ri_i8_r8 = new(OperationKind.BE, [new(ParameterKind.Ri), new(ParameterKind.I8), new(ParameterKind.R8)], Size: 3, Cycles: 2);

    public static Operation BNE_i8_r8 = new(OperationKind.BNE, [new(ParameterKind.I8), new(ParameterKind.R8)], Size: 3, Cycles: 2);
    public static Operation BNE_d9_r8 = new(OperationKind.BNE, [new(ParameterKind.D9), new(ParameterKind.R8)], Size: 3, Cycles: 2);
    public static Operation BNE_Ri_i8_r8 = new(OperationKind.BNE, [new(ParameterKind.Ri), new(ParameterKind.I8), new(ParameterKind.R8)], Size: 3, Cycles: 2);

    public static Operation CALL_a12 = new(OperationKind.CALL, [new(ParameterKind.A12)], Size: 2, Cycles: 2);
    public static Operation CALLF_a16 = new(OperationKind.CALL, [new(ParameterKind.A16)], Size: 3, Cycles: 2);
    public static Operation CALLR_r16 = new(OperationKind.CALL, [new(ParameterKind.R16)], Size: 3, Cycles: 4);

    public static Operation RET = new(OperationKind.RET, [], Size: 1, Cycles: 2);
    public static Operation RETI = new(OperationKind.RETI, [], Size: 1, Cycles: 2);

    public static Operation CLR1_d9_b3 = new(OperationKind.CLR1, [new(ParameterKind.D9), new(ParameterKind.B3)], Size: 2, Cycles: 1);
    public static Operation SET1_d9_b3 = new(OperationKind.SET1, [new(ParameterKind.D9), new(ParameterKind.B3)], Size: 2, Cycles: 1);
    public static Operation NOT1_d9_b3 = new(OperationKind.NOT1, [new(ParameterKind.D9), new(ParameterKind.B3)], Size: 2, Cycles: 1);

    public static Operation NOP = new(OperationKind.NOP, [], Size: 1, Cycles: 1);
}

/// <summary>
/// The information we know about an instruction originating solely from the code, and not any cpu state.
/// </summary>
record struct Instruction(Operation Operation, ushort[] Arguments)
{
    public override string ToString()
    {
        // TODO: the most interesting/useful display is going to include some(?) cpu state.
        // e.g. if bank 0, can show built-in symbols for that bank. Same for bank 1.
        // Including cpu state really reflects a moment in time interpretation of the instruction though.
        // Same instruction could be run with different cpu states and mean different things.
        // Both forms of display are possibly useful.
        // We can include not only symbols, but, we can also include the values which are being modified, as well as whether branches are taken.
        // In other words logging the execution of hte program in quite useful detail.
        return $"{Operation.Opcode} {string.Join(',', Arguments.Select(a => a.ToString("X")))}";
    }
}

// TODO: it feels like an instruction should have a delegate like 'Execute(Cpu)'.
// To minimize garbage, maybe an operation should have 'Execute(Cpu, Arguments)'

// TODO: Consider allowing parameter names.
// TODO: display an instruction using parameterkind and argument to come up with a useful mnemonic.
record struct Parameter(ParameterKind Kind);

public enum ParameterKind
{
    None,

    /// <summary>8-bit immediate value</summary>
    I8,

    /// <summary>9-bit direct address</summary>
    D9,

    /// <summary>2-bit indirect address</summary>
    Ri,

    /// <summary>3-bit bit address</summary>
    B3,

    /// <summary>8-bit signed relative code address</summary>
    R8,

    /// <summary>16-bit unsigned relative code address</summary>
    R16,

    /// <summary>8-bit absolute code address</summary>
    A8,

    /// <summary>12-bit absolute code address</summary>
    A12,

    /// <summary>16-bit absolute code address</summary>
    A16,
}

enum OperationKind
{
    ADD,
    ADDC,
    SUB,
    SUBC,
    INC,
    DEC,
    MUL,
    DIV,
    AND,
    OR,
    XOR,
    ROL,
    ROLC,
    ROR,
    RORC,
    LD,
    ST,
    MOV,
    LDC,
    LDF,
    STF,
    PUSH,
    POP,
    XCH,
    JMP,
    JMPF,
    BR,
    BRF,
    BZ,
    BNZ,
    BP,
    BPC,
    BN,
    DBNZ,
    BE,
    BNE,
    CALL,
    CALLF,
    CALLR,
    RET,
    RETI,
    CLR1,
    SET1,
    NOT1,
    NOP
}

static class AddressModeMask
{
    public const byte Immediate = 0b1;
    public const byte Direct0 = 0b10;
    public const byte Direct1 = 0b11;
    public const byte Indirect0 = 0b100;
    public const byte Indirect1 = 0b101;
    public const byte Indirect2 = 0b110;
    public const byte Indirect3 = 0b111;

    public const byte A11 =		0b1_0000;
    public const byte A10_9_8 =	0b0_0111;

    // Same bit pattern can either hold 4 bits of A12, or, hold 1 bit of D9 and a B3
    public const byte D8 = A11;
    public const byte B2_1_0 = A10_9_8;
}

static class OpcodeMask
{
    // Arithmetic
    public const byte ADD =		0b1000_0000;
    public const byte ADDC =	0b1001_0000;
    public const byte SUB =		0b1010_0000;
    public const byte SUBC =	0b1011_0000;
    public const byte INC =		0b0110_0000;
    public const byte DEC =		0b0111_0000;

    public const byte ROL =		0b1110_0000;
    public const byte ROLC =	0b1111_0000;
    public const byte ROR =		0b1100_0000;
    public const byte RORC =	0b1101_0000;
    public const byte MUL =		0b0011_0000;
    public const byte DIV =		0b0100_0000;

    // Logical
    public const byte AND =		0b1110_0000;
    public const byte OR =		0b1101_0000;
    public const byte XOR =		0b1111_0000;

    // Data Transfer
    public const byte LD =		0b0000_0000;
    public const byte ST =		0b0001_0000;
    public const byte MOV =		0b0010_0000;
    public const byte PUSH =	0b0110_0000;
    public const byte POP =		0b0111_0000;
    public const byte XCH =		0b1100_0000;

    public const byte LDC =		0b1100_0001;

    // Jump
    public const byte JMP =		0b0010_1000;

    public const byte JMPF =	0b0010_0001;
    public const byte BR =		0b0000_0001;
    public const byte BRF =		0b0001_0001;
    public const byte BZ =		0b1000_0000;
    public const byte BNZ =		0b1001_0000;

    // Conditional Branch
    public const byte BP =		0b0110_1000;
    public const byte BPC =		0b0100_1000;
    public const byte BN =		0b1000_1000;
    public const byte DBNZ =	0b0101_0000;
    public const byte BE =		0b0011_0000;
    public const byte BNE =		0b0100_0000;

    // Subroutine
    public const byte CALL =	0b0000_1000;
    public const byte CALLF =	0b0010_0000;
    public const byte CALLR =	0b0001_0000;
    public const byte RET =		0b1010_0000;
    public const byte RETI =	0b1011_0000;

    // Bit Manipulation
    public const byte CLR1 =	0b1100_1000;
    public const byte SET1 =	0b1110_1000;
    public const byte NOT1 =	0b1010_1000;

    // Misc
    public const byte NOP =		0b0000_0000;

    // Undocumented
    public const byte LDF =		0b0101_0000;
    public const byte STF =		0b0101_0001;

    /// <summary>
    /// Gets the mask of the first byte of the instruction which identifies the operation kind.
    /// If the operation has no arguments, then this is usually the same as the instruction itself.
    /// Note also that these prefixes by themselves do not uniquely identify an operation kind.
    /// Encoding of the argument(s) also affects which operation it is treated as.
    /// </summary>
    internal static byte GetOpcodeMask(this OperationKind kind)
    {
        switch (kind)
        {
            case OperationKind.ADD:		return ADD;
            case OperationKind.ADDC:	return ADDC;
            case OperationKind.SUB:		return SUB;
            case OperationKind.SUBC:	return SUBC;
            case OperationKind.INC:		return INC;
            case OperationKind.DEC:		return DEC;
            case OperationKind.ROL:		return ROL;
            case OperationKind.ROLC:	return ROLC;
            case OperationKind.ROR:		return ROR;
            case OperationKind.RORC:	return RORC;
            case OperationKind.MUL:		return MUL;
            case OperationKind.DIV:		return DIV;
            case OperationKind.AND:		return AND;
            case OperationKind.OR:		return OR;
            case OperationKind.XOR:		return XOR;
            case OperationKind.LD:		return LD;
            case OperationKind.ST:		return ST;
            case OperationKind.MOV:		return MOV;
            case OperationKind.PUSH:	return PUSH;
            case OperationKind.POP:		return POP;
            case OperationKind.XCH:		return XCH;
            case OperationKind.LDC:		return LDC;
            case OperationKind.JMP:		return JMP;
            case OperationKind.JMPF:	return JMPF;
            case OperationKind.BR:		return BR;
            case OperationKind.BRF:		return BRF;
            case OperationKind.BZ:		return BZ;
            case OperationKind.BNZ:		return BNZ;
            case OperationKind.BP:		return BP;
            case OperationKind.BN:		return BN;
            case OperationKind.DBNZ:	return DBNZ;
            case OperationKind.BE:		return BE;
            case OperationKind.BNE:		return BNE;
            case OperationKind.CALLF:	return CALLF;
            case OperationKind.CALLR:	return CALLR;
            case OperationKind.RET:		return RET;
            case OperationKind.CLR1:	return CLR1;
            case OperationKind.SET1:	return SET1;
            case OperationKind.NOT1:	return NOT1;
            case OperationKind.NOP:		return NOP;
            case OperationKind.LDF:		return LDF;
            case OperationKind.STF:		return STF;
            default: throw new ArgumentException($"Unknown OperationKind: 0x{kind:X}");
        }
    }
}