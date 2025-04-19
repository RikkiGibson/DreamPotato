using System.Diagnostics;
using System.Numerics;

namespace VEmu.Core;

class Cpu
{
    // VMU-2: standalone instruction time 183us (microseconds).
    // Compare with 1us when connected to console.

    // VMD-35: Accumulator and all registers are mapped to RAM.

    // VMD-38: Memory
    //

    /// <summary>Read-only memory space.</summary>
    private readonly byte[] ROM = new byte[64 * 1024];

    private readonly byte[] RamBank0 = new byte[0x1c0]; // 448 dec

    private Span<byte> MainRam_0 => RamBank0.AsSpan(0..0x100);

    private short Pc;

    // VMD-39
    private Span<byte> IndirectAddressRegisters => RamBank0.AsSpan(0..0x10); // 16 dec


    // VMD-40, table 2.6
    private SpecialFunctionRegisters SFRs => new(RamBank0);

    /// <summary>LCD video XRAM, bank 0.</summary>
    private Span<byte> XRam_0 => RamBank0.AsSpan(0x180..0x1c0);

    private readonly byte[] RamBank1 = new byte[0x1c0];
    private Span<byte> MainRam_1 => RamBank1.AsSpan(0..0x100);
    /// <summary>LCD video XRAM, bank 1.</summary>
    private Span<byte> XRam_1 => RamBank1.AsSpan(0x180..0x1c0);

    private readonly byte[] RamBank2 = new byte[0x1c0];
    /// <summary>LCD video XRAM, bank 2.</summary>
    private Span<byte> XRam_2 => RamBank1.AsSpan(0x180..0x190);

    private void Step()
    {
        // Fetch
        byte prefix = ROM[Pc];
        switch (OpcodePrefixExtensions.GetPrefix(prefix))
        {
            case OpcodePrefix.ADD:
                Op_ADD();
                break;
        }
        // Decode
        // Execute
    }

    private void Op_ADD()
    {
        byte prefix = ROM[Pc];
        byte size;

        var mode = prefix & 0x0f;
        switch (mode)
        {
            case 0x01: // immediate
                size = 2;
                SFRs.Acc += ROM[Pc + 1];
                break;
            case 0x10: // direct
            case 0x11:
                {
                    size = 2;
                    // 9 bit address: oooommmd dddd_dddd
                    var address = ((prefix & 0x1f) << 8) | ROM[Pc + 1];
                    var bank = (SFRs.Psw & 0x10) == 0x10 ? RamBank1 : RamBank0;
                    SFRs.Acc += bank[address];
                    break;
                }
            case 0x100:
            case 0x110:
            case 0x101:
            case 0x111: // indirect
                {
                    size = 1;
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
                        Console.Error.WriteLine("Accessing nonexistent bank 3");
                        term = 0;
                    }
                    else if (bankId == 2)
                    {
                        Console.Error.WriteLine("Accessing bank 2, but no bounds checks are implemented");
                        term = RamBank2[address];
                    }
                    else
                    {
                        var bank = bankId switch { 0 => RamBank0, 1 => RamBank1, _ => throw new InvalidOperationException() };
                        term = bank[address];
                    }
                    SFRs.Acc += term;

                    break;
                }
            default:
                throw new InvalidOperationException();
        }
        Pc += size;
    }
}