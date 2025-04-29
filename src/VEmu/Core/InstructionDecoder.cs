

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
        var opcode = instruction.Operation.Kind.GetOpcodeMask();
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
                return new(offset, Operations.ADD_i8, code[1]);
            case ADD | Direct0 or ADD | Direct1:
                return new(offset, Operations.ADD_d9, DecodeD9(code));
            case >= (ADD | Indirect0) and <= (ADD | Indirect3):
                return new(offset, Operations.ADD_Ri, DecodeRi(code));

            case ADDC | Immediate:
                return new(offset, Operations.ADDC_i8, code[1]);
            case ADDC | Direct0 or ADDC | Direct1:
                return new(offset, Operations.ADDC_d9, DecodeD9(code));
            case >= (ADDC | Indirect0) and <= (ADDC | Indirect3):
                return new(offset, Operations.ADDC_Ri, DecodeRi(code));

            case SUB | Immediate:
                return new(offset, Operations.SUB_i8, code[1]);
            case SUB | Direct0 or SUB | Direct1:
                return new(offset, Operations.SUB_d9, DecodeD9(code));
            case >= (SUB | Indirect0) and <= (SUB | Indirect3):
                return new(offset, Operations.SUB_Ri, DecodeRi(code));

            case SUBC | Immediate:
                return new(offset, Operations.SUBC_i8, code[1]);
            case SUBC | Direct0 or SUBC | Direct1:
                return new(offset, Operations.SUBC_d9, DecodeD9(code));
            case >= (SUBC | Indirect0) and <= (SUBC | Indirect3):
                return new(offset, Operations.SUBC_Ri, DecodeRi(code));

            case INC | Direct0 or INC | Direct1:
                return new(offset, Operations.INC_d9, DecodeD9(code));
            case >= (INC | Indirect0) and <= (INC | Indirect3):
                return new(offset, Operations.INC_Ri, DecodeRi(code));

            case DEC | Direct0 or DEC | Direct1:
                return new(offset, Operations.DEC_d9, DecodeD9(code));
            case >= (DEC | Indirect0) and <= (DEC | Indirect3):
                return new(offset, Operations.DEC_Ri, DecodeRi(code));

            case MUL: return new(offset, Operations.MUL);
            case DIV: return new(offset, Operations.DIV);

            case AND | Immediate:
                return new(offset, Operations.AND_i8, code[1]);
            case AND | Direct0 or AND | Direct1:
                return new(offset, Operations.AND_d9, DecodeD9(code));
            case >= (AND | Indirect0) and <= (AND | Indirect3):
                return new(offset, Operations.AND_Ri, DecodeRi(code));

            case OR | Immediate:
                return new(offset, Operations.OR_i8, code[1]);
            case OR | Direct0 or OR | Direct1:
                return new(offset, Operations.OR_d9, DecodeD9(code));
            case >= (OR | Indirect0) and <= (OR | Indirect3):
                return new(offset, Operations.OR_Ri, DecodeRi(code));

            case XOR | Immediate:
                return new(offset, Operations.XOR_i8, code[1]);
            case XOR | Direct0 or XOR | Direct1:
                return new(offset, Operations.XOR_d9, DecodeD9(code));
            case >= (XOR | Indirect0) and <= (XOR | Indirect3):
                return new(offset, Operations.XOR_Ri, DecodeRi(code));

            case ROL: return new(offset, Operations.ROL);
            case ROLC: return new(offset, Operations.ROLC);
            case ROR: return new(offset, Operations.ROR);
            case RORC: return new(offset, Operations.RORC);

            case LD | Direct0 or LD | Direct1:
                return new(offset, Operations.LD_d9, DecodeD9(code));
            case >= (LD | Indirect0) and <= (LD | Indirect3):
                return new(offset, Operations.LD_Ri, DecodeRi(code));

            case ST | Direct0 or ST | Direct1:
                return new(offset, Operations.ST_d9, DecodeD9(code));
            case >= (ST | Indirect0) and <= (ST | Indirect3):
                return new(offset, Operations.ST_Ri, DecodeRi(code));

            // in binary, address comes first, then immediate
            case MOV | Direct0 or MOV | Direct1:
                return new(offset, Operations.MOV_i8_d9, code[2], DecodeD9(code));
            case >= (MOV | Indirect0) and <= (MOV | Indirect3):
                return new(offset, Operations.MOV_i8_Rj, code[2], DecodeRi(code));

            case LDC: return new(offset, Operations.LDC);
            case LDF: return new(offset, Operations.LDF);
            case STF: return new(offset, Operations.STF);

            case PUSH or PUSH | 1:
                return new(offset, Operations.PUSH_d9, DecodeD9(code));
            case POP or POP | 1:
                return new(offset, Operations.POP_d9, DecodeD9(code));

            case XCH | Direct0 or XCH | Direct1:
                return new(offset, Operations.XCH_d9, DecodeD9(code));
            case >= (XCH | Indirect0) and <= (XCH | Indirect3):
                return new(offset, Operations.XCH_Ri, DecodeRi(code));

            case >= JMP and <= (JMP | A10_9_8):
            case >= (JMP | A11) and <= (JMP | A11 | A10_9_8):
                return new(offset, Operations.JMP_a12, DecodeA12(code));

            case JMPF: return new(offset, Operations.JMPF_a16, DecodeA16(code));
            case BR: return new(offset, Operations.BR_r8, code[1]);
            case BRF: return new(offset, Operations.BRF_r16, DecodeR16(code));
            case BZ: return new(offset, Operations.BZ_r8, code[1]);
            case BNZ: return new(offset, Operations.BNZ_r8, code[1]);

            case >= BP and <= (BP | B2_1_0):
            case >= (BP | D8) and <= (BP | D8 | B2_1_0):
            {
                var (d9, b3) = DecodeD9AndB3(code);
                return new(offset, Operations.BP_d9_b3_r8, d9, b3, code[2]);
            }

            case >= BPC and <= (BPC | B2_1_0):
            case >= (BPC | D8) and <= (BPC | D8 | B2_1_0):
            {
                var (d9, b3) = DecodeD9AndB3(code);
                return new(offset, Operations.BPC_d9_b3_r8, d9, b3, code[2]);
            }

            case >= BN and <= (BN | B2_1_0):
            case >= (BN | D8) and <= (BN | D8 | B2_1_0):
            {
                var (d9, b3) = DecodeD9AndB3(code);
                return new(offset, Operations.BN_d9_b3_r8, d9, b3, code[2]);
            }

            case DBNZ | Direct0 or DBNZ | Direct1:
                return new(offset, Operations.DBNZ_d9_r8, DecodeD9(code), code[2]);
            case >= (DBNZ | Indirect0) and <= (DBNZ | Indirect3):
                return new(offset, Operations.DBNZ_Ri_r8, DecodeRi(code), code[1]);

            case BE | Immediate:
                return new(offset, Operations.BE_i8_r8, code[1], code[2]);
            case BE | Direct0 or BE | Direct1:
                return new(offset, Operations.BE_d9_r8, DecodeD9(code), code[2]);
            case >= (BE | Indirect0) and <= (BE | Indirect3):
                return new(offset, Operations.BE_Ri_i8_r8, DecodeRi(code), code[1], code[2]);

            case BNE | Immediate:
                return new(offset, Operations.BNE_i8_r8, code[1], code[2]);
            case BNE | Direct0 or BNE | Direct1:
                return new(offset, Operations.BNE_d9_r8, DecodeD9(code), code[2]);
            case >= (BNE | Indirect0) and <= (BNE | Indirect3):
                return new(offset, Operations.BNE_Ri_i8_r8, DecodeRi(code), code[1], code[2]);

            case >= CALL and <= (CALL | A10_9_8):
            case >= (CALL | A11) and <= (CALL | A11 | A10_9_8):
                return new(offset, Operations.CALL_a12, DecodeA12(code));

            case CALLF: return new(offset, Operations.CALLF_a16, DecodeA16(code));
            case CALLR: return new(offset, Operations.CALLR_r16, DecodeR16(code));

            case RET: return new(offset, Operations.RET);
            case RETI: return new(offset, Operations.RETI);

            case >= CLR1 and <= (CLR1 | B2_1_0):
            case >= (CLR1 | D8) and <= (CLR1 | D8 | B2_1_0):
            {
                var (d9, b3) = DecodeD9AndB3(code);
                return new(offset, Operations.CLR1_d9_b3, d9, b3);
            }

            case >= SET1 and <= (SET1 | B2_1_0):
            case >= (SET1 | D8) and <= (SET1 | D8 | B2_1_0):
            {
                var (d9, b3) = DecodeD9AndB3(code);
                return new(offset, Operations.SET1_d9_b3, d9, b3);
            }

            case >= NOT1 and <= (NOT1 | B2_1_0):
            case >= (NOT1 | D8) and <= (NOT1 | D8 | B2_1_0):
            {
                var (d9, b3) = DecodeD9AndB3(code);
                return new(offset, Operations.NOT1_d9_b3, d9, b3);
            }

            case NOP: return new(offset, Operations.NOP);
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