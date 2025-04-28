

namespace VEmu.Core;

using static OpcodeMask;
using static AddressModeMask;
using System.Diagnostics;

static class InstructionDecoder
{
    public static Instruction Decode(ReadOnlySpan<byte> code, ushort offset)
    {
        var instruction = DecodeCore(code.Slice(offset), offset);
#if DEBUG
        // Enforce consistency between the DecodeCore switch and the opcode prefix definitions
        var opcode = instruction.Operation.Opcode.GetOpcodeMask();
        Debug.Assert((opcode & code[offset]) == opcode);
#endif
        return instruction;
    }

    /// <summary>
    /// Translates code at the given position to an instruction.
    /// Does not use any state from cpu etc to decide meaning of instructions.
    /// </summary>
    private static Instruction DecodeCore(ReadOnlySpan<byte> code, ushort offset)
    {
        switch (code[0])
        {
            case ADD | Immediate:
                return new(Operations.ADD_i8, [code[1]], offset);
            case ADD | Direct0 or ADD | Direct1:
                return new(Operations.ADD_d9, [DecodeD9(code)], offset);
            case >= (ADD | Indirect0) and <= (ADD | Indirect3):
                return new(Operations.ADD_Ri, [DecodeRi(code)], offset);

            case ADDC | Immediate:
                return new(Operations.ADDC_i8, [code[1]], offset);
            case ADDC | Direct0 or ADDC | Direct1:
                return new(Operations.ADDC_d9, [DecodeD9(code)], offset);
            case >= (ADDC | Indirect0) and <= (ADDC | Indirect3):
                return new(Operations.ADDC_Ri, [DecodeRi(code)], offset);

            case SUB | Immediate:
                return new(Operations.SUB_i8, [code[1]], offset);
            case SUB | Direct0 or SUB | Direct1:
                return new(Operations.SUB_d9, [DecodeD9(code)], offset);
            case >= (SUB | Indirect0) and <= (SUB | Indirect3):
                return new(Operations.SUB_Ri, [DecodeRi(code)], offset);

            case SUBC | Immediate:
                return new(Operations.SUBC_i8, [code[1]], offset);
            case SUBC | Direct0 or SUBC | Direct1:
                return new(Operations.SUBC_d9, [DecodeD9(code)], offset);
            case >= (SUBC | Indirect0) and <= (SUBC | Indirect3):
                return new(Operations.SUBC_Ri, [DecodeRi(code)], offset);

            case INC | Direct0 or INC | Direct1:
                return new(Operations.INC_d9, [DecodeD9(code)], offset);
            case >= (INC | Indirect0) and <= (INC | Indirect3):
                return new(Operations.INC_Ri, [DecodeRi(code)], offset);

            case DEC | Direct0 or DEC | Direct1:
                return new(Operations.DEC_d9, [DecodeD9(code)], offset);
            case >= (DEC | Indirect0) and <= (DEC | Indirect3):
                return new(Operations.DEC_Ri, [DecodeRi(code)], offset);

            case MUL: return new(Operations.MUL, [], offset);
            case DIV: return new(Operations.DIV, [], offset);

            case AND | Immediate:
                return new(Operations.AND_i8, [code[1]], offset);
            case AND | Direct0 or AND | Direct1:
                return new(Operations.AND_d9, [DecodeD9(code)], offset);
            case >= (AND | Indirect0) and <= (AND | Indirect3):
                return new(Operations.AND_Ri, [DecodeRi(code)], offset);

            case OR | Immediate:
                return new(Operations.OR_i8, [code[1]], offset);
            case OR | Direct0 or OR | Direct1:
                return new(Operations.OR_d9, [DecodeD9(code)], offset);
            case >= (OR | Indirect0) and <= (OR | Indirect3):
                return new(Operations.OR_Ri, [DecodeRi(code)], offset);

            case XOR | Immediate:
                return new(Operations.XOR_i8, [code[1]], offset);
            case XOR | Direct0 or XOR | Direct1:
                return new(Operations.XOR_d9, [DecodeD9(code)], offset);
            case >= (XOR | Indirect0) and <= (XOR | Indirect3):
                return new(Operations.XOR_Ri, [DecodeRi(code)], offset);

            case ROL: return new(Operations.ROL, [], offset);
            case ROLC: return new(Operations.ROLC, [], offset);
            case ROR: return new(Operations.ROR, [], offset);
            case RORC: return new(Operations.RORC, [], offset);

            case LD | Direct0 or LD | Direct1:
                return new(Operations.LD_d9, [DecodeD9(code)], offset);
            case >= (LD | Indirect0) and <= (LD | Indirect3):
                return new(Operations.LD_Ri, [DecodeRi(code)], offset);

            case ST | Direct0 or ST | Direct1:
                return new(Operations.ST_d9, [DecodeD9(code)], offset);
            case >= (ST | Indirect0) and <= (ST | Indirect3):
                return new(Operations.ST_Ri, [DecodeRi(code)], offset);

            // in binary, address comes first, then immediate
            case MOV | Direct0 or MOV | Direct1:
                return new(Operations.MOV_i8_d9, [code[2], DecodeD9(code)], offset);
            case >= (MOV | Indirect0) and <= (MOV | Indirect3):
                return new(Operations.MOV_i8_Rj, [code[2], DecodeRi(code)], offset);

            case LDC: return new(Operations.LDC, [], offset);
            case LDF: return new(Operations.LDF, [], offset);
            case STF: return new(Operations.STF, [], offset);

            case PUSH or PUSH | 1:
                return new(Operations.PUSH_d9, [DecodeD9(code)], offset);
            case POP or POP | 1:
                return new(Operations.POP_d9, [DecodeD9(code)], offset);

            case XCH | Direct0 or XCH | Direct1:
                return new(Operations.XCH_d9, [DecodeD9(code)], offset);
            case >= (XCH | Indirect0) and <= (XCH | Indirect3):
                return new(Operations.XCH_Ri, [DecodeRi(code)], offset);

            case >= JMP and <= (JMP | A10_9_8):
            case >= (JMP | A11) and <= (JMP | A11 | A10_9_8):
                return new(Operations.JMP_a12, [DecodeA12(code)], offset);

            case JMPF: return new(Operations.JMPF_a16, [DecodeA16(code)], offset);
            case BR: return new(Operations.BR_r8, [code[1]], offset);
            case BRF: return new(Operations.BRF_r16, [DecodeR16(code)], offset);
            case BZ: return new(Operations.BZ_r8, [code[1]], offset);
            case BNZ: return new(Operations.BNZ_r8, [code[1]], offset);

            case >= BP and <= (BP | B2_1_0):
            case >= (BP | D8) and <= (BP | D8 | B2_1_0):
            {
                var (d9, b3) = DecodeD9AndB3(code);
                return new(Operations.BP_d9_b3_r8, [d9, b3, code[2]], offset);
            }

            case >= BPC and <= (BPC | B2_1_0):
            case >= (BPC | D8) and <= (BPC | D8 | B2_1_0):
            {
                var (d9, b3) = DecodeD9AndB3(code);
                return new(Operations.BPC_d9_b3_r8, [d9, b3, code[2]], offset);
            }

            case >= BN and <= (BN | B2_1_0):
            case >= (BN | D8) and <= (BN | D8 | B2_1_0):
            {
                var (d9, b3) = DecodeD9AndB3(code);
                return new(Operations.BN_d9_b3_r8, [d9, b3, code[2]], offset);
            }

            case DBNZ | Direct0 or DBNZ | Direct1:
                return new(Operations.DBNZ_d9_r8, [DecodeD9(code), code[2]], offset);
            case >= (DBNZ | Indirect0) and <= (DBNZ | Indirect3):
                return new(Operations.DBNZ_Ri_r8, [DecodeRi(code), code[1]], offset);

            case BE | Immediate:
                return new(Operations.BE_i8_r8, [code[1], code[2]], offset);
            case BE | Direct0 or BE | Direct1:
                return new(Operations.BE_d9_r8, [DecodeD9(code), code[2]], offset);
            case >= (BE | Indirect0) and <= (BE | Indirect3):
                return new(Operations.BE_Ri_i8_r8, [DecodeRi(code), code[1], code[2]], offset);

            case BNE | Immediate:
                return new(Operations.BNE_i8_r8, [code[1], code[2]], offset);
            case BNE | Direct0 or BNE | Direct1:
                return new(Operations.BNE_d9_r8, [DecodeD9(code), code[2]], offset);
            case >= (BNE | Indirect0) and <= (BNE | Indirect3):
                return new(Operations.BNE_Ri_i8_r8, [DecodeRi(code), code[1], code[2]], offset);

            case >= CALL and <= (CALL | A10_9_8):
            case >= (CALL | A11) and <= (CALL | A11 | A10_9_8):
                return new(Operations.CALL_a12, [DecodeA12(code)], offset);

            case CALLF: return new(Operations.CALLF_a16, [DecodeA16(code)], offset);
            case CALLR: return new(Operations.CALLR_r16, [DecodeR16(code)], offset);

            case RET: return new(Operations.RET, [], offset);
            case RETI: return new(Operations.RETI, [], offset);

            case >= CLR1 and <= (CLR1 | B2_1_0):
            case >= (CLR1 | D8) and <= (CLR1 | D8 | B2_1_0):
            {
                var (d9, b3) = DecodeD9AndB3(code);
                return new(Operations.CLR1_d9_b3, [d9, b3], offset);
            }

            case >= SET1 and <= (SET1 | B2_1_0):
            case >= (SET1 | D8) and <= (SET1 | D8 | B2_1_0):
            {
                var (d9, b3) = DecodeD9AndB3(code);
                return new(Operations.SET1_d9_b3, [d9, b3], offset);
            }

            case >= NOT1 and <= (NOT1 | B2_1_0):
            case >= (NOT1 | D8) and <= (NOT1 | D8 | B2_1_0):
            {
                var (d9, b3) = DecodeD9AndB3(code);
                return new(Operations.NOT1_d9_b3, [d9, b3], offset);
            }

            case NOP: return new(Operations.NOP, [], offset);
        }
    }

    private static ushort DecodeD9(ReadOnlySpan<byte> code)
    {
        var address = (ushort)((BitHelpers.ReadBit(code[0], bit: 0) ? 0x100 : 0) | code[1]);
        return address;
    }

    private static ushort DecodeRi(ReadOnlySpan<byte> code)
    {
        var address = (ushort)(code[0] & 0b11);
        return address;
    }

    private static ushort DecodeA12(ReadOnlySpan<byte> code)
    {
        // a_1aaa aaaa_aaaa
        var addressHigh = (code[0] & A11) >> 1 | code[0] & A10_9_8;
        var address = (ushort)(addressHigh << 8 | code[1]);
        return address;
    }

    private static ushort DecodeA16(ReadOnlySpan<byte> code)
    {
        var address = (ushort)(code[1] << 8 | code[2]);
        return address;
    }

    private static ushort DecodeR16(ReadOnlySpan<byte> code)
    {
        // For whatever reason, small byte comes first with R16
        var address = (ushort)(code[1] | (code[2] << 8));
        return address;
    }

    private static (ushort D9, ushort B3) DecodeD9AndB3(ReadOnlySpan<byte> code)
    {
        // 000d_0bbb dddd_dddd
        var d9 = (ushort)((code[0] & D8) << 4 | code[1]);
        var b3 = (ushort)(code[0] & B2_1_0);
        return (d9, b3);
    }
}