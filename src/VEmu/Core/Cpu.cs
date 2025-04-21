using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace VEmu.Core;

class Cpu
{
    internal TextWriter Logger { get => field ??= Console.Error; init; }

    // VMU-2: standalone instruction cycle time 183us (microseconds).
    // Compare with 1us when connected to console.

    // VMD-35: Accumulator and all registers are mapped to RAM.

    // VMD-38: Memory
    //

    /// <summary>Read-only memory space.</summary>
    internal readonly byte[] ROM = new byte[64 * 1024];
    internal readonly byte[] FlashBank0 = new byte[64 * 1024];
    internal readonly byte[] FlashBank1 = new byte[64 * 1024];

    /// <summary>
    /// May point to either ROM (BIOS), flash memory bank 0 or bank 1.
    /// </summary>
    /// <remarks>
    /// Note that we need an extra bit of state here. We can't just look at the value of <see cref="SpecialFunctionRegisters.Ext"/>.
    /// The bank is only actually switched when using a jmpf instruction.
    /// </remarks>
    internal byte[] CurrentROMBank;

    internal readonly byte[] RamBank0 = new byte[0x1c0]; // 448 dec
    internal readonly byte[] RamBank1 = new byte[0x1c0];
    internal readonly byte[] RamBank2 = new byte[0x1c0];

    internal short Pc;

    public Cpu()
    {
        CurrentROMBank = ROM;
    }

    internal byte[] CurrentRamBank => SFRs.Rambk0 ? RamBank1 : RamBank0;

    internal Span<byte> MainRam_0 => RamBank0.AsSpan(0..0x100);

    // VMD-39
    internal Span<byte> IndirectAddressRegisters => RamBank0.AsSpan(0..0x10); // 16 dec

    // VMD-40, table 2.6
    internal SpecialFunctionRegisters SFRs => new(RamBank0);

    /// <summary>LCD video XRAM, bank 0.</summary>
    internal Span<byte> XRam_0 => RamBank0.AsSpan(0x180..0x1c0);

    internal Span<byte> MainRam_1 => RamBank1.AsSpan(0..0x100);
    /// <summary>LCD video XRAM, bank 1.</summary>
    internal Span<byte> XRam_1 => RamBank1.AsSpan(0x180..0x1c0);

    /// <summary>LCD video XRAM, bank 2.</summary>
    internal Span<byte> XRam_2 => RamBank1.AsSpan(0x180..0x190);

    /// <returns>Number of cycles consumed by the instruction.</returns>
    internal int Step()
    {
        // TODO: review the bit patterns and decide whether to represent more of these using "OpcodePrefix"
        // Perhaps some nifty use of range patterns could help here, e.g. '>= (byte)OpcodePrefix.XOR | (byte)AddressingMode.Immediate and <= (byte)OpcodePrefix.XOR | (byte)AddressingMode.Indirect3
        byte prefix = CurrentROMBank[Pc];
        switch ((Opcode)prefix)
        {
            case Opcode.MUL: return Op_MUL();
            case Opcode.DIV: return Op_DIV();
            case Opcode.ROL: return Op_ROL();
            case Opcode.ROLC: return Op_ROLC();
            case Opcode.ROR: return Op_ROR();
            case Opcode.RORC: return Op_RORC();
            case Opcode.LDC: return Op_LDC();
        }

        // TODO: the limited supported addressing modes of various ops are used to pack in more kinds of ops.
        // e.g. INC does not support immediate mode, so that bit pattern is used for PUSH, which only supports direct mode.

        switch (prefix)
        {
            case >= ((byte)OpcodePrefix.ADD | (byte)AddressingMode.Immediate) and <= ((byte)OpcodePrefix.ADD | (byte)AddressingMode.Indirect3): return Op_ADD();
            case >= ((byte)OpcodePrefix.ADDC | (byte)AddressingMode.Immediate) and <= ((byte)OpcodePrefix.ADDC | (byte)AddressingMode.Indirect3): return Op_ADDC();
            case >= ((byte)OpcodePrefix.SUB | (byte)AddressingMode.Immediate) and <= ((byte)OpcodePrefix.SUB | (byte)AddressingMode.Indirect3): return Op_SUB();
            case >= ((byte)OpcodePrefix.SUBC | (byte)AddressingMode.Immediate) and <= ((byte)OpcodePrefix.SUBC | (byte)AddressingMode.Indirect3): return Op_SUBC();
            case >= ((byte)OpcodePrefix.INC | (byte)AddressingMode.Direct0) and <= ((byte)OpcodePrefix.INC | (byte)AddressingMode.Indirect3): return Op_INC();
            case >= ((byte)OpcodePrefix.DEC | (byte)AddressingMode.Direct0) and <= ((byte)OpcodePrefix.DEC | (byte)AddressingMode.Indirect3): return Op_DEC();
            // TODO: MUL, DIV might go here

            case >= ((byte)OpcodePrefix.AND | (byte)AddressingMode.Immediate) and <= ((byte)OpcodePrefix.AND | (byte)AddressingMode.Indirect3): return Op_AND();
            case >= ((byte)OpcodePrefix.OR | (byte)AddressingMode.Immediate) and <= ((byte)OpcodePrefix.OR | (byte)AddressingMode.Indirect3): return Op_OR();
            case >= ((byte)OpcodePrefix.XOR | (byte)AddressingMode.Immediate) and <= ((byte)OpcodePrefix.XOR | (byte)AddressingMode.Indirect3): return Op_XOR();
            // TODO: ROL, ROLC, ROR, RORC might go here

            case >= ((byte)OpcodePrefix.LD | (byte)AddressingMode.Immediate) and <= ((byte)OpcodePrefix.LD | (byte)AddressingMode.Indirect3): return Op_LD();
            case >= ((byte)OpcodePrefix.ST | (byte)AddressingMode.Immediate) and <= ((byte)OpcodePrefix.ST | (byte)AddressingMode.Indirect3): return Op_ST();
            case >= ((byte)OpcodePrefix.MOV | (byte)AddressingMode.Immediate) and <= ((byte)OpcodePrefix.MOV | (byte)AddressingMode.Indirect3): return Op_MOV();
            // TODO: LDC,...might go here

            case (byte)OpcodePrefix.PUSH or ((byte)OpcodePrefix.PUSH | 1): return Op_PUSH();
            case (byte)OpcodePrefix.POP or ((byte)OpcodePrefix.POP | 1): return Op_POP();

            default: throw new NotImplementedException();
        }
    }

    internal (byte operand, byte instructionSize) FetchOperand()
    {
        var prefix = CurrentROMBank[Pc];
        var mode = prefix & 0x0f;
        switch (mode)
        {
            case 0b01: // immediate
                return (operand: CurrentROMBank[Pc + 1], instructionSize: 2);
            case 0b10: // direct
            case 0b11:
                {
                    // 9 bit address: oooommmd dddd_dddd
                    var address = ((prefix & 0x1) << 8) | CurrentROMBank[Pc + 1];
                    return (operand: CurrentRamBank[address], instructionSize: 2);
                }
            case 0b100:
            case 0b110:
            case 0b101:
            case 0b111: // indirect
                {
                    // There are 16 indirect registers, each 1 byte in size.
                    // - bit 3: IRBK1
                    // - bit 2: IRBK0
                    // - bit 1: j1 (instruction data)
                    // - bit 0: j0 (instruction data)

                    var irbk = SFRs.Psw & 0b11000; // Mask out IRBK1, IRBK0 bits (VMD-44).
                    var bankId = irbk >> 3; // Normalize for reuse.
                    Debug.Assert(bankId is >= 0 and < 4);

                    var instructionBits = prefix & 0b11; // Mask out j1, j0 bits from instruction data.

                    var registerAddress = (irbk >> 1) | instructionBits; // compose (IRBK1, IRBK0, j1, j0)
                    Debug.Assert(registerAddress is >= 0 and < 16);

                    // 9-bit address, where the 9th bit is j1 from instruction data (indicating to check SFRs range 0x100-1x1ff)
                    var address = ((prefix & 0b10) == 0b10 ? 0b1_0000_0000 : 0)
                        | IndirectAddressRegisters[registerAddress];

                    byte term;
                    if (bankId == 3)
                    {
                        Logger.WriteLine($"[PC: 0x{Pc:X}] Accessing nonexistent bank 3");
                        term = 0;
                    }
                    else if (bankId == 2)
                    {
                        Logger.WriteLine($"[PC: 0x{Pc:X}] Accessing bank 2, but no bounds checks are implemented");
                        term = RamBank2[address];
                    }
                    else
                    {
                        var bank = bankId switch { 0 => RamBank0, 1 => RamBank1, _ => throw new InvalidOperationException() };
                        term = bank[address];
                    }
                    return (term, instructionSize: 1);
                }
            default:
                throw new InvalidOperationException();
        }
    }

    internal ref byte GetOperandRef(out byte instructionSize)
    {
        var prefix = CurrentROMBank[Pc];
        var mode = prefix & 0x0f;
        switch (mode)
        {
            case 0b01: // immediate
                instructionSize = 2;
                return ref CurrentROMBank[Pc + 1];
            case 0b10: // direct
            case 0b11:
                {
                    // 9 bit address: oooommmd dddd_dddd
                    var address = ((prefix & 0x1) << 8) | CurrentROMBank[Pc + 1];
                    instructionSize = 2;
                    return ref CurrentRamBank[address];
                }
            case 0b100:
            case 0b110:
            case 0b101:
            case 0b111: // indirect
                {
                    // There are 16 indirect registers, each 1 byte in size.
                    // - bit 3: IRBK1
                    // - bit 2: IRBK0
                    // - bit 1: j1 (instruction data)
                    // - bit 0: j0 (instruction data)

                    var irbk = SFRs.Psw & 0b11000; // Mask out IRBK1, IRBK0 bits (VMD-44).
                    var bankId = irbk >> 3; // Normalize for reuse.
                    Debug.Assert(bankId is >= 0 and < 4);

                    var instructionBits = prefix & 0b11; // Mask out j1, j0 bits from instruction data.

                    var registerAddress = (irbk >> 1) | instructionBits; // compose (IRBK1, IRBK0, j1, j0)
                    Debug.Assert(registerAddress is >= 0 and < 16);

                    // 9-bit address, where the 9th bit is j1 from instruction data (indicating to check SFRs range 0x100-1x1ff)
                    var address = ((prefix & 0b10) == 0b10 ? 0b1_0000_0000 : 0)
                        | IndirectAddressRegisters[registerAddress];

                    if (bankId == 3)
                    {
                        Logger.WriteLine($"[PC: 0x{Pc:X}] Accessing nonexistent bank 3");
                        instructionSize = 2;
                        unsafe
                        {
                            return ref Unsafe.AsRef<byte>(null);
                        }
                    }
                    else if (bankId == 2)
                    {
                        Logger.WriteLine($"[PC: 0x{Pc:X}] Accessing bank 2, but no bounds checks are implemented");
                        instructionSize = 1;
                        return ref RamBank2[address];
                    }
                    else
                    {
                        var bank = bankId switch { 0 => RamBank0, 1 => RamBank1, _ => throw new InvalidOperationException() };
                        instructionSize = 1;
                        return ref bank[address];
                    }
                }
            default:
                throw new InvalidOperationException();
        }
    }

    internal int Op_ADD()
    {
        // ACC <- ACC + operand
        var (rhs, instructionSize) = FetchOperand();
        var lhs = SFRs.Acc;
        var result = (byte)(lhs + rhs);
        SFRs.Acc = result;

        SFRs.Cy = result < lhs;
        SFRs.Ac = (lhs & 0xf) + (rhs & 0xf) > 0xf;

        // Overflow occurs if either:
        // - both operands had MSB set (i.e. were two's complement negative), but the result has the MSB cleared.
        // - both operands had MSB cleared (i.e. were two's complement positive), but the result has the MSB set.
        SFRs.Ov = (BitHelpers.ReadBit(lhs, bit: 7), BitHelpers.ReadBit(rhs, bit: 7), BitHelpers.ReadBit(result, bit: 7)) switch
        {
            (true, true, false) => true,
            (false, false, true) => true,
            _ => false
        };

        Pc += instructionSize;
        return 1;
    }

    internal int Op_ADDC()
    {
        // ACC <- ACC + CY + operand
        var (rhs, instructionSize) = FetchOperand();
        var lhs = SFRs.Acc;
        var carry = SFRs.Cy ? 1 : 0;
        var result = (byte)(lhs + carry + rhs);
        SFRs.Acc = result;

        SFRs.Cy = result < lhs;
        SFRs.Ac = (lhs & 0xf) + carry + (rhs & 0xf) > 0xf;

        // Overflow occurs if either:
        // - both operands had MSB set (i.e. were two's complement negative), but the result has the MSB cleared.
        // - both operands had MSB cleared (i.e. were two's complement positive), but the result has the MSB set.
        SFRs.Ov = (BitHelpers.ReadBit((byte)(lhs + carry), bit: 7), BitHelpers.ReadBit(rhs, bit: 7), BitHelpers.ReadBit(result, bit: 7)) switch
        {
            (true, true, false) => true,
            (false, false, true) => true,
            _ => false
        };

        Pc += instructionSize;
        return 1;
    }

    internal int Op_SUB()
    {
        // ACC <- ACC - operand
        var (rhs, instructionSize) = FetchOperand();
        var lhs = SFRs.Acc;
        var result = (byte)(lhs - rhs);
        SFRs.Acc = result;

        SFRs.Cy = lhs < rhs;
        SFRs.Ac = (lhs & 0xf) < (rhs & 0xf);

        // Overflow occurs if either:
        // - first operand has MSB set (negative number), second operand has MSB cleared (positive number), and the result has the MSB cleared (positive number).
        // - first operand has MSB cleared (positive number), second operand has MSB set (negative number), and the result has the MSB set (negative number).
        SFRs.Ov = (BitHelpers.ReadBit(lhs, bit: 7), BitHelpers.ReadBit(rhs, bit: 7), BitHelpers.ReadBit(result, bit: 7)) switch
        {
            (true, false, false) => true,
            (false, true, true) => true,
            _ => false
        };

        Pc += instructionSize;
        return 1;
    }

    internal int Op_SUBC()
    {
        // ACC <- ACC - CY - operand
        var (rhs, instructionSize) = FetchOperand();
        var lhs = SFRs.Acc;
        var carry = SFRs.Cy ? 1 : 0;
        var result = (byte)(lhs - carry - rhs);
        SFRs.Acc = result;

        // Carry is set when the subtraction yields a negative result.
        SFRs.Cy = lhs - carry - rhs < 0;
        SFRs.Ac = (lhs & 0xf) - carry - (rhs & 0xf) < 0;

        // Overflow occurs if either:
        // - subtracting a negative changes the sign from negative to positive
        // - first operand has MSB set (negative number), second operand has MSB cleared (positive number), and the result has the MSB cleared (positive number).
        // - first operand has MSB cleared (positive number), second operand has MSB set (negative number), and the result has the MSB set (negative number).
        SFRs.Ov = (BitHelpers.ReadBit((byte)(lhs + carry), bit: 7), BitHelpers.ReadBit(rhs, bit: 7), BitHelpers.ReadBit(result, bit: 7)) switch
        {
            (true, false, false) => true,
            (false, true, true) => true,
            _ => false
        };

        Pc += instructionSize;
        return 1;
    }

    internal int Op_INC()
    {
        // (operand) <- (operand) + 1
        // (could be either direct or indirect)
        ref var operand = ref GetOperandRef(out var instructionSize);
        operand++;
        Pc += instructionSize;
        return 1;
    }

    internal int Op_DEC()
    {
        // (operand) <- (operand) - 1
        // (could be either direct or indirect)
        ref var operand = ref GetOperandRef(out var instructionSize);
        operand--;
        Pc += instructionSize;
        return 1;
    }

    internal int Op_MUL()
    {
        // TODO: interrupts enabled on the 7th cycle.
        // Have to consider when implementing interrupts.

        // (B) (ACC) (C) <- (ACC) (C) * (B)
        int result = (SFRs.Acc << 0x8 | SFRs.C) * SFRs.B;
        SFRs.B = (byte)(result >> 0x10); // Casting to byte just takes the 8 least significant bits of the expression
        SFRs.Acc = (byte)(result >> 0x8);
        SFRs.C = (byte)result;

        // Overflow cleared indicates the result can fit into 16 bits, i.e. B is 0.
        SFRs.Ov = SFRs.B != 0;
        SFRs.Cy = false;
        Pc += 1;
        return 7;
    }

    internal int Op_DIV()
    {
        // (ACC) (C), mod(B) <- (ACC) (C) / (B)
        if (SFRs.B == 0)
        {
            SFRs.Acc = 0xff;
            SFRs.Ov = true;
        }
        else
        {
            int lhs = SFRs.Acc << 0x8 | SFRs.C;
            int result = lhs / SFRs.B;
            int mod = lhs % SFRs.B;

            SFRs.Acc = (byte)(result >> 0x8);
            SFRs.C = (byte)result;
            SFRs.B = (byte)mod;
            SFRs.Ov = false;
        }
        SFRs.Cy = false;
        Pc += 1;
        return 7;
    }

    internal int Op_AND()
    {
        // ACC <- ACC & operand
        var (rhs, instructionSize) = FetchOperand();
        SFRs.Acc &= rhs;
        Pc += instructionSize;
        return 1;
    }


    internal int Op_OR()
    {
        // ACC <- ACC | operand
        var (rhs, instructionSize) = FetchOperand();
        SFRs.Acc |= rhs;
        Pc += instructionSize;
        return 1;
    }
    internal int Op_XOR()
    {
        // ACC <- ACC ^ operand
        var (rhs, instructionSize) = FetchOperand();
        SFRs.Acc ^= rhs;
        Pc += instructionSize;
        return 1;
    }

    internal int Op_ROL()
    {
        // <-A7<-A6<-A5<-A4<-A3<-A2<-A1<-A0
        int shifted = SFRs.Acc << 1;
        bool bit0 = (shifted & 0x100) != 0;
        SFRs.Acc = (byte)(shifted | (bit0 ? 1 : 0));
        Pc += 1;
        return 1;
    }

    internal int Op_ROLC()
    {
        // <-A7<-A6<-A5<-A4<-A3<-A2<-A1<-A0<-CY<- (A7)
        int shifted = SFRs.Acc << 1 | (SFRs.Cy ? 1 : 0);
        SFRs.Cy = (shifted & 0x100) != 0;
        SFRs.Acc = (byte)shifted;
        Pc += 1;
        return 1;
    }

    internal int Op_ROR()
    {
        // (A0) ->A7->A6->A5->A4->A3->A2->A1->A0
        bool bit7 = (SFRs.Acc & 1) != 0;
        SFRs.Acc = (byte)((SFRs.Acc >> 1) | (bit7 ? 0x80 : 0));
        Pc += 1;
        return 1;
    }


    internal int Op_RORC()
    {
        // (A0) ->CY->A7->A6->A5->A4->A3->A2->A1->A0
        bool newCarry = (SFRs.Acc & 1) != 0;
        SFRs.Acc = (byte)((SFRs.Cy ? 0x80 : 0) | SFRs.Acc >> 1);
        SFRs.Cy = newCarry;
        Pc += 1;
        return 1;
    }

    internal int Op_LD()
    {
        // (ACC) <- (d9)
        (SFRs.Acc, var instructionSize) = FetchOperand();
        Pc += instructionSize;
        return 1;
    }

    internal int Op_ST()
    {
        // (d9) <- (ACC)
        GetOperandRef(out var instructionSize) = SFRs.Acc;
        Pc += instructionSize;
        return 1;
    }

    internal int Op_MOV()
    {
        // two operands: direct or indirect address, followed by immediate data.
        // TODO: perhaps some renaming here.
        // (d9) <- #i8
        ref var dest = ref GetOperandRef(out var instructionSize);
        Pc += instructionSize;
        dest = CurrentROMBank[Pc];
        Pc++;
        return instructionSize; // instructionSize at this moment (1 less than true instruction size) also happens to be the cycle count.
    }

    internal int Op_LDC()
    {
        // (ACC) <- (BNK)((TRR) + (ACC)) [ROM]
        // For a program running in ROM, ROM is accessed.
        // For a program running in flash memory, bank 0 of flash memory is accessed.
        // Cannot access bank 1 of flash memory. System BIOS function must be used instead.
        var address = ((SFRs.Trh << 8) | SFRs.Trl) + SFRs.Acc;
        SFRs.Acc = CurrentROMBank[address];
        Pc++;
        return 2;
    }

    internal int Op_PUSH()
    {
        // (SP) <- (SP) + 1, ((SP)) <- d9
        SFRs.Sp++;
        var dAddress = ((CurrentROMBank[Pc] & 0x1) << 8) | CurrentROMBank[Pc + 1];
        RamBank0[SFRs.Sp] = CurrentRamBank[dAddress];

        Pc += 2;
        return 2;
    }

    internal int Op_POP()
    {
        // (d9) <- ((SP)), (SP) <- (SP) - 1
        var dAddress = ((CurrentROMBank[Pc] & 0x1) << 8) | CurrentROMBank[Pc + 1];
        CurrentRamBank[dAddress] = RamBank0[SFRs.Sp];
        SFRs.Sp--;

        Pc += 2;
        return 2;
    }
}