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

    internal readonly byte[] RamBank0 = new byte[0x1c0]; // 448 dec

    internal byte[] CurrentRamBank => SFRs.Rambk0 ? RamBank1 : RamBank0;

    internal Span<byte> MainRam_0 => RamBank0.AsSpan(0..0x100);

    internal short Pc;

    // VMD-39
    internal Span<byte> IndirectAddressRegisters => RamBank0.AsSpan(0..0x10); // 16 dec


    // VMD-40, table 2.6
    internal SpecialFunctionRegisters SFRs => new(RamBank0);

    /// <summary>LCD video XRAM, bank 0.</summary>
    internal Span<byte> XRam_0 => RamBank0.AsSpan(0x180..0x1c0);

    internal readonly byte[] RamBank1 = new byte[0x1c0];
    internal Span<byte> MainRam_1 => RamBank1.AsSpan(0..0x100);
    /// <summary>LCD video XRAM, bank 1.</summary>
    internal Span<byte> XRam_1 => RamBank1.AsSpan(0x180..0x1c0);

    internal readonly byte[] RamBank2 = new byte[0x1c0];
    /// <summary>LCD video XRAM, bank 2.</summary>
    internal Span<byte> XRam_2 => RamBank1.AsSpan(0x180..0x190);

    /// <returns>Number of cycles consumed by the instruction.</returns>
    internal int Step()
    {
        // TODO: probably number of cycles consumed should be returned here.
        byte prefix = ROM[Pc];
        switch (OpcodePrefixExtensions.GetPrefix(prefix))
        {
            case OpcodePrefix.ADD: return Op_ADD();
            case OpcodePrefix.ADDC: return Op_ADDC();
            case OpcodePrefix.SUB: return Op_SUB();
            case OpcodePrefix.SUBC: return Op_SUBC();
            case OpcodePrefix.INC: return Op_INC();
            default:
                throw new NotImplementedException();
        }
    }

    internal (byte operand, byte instructionSize) FetchArithmeticOperand()
    {
        var prefix = ROM[Pc];
        var mode = prefix & 0x0f;
        switch (mode)
        {
            case 0b01: // immediate
                return (operand: ROM[Pc + 1], instructionSize: 2);
            case 0b10: // direct
            case 0b11:
                {
                    // 9 bit address: oooommmd dddd_dddd
                    var address = ((prefix & 0x1) << 8) | ROM[Pc + 1];
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

    internal ref byte GetArithmeticOperand(out byte instructionSize)
    {
        var prefix = ROM[Pc];
        var mode = prefix & 0x0f;
        switch (mode)
        {
            case 0b01: // immediate
                instructionSize = 2;
                return ref ROM[Pc + 1];
            case 0b10: // direct
            case 0b11:
                {
                    // 9 bit address: oooommmd dddd_dddd
                    var address = ((prefix & 0x1) << 8) | ROM[Pc + 1];
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
        var (rhs, instructionSize) = FetchArithmeticOperand();
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
        var (rhs, instructionSize) = FetchArithmeticOperand();
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
        var (rhs, instructionSize) = FetchArithmeticOperand();
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
        var (rhs, instructionSize) = FetchArithmeticOperand();
        var lhs = SFRs.Acc;
        var carry = SFRs.Cy ? 1 : 0;
        var result = (byte)(lhs - carry - rhs);
        SFRs.Acc = result;

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
        ref var operand = ref GetArithmeticOperand(out var instructionSize);
        operand++;
        Pc += instructionSize;
        return 1;
    }
}