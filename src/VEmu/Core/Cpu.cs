using System.Diagnostics;

namespace VEmu.Core;

public class Cpu
{
    // TODO: bring in a logger type with configurable severity etc
    // perhaps with a rolling buffer
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

    internal readonly InstructionMap InstructionMap = new();

    /// <summary>
    /// May point to either ROM (BIOS), flash memory bank 0 or bank 1.
    /// </summary>
    /// <remarks>
    /// Note that we need an extra bit of state here. We can't just look at the value of <see cref="SpecialFunctionRegisters.Ext"/>.
    /// The bank is only actually switched when using a jmpf instruction.
    /// </remarks>
    internal byte[] CurrentROMBank;

    internal readonly Memory Memory = new Memory();

    internal ushort Pc;

    public Cpu()
    {
        CurrentROMBank = ROM;
    }

    public byte ReadRam(int address)
    {
        Debug.Assert(address <= 0x200);
        return Memory.Read((ushort)address);
    }

    public void WriteRam(int address, byte value)
    {
        Debug.Assert(address <= 0x200);
        Memory.Write((ushort)address, value);
    }

    internal SpecialFunctionRegisters SFRs => Memory.SFRs;

    internal int Run(int cyclesToRun)
    {
        int cyclesSoFar = 0;
        while (cyclesSoFar < cyclesToRun)
        {
            cyclesSoFar += Step();
        }
        return cyclesSoFar;
    }

    /// <returns>Number of cycles consumed by the instruction.</returns>
    internal int Step()
    {
        if (BitHelpers.ReadBit(SFRs.Pcon, bit: 0))
        {
            Logger.WriteLine("---HALT---");
        }

        // TODO: cleanup the way opcode ranges are represented
        byte prefix = CurrentROMBank[Pc];

        var inst = InstructionDecoder.Decode(CurrentROMBank, Pc);
        InstructionMap[Pc] = inst;
        Logger.WriteLine($"{inst} Acc={SFRs.Acc:X} B={SFRs.B:X} C={SFRs.C:X} R2={ReadRam(2)}");

        switch ((Opcode)prefix)
        {
            case Opcode.MUL: return Op_MUL();
            case Opcode.DIV: return Op_DIV();
            case Opcode.ROL: return Op_ROL();
            case Opcode.ROLC: return Op_ROLC();
            case Opcode.ROR: return Op_ROR();
            case Opcode.RORC: return Op_RORC();

            case Opcode.LDC: return Op_LDC();

            case Opcode.JMPF: return Op_JMPF();
            case Opcode.BR: return Op_BR();
            case Opcode.BRF: return Op_BRF();
            case Opcode.BZ: return Op_BZ();
            case Opcode.BNZ: return Op_BNZ();

            case Opcode.CALLF: return Op_CALLF();
            case Opcode.CALLR: return Op_CALLR();
            case Opcode.RET: return Op_RET();

            case Opcode.LDF: return Op_LDF();
            case Opcode.NOP: return Op_NOP();
        }

        // the limited supported addressing modes of various ops are used to pack in more kinds of ops.
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
            case >= ((byte)OpcodePrefix.MOV | (byte)AddressingMode.Direct0) and <= ((byte)OpcodePrefix.MOV | (byte)AddressingMode.Indirect3): return Op_MOV();
            // TODO: LDC,...might go here

            case (byte)OpcodePrefix.PUSH or ((byte)OpcodePrefix.PUSH | 1): return Op_PUSH();
            case (byte)OpcodePrefix.POP or ((byte)OpcodePrefix.POP | 1): return Op_POP();
            case >= ((byte)OpcodePrefix.XCH | (byte)AddressingMode.Direct0) and <= ((byte)OpcodePrefix.XCH | (byte)AddressingMode.Indirect3): return Op_XCH();

            case (>= 0b0010_1000 and <= 0b0010_1111) or (>= 0b0011_1000 and <= 0b0011_1111): return Op_JMP();
            case (>= 0b0110_1000 and <= 0b0110_1111) or (>= 0b0111_1000 and <= 0b0111_1111): return Op_BP();
            case (>= 0b0100_1000 and <= 0b0100_1111) or (>= 0b0101_1000 and <= 0b0101_1111): return Op_BPC();
            case (>= 0b1000_1000 and <= 0b1000_1111) or (>= 0b1001_1000 and <= 0b1001_1111): return Op_BN();
            case >= ((byte)OpcodePrefix.DBNZ | (byte)AddressingMode.Direct0) and <= ((byte)OpcodePrefix.DBNZ | (byte)AddressingMode.Indirect3): return Op_DBNZ();
            case >= ((byte)OpcodePrefix.BE | (byte)AddressingMode.Immediate) and <= ((byte)OpcodePrefix.BE | (byte)AddressingMode.Indirect3): return Op_BE();
            case >= ((byte)OpcodePrefix.BNE | (byte)AddressingMode.Immediate) and <= ((byte)OpcodePrefix.BNE | (byte)AddressingMode.Indirect3): return Op_BNE();

            case (>= 0b0000_1000 and <= 0b0000_1111) or (>= 0b0001_1000 and <= 0b0001_1111): return Op_CALL();
            case (>= 0b1100_1000 and <= 0b1100_1111) or (>= 0b1101_1000 and <= 0b1101_1111): return Op_CLR1();
            case (>= 0b1110_1000 and <= 0b1110_1111) or (>= 0b1111_1000 and <= 0b1111_1111): return Op_SET1();
            case (>= 0b1010_1000 and <= 0b1010_1111) or (>= 0b1011_1000 and <= 0b1011_1111): return Op_NOT1();

            // Missing: STF (0101_0001)
            default: throw new NotImplementedException($"[0x{Pc:X}] Unknown prefix: 0x{prefix:X}");
        }
    }

    internal (byte operand, byte instructionSize) FetchOperand()
    {
        var prefix = CurrentROMBank[Pc];
        var mode = prefix & 0x0f;
        if (mode == 0b01) // immediate
            return (operand: CurrentROMBank[Pc + 1], instructionSize: 2);

        var address = GetOperandAddress(out var operandSize).address;
        return (Memory.Read(address), operandSize);
    }

    // TODO: different regions of memory are controlled by multiple banks.
    internal int GetCurrentBankId() => this.SFRs.Rambk0 ? 1 : 0;

    /// <param name="operandSize">how many bytes of instructions were used to represent this operand. e.g. 2 in direct or immediate mode, 1 in indirect mode. If this is the only argument to the instruction then it is usually the same as the instruction size.</param>
    internal (ushort address, int bankId) GetOperandAddress(out byte operandSize)
    {
        var prefix = CurrentROMBank[Pc];
        var mode = prefix & 0x0f;
        switch (mode)
        {
            case 0b01: // immediate
                Debug.Assert(false);
                operandSize = 0;
                return default;
            case 0b10: // direct
            case 0b11:
                {
                    // 9 bit address: oooo_mmmd dddd_dddd
                    var address = ((prefix & 0x1) << 8) | CurrentROMBank[Pc + 1];
                    operandSize = 2;
                    return ((ushort)address, GetCurrentBankId());
                }
            case 0b100:
            case 0b110:
            case 0b101:
            case 0b111: // indirect
                {
                    operandSize = 1;
                    return (Memory.ReadIndirectAddressRegister(prefix & 0b11), GetCurrentBankId());
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
        var address = GetOperandAddress(out var operandSize).address;
        var operand = ReadRam(address);
        operand++;
        WriteRam(address, operand);
        Pc += operandSize;
        return 1;
    }

    internal int Op_DEC()
    {
        // (operand) <- (operand) - 1
        // (could be either direct or indirect)
        var address = GetOperandAddress(out var operandSize).address;
        var operand = ReadRam(address);
        operand--;
        WriteRam(address, operand);
        Pc += operandSize;
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
        var address = GetOperandAddress(out var operandSize).address;
        WriteRam(address, SFRs.Acc);
        Pc += operandSize;
        return 1;
    }

    internal int Op_MOV()
    {
        // two operands: direct or indirect address, followed by immediate data.
        // TODO: perhaps some renaming here.
        // (d9) <- #i8
        var address = GetOperandAddress(out var operandSize).address;
        Pc += operandSize;
        WriteRam(address, CurrentROMBank[Pc]);
        Pc++;
        return operandSize; // instructionSize at this moment (1 less than true instruction size) also happens to be the cycle count.
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
        var dAddress = (ushort)(((CurrentROMBank[Pc] & 0x1) << 8) | CurrentROMBank[Pc + 1]);
        Memory.PushStack(Memory.Read(dAddress));

        Pc += 2;
        return 2;
    }

    internal int Op_POP()
    {
        // (d9) <- ((SP)), (SP) <- (SP) - 1
        var dAddress = ((CurrentROMBank[Pc] & 0x1) << 8) | CurrentROMBank[Pc + 1];
        WriteRam(dAddress, Memory.PopStack());

        Pc += 2;
        return 2;
    }

    internal int Op_XCH()
    {
        // (ACC) <--> (d9)
        // (ACC) <--> ((Rj)) j = 0, 1, 2, 3
        var address = GetOperandAddress(out var operandSize).address;
        var temp = ReadRam(address);
        WriteRam(address, SFRs.Acc);
        SFRs.Acc = temp;

        Pc += operandSize;
        return 1;
    }

    /// <summary>Jump near absolute address</summary>
    internal int Op_JMP()
    {
        // (PC) <- (PC) + 2, (PC11 to 00) <- a12
        // 001a_1aaa aaaa_aaaa
        byte prefix = CurrentROMBank[Pc];
        bool bit11 = (prefix & 0b1_0000) != 0;
        int a12 = (bit11 ? 0x800 : 0) | (prefix & 0b111) << 8 | CurrentROMBank[Pc + 1];
        Pc += 2;
        Pc &= 0b1111_0000__0000_0000;
        Pc |= (ushort)a12;
        return 2;
    }

    /// <summary>Jump far absolute address</summary>
    internal int Op_JMPF()
    {
        // (PC) <- a16
        Pc = (ushort)(CurrentROMBank[Pc + 1] << 8 | CurrentROMBank[Pc + 2]);
        // TODO: Set CurrentROMBank based on Ext
        return 2;
    }

    /// <summary>Branch near relative address</summary>
    internal int Op_BR()
    {
        // (PC) <- (PC) + 2, (PC) <- (PC) + r8
        var r8 = (sbyte)CurrentROMBank[Pc + 1];
        Pc = (ushort)(Pc + 2 + r8);
        return 2;
    }

    /// <summary>Branch far relative address</summary>
    internal int Op_BRF()
    {
        // (PC) <- (PC) + 3, (PC) <- (PC) - 1 + r16
        // NB: for some reason, this instruction is little endian (starts with least significant byte).
        var r16 = (ushort)(CurrentROMBank[Pc + 1] + (CurrentROMBank[Pc + 2] << 8));
        Pc = (ushort)(Pc + 3 - 1 + r16);

        return 4;
    }

    /// <summary>Branch near relative address if accumulator is zero</summary>
    internal int Op_BZ()
    {
        // (PC) <- (PC) + 2, if (ACC) = 0 then (PC) <- PC + r8
        var r8 = (sbyte)CurrentROMBank[Pc + 1];
        var z = SFRs.Acc == 0;

        Pc += 2;
        if (z)
            Pc = (ushort)(Pc + r8);

        return 2;
    }

    /// <summary>Branch near relative address if accumulator is not zero</summary>
    internal int Op_BNZ()
    {
        // (PC) <- (PC) + 2, if (ACC) != 0 then (PC) <- PC + r8
        var r8 = (sbyte)CurrentROMBank[Pc + 1];
        var nz = SFRs.Acc != 0;

        Pc += 2;
        if (nz)
            Pc = (ushort)(Pc + r8);

        return 2;
    }

    /// <summary>Branch near relative address if direct bit is one ("positive")</summary>
    internal int Op_BP()
    {
        // 3 operands: 'd' direct address, 'b' bit within (d), 'r' relative address to branch to
        // 011d_1bbb dddd_dddd rrrr_rrrr
        // (PC) <- (PC) + 3, if (d9, b3) = 1 then (PC) <- (PC) + r8
        var prefix = CurrentROMBank[Pc];
        var d9 = (BitHelpers.ReadBit(prefix, 4) ? 0x100 : 0) | CurrentROMBank[Pc + 1];
        var b3 = (byte)(prefix & 0b0111);
        var r8 = (sbyte)CurrentROMBank[Pc + 2];

        Pc += 3;
        if (BitHelpers.ReadBit(ReadRam(d9), b3))
            Pc = (ushort)(Pc + r8);

        return 2;
    }

    /// <summary>Branch near relative address if direct bit is one ("positive"), and clear</summary>
    internal int Op_BPC()
    {
        // When applied to port P1 and P3, the latch of each port is selected. The external signal is not selected.
        // When applied to port P7, there is no change in status.

        // 3 operands: 'd' direct address, 'b' bit within (d), 'r' relative address to branch to
        // 010d_1bbb dddd_dddd rrrr_rrrr
        // (PC) <- (PC) + 3, if (d9, b3) = 1 then (PC) <- (PC) + r8, (d9, b3) = 0
        var prefix = CurrentROMBank[Pc];
        var d9 = (BitHelpers.ReadBit(prefix, 4) ? 0x100 : 0) | CurrentROMBank[Pc + 1];
        var b3 = (byte)(prefix & 0b0111);
        var r8 = (sbyte)CurrentROMBank[Pc + 2];

        var d_value = ReadRam(d9);
        var new_d_value = d_value;
        BitHelpers.WriteBit(ref new_d_value, bit: b3, value: false);

        Pc += 3;
        if (d_value != new_d_value)
            Pc = (ushort)(Pc + r8);

        WriteRam(d9, new_d_value);

        return 2;
    }

    /// <summary>Branch near relative address if direct bit is zero ("negative")</summary>
    internal int Op_BN()
    {
        // 3 operands: 'd' direct address, 'b' bit within (d), 'r' relative address to branch to
        // 100d_1bbb dddd_dddd rrrr_rrrr
        // (PC) <- (PC) + 3, if (d9, b3) = 0 then (PC) <- (PC) + r8
        var prefix = CurrentROMBank[Pc];
        var d9 = (BitHelpers.ReadBit(prefix, 4) ? 0x100 : 0) | CurrentROMBank[Pc + 1];
        var b3 = (byte)(prefix & 0b0111);
        var r8 = (sbyte)CurrentROMBank[Pc + 2];

        Pc += 3;
        if (!BitHelpers.ReadBit(ReadRam(d9), b3))
            Pc = (ushort)(Pc + r8);

        return 2;
    }

    /// <summary>Decrement direct byte and branch near relative address if direct byte is nonzero</summary>
    internal int Op_DBNZ()
    {
        // (PC) <- (PC) + 3, (d9) = (d9)-1, if (d9) != 0 then (PC) <- (PC) + r8
        var d9 = GetOperandAddress(out var operandSize).address;
        var d9Value = ReadRam(d9);
        var r8 = (sbyte)CurrentROMBank[Pc + operandSize];

        Pc += (byte)(operandSize + 1); // 2 or 3 depending on addressing mode
        --d9Value;
        WriteRam(d9, d9Value);
        if (d9Value != 0)
            Pc = (ushort)(Pc + r8);

        return 2;
    }

    /// <summary>
    /// - Compare immediate data or direct byte to accumulator and branch near relative address if equal
    /// - Compare immediate data to indirect byte and branch near relative address if equal
    /// </summary>
    internal int Op_BE()
    {
        // (PC) <- (PC) + 3, if (ACC) = #i8 then (PC) <- (PC) + r8
        var indirectMode = BitHelpers.ReadBit(CurrentROMBank[Pc], bit: 2);
        // lhs is indirect byte in indirect mode, or acc in immediate or direct mode
        var lhs = indirectMode ? FetchOperand().operand : SFRs.Acc;
        // rhs is immediate data in indirect mode or immediate mode, and direct byte in direct mode
        var rhs = indirectMode ? CurrentROMBank[Pc + 1] : FetchOperand().operand;
        var r8 = (sbyte)CurrentROMBank[Pc + 2];

        Pc += 3;
        SFRs.Cy = lhs < rhs;
        if (lhs == rhs)
            Pc = (ushort)(Pc + r8);

        return 2;
    }

    /// <summary>
    /// - Compare immediate data or direct byte to accumulator and branch near relative address if not equal
    /// - Compare immediate data to indirect byte and branch near relative address if not equal
    /// </summary>
    internal int Op_BNE()
    {
        // (PC) <- (PC) + 3, if (ACC) != #i8 then (PC) <- (PC) + r8
        var indirectMode = BitHelpers.ReadBit(CurrentROMBank[Pc], bit: 2);
        // lhs is indirect byte in indirect mode, or acc in immediate or direct mode
        var lhs = indirectMode ? FetchOperand().operand : SFRs.Acc;
        // rhs is immediate data in indirect mode or immediate mode, and direct byte in direct mode
        var rhs = indirectMode ? CurrentROMBank[Pc + 1] : FetchOperand().operand;
        var r8 = (sbyte)CurrentROMBank[Pc + 2];

        Pc += 3;
        SFRs.Cy = lhs < rhs;
        if (lhs != rhs)
            Pc = (ushort)(Pc + r8);

        return 2;
    }

    /// <summary>Near absolute subroutine call</summary>
    internal int Op_CALL()
    {
        // similar to OP_JMP
        // (PC) <- (PC) + 2, (SP) <- (SP) + 1, ((SP)) <- (PC7 to 0), (SP) <- (SP) + 1, ((SP)) <- (PC15 to 8), (PC11 to 00) <- a12
        // 000a_1aaa aaaa_aaaa
        byte prefix = CurrentROMBank[Pc];
        bool a12_bit11 = BitHelpers.ReadBit(prefix, bit: 4);
        int a12 = (a12_bit11 ? 0x800 : 0) | (prefix & 0b111) << 8 | CurrentROMBank[Pc + 1];

        Pc += 2;
        Memory.PushStack((byte)Pc);
        Memory.PushStack((byte)(Pc >> 8));

        Pc &= 0b1111_0000__0000_0000;
        Pc |= (ushort)a12;
        return 2;
    }

    /// <summary>Far absolute subroutine call</summary>
    internal int Op_CALLF()
    {
        // Similar to Op_JMPF
        // (PC) <- (PC) + 3, (SP) <- (SP) + 1, ((SP)) <- (PC7 to 0),
        // (SP) <- (SP) + 1, ((SP)) <- (PC15 to 8), (PC) <- a16
        var a16 = (ushort)(CurrentROMBank[Pc + 1] << 8 | CurrentROMBank[Pc + 2]);
        Pc += 3;
        Memory.PushStack((byte)Pc);
        Memory.PushStack((byte)(Pc >> 8));
        Pc = a16;
        return 2;
    }

    /// <summary>Far relative subroutine call</summary>
    internal int Op_CALLR()
    {
        // (PC) <- (PC) + 3, (SP) <- (SP) + 1, ((SP)) <- (PC7 to 0),
        // (SP) <- (SP) + 1, ((SP)) <- (PC15 to 8), (PC) <- (PC) - 1 + r16
        // NB: for some reason, this instruction is little endian (starts with least significant byte).

        var r16 = (ushort)(CurrentROMBank[Pc + 1] + (CurrentROMBank[Pc + 2] << 8));
        Pc += 3;
        Memory.PushStack((byte)Pc);
        Memory.PushStack((byte)(Pc >> 8));
        Pc = (ushort)(Pc - 1 + r16);

        return 4;
    }

    /// <summary>Return from subroutine</summary>
    internal int Op_RET()
    {
        // (PC15 to 8) <- ((SP)), (SP) <- (SP) - 1, (PC7 to 0) <- ((SP)), (SP) <- (SP) -1
        var Pc15_8 = Memory.PopStack();
        var Pc7_0 = Memory.PopStack();
        Pc = (ushort)(Pc15_8 << 8 | Pc7_0);
        return 2;
    }

    // RETI

    /// <summary>Clear direct bit</summary>
    internal int Op_CLR1()
    {
        // (d9, b3) <- 0
        // Similar to Op_BP()
        var prefix = CurrentROMBank[Pc];
        var d9 = (BitHelpers.ReadBit(prefix, 4) ? 0x100 : 0) | CurrentROMBank[Pc + 1];
        var b3 = (byte)(prefix & 0b0111);
        var memory = ReadRam(d9);
        BitHelpers.WriteBit(ref memory, bit: b3, value: false);
        WriteRam(d9, memory);
        Pc += 2;
        return 1;
    }

    /// <summary>Set direct bit</summary>
    internal int Op_SET1()
    {
        // (d9, b3) <- 1
        var prefix = CurrentROMBank[Pc];
        var d9 = (BitHelpers.ReadBit(prefix, 4) ? 0x100 : 0) | CurrentROMBank[Pc + 1];
        var b3 = (byte)(prefix & 0b0111);
        var memory = ReadRam(d9);
        BitHelpers.WriteBit(ref memory, bit: b3, value: true);
        WriteRam(d9, memory);
        Pc += 2;
        return 1;
    }

    /// <summary>Not direct bit</summary>
    internal int Op_NOT1()
    {
        // (d9, b3) <- !(d9, b3)
        var prefix = CurrentROMBank[Pc];
        var d9 = (BitHelpers.ReadBit(prefix, 4) ? 0x100 : 0) | CurrentROMBank[Pc + 1];
        var b3 = (byte)(prefix & 0b0111);
        var memory = ReadRam(d9);
        var bit = BitHelpers.ReadBit(memory, b3);
        BitHelpers.WriteBit(ref memory, bit: b3, value: !bit);
        WriteRam(d9, memory);
        Pc += 2;
        return 1;
    }

    /// <summary>Load a value from flash memory into accumulator. Undocumented.</summary>
    internal int Op_LDF()
    {
        var a16 = SFRs.Trl | (SFRs.Trh << 8);
        var bank = SFRs.FPR0 ? FlashBank1 : FlashBank0;
        SFRs.Acc = bank[a16];
        Pc += 2;
        return 2;
    }

    // OP_STF

    /// <summary>No operation</summary>
    internal int Op_NOP()
    {
        Pc++;
        return 1;
    }
}