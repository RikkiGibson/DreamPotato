namespace VEmu.Core;

class Cpu
{
    // VMD-35: Accumulator and all registers are mapped to RAM.

    // VMD-38: Memory
    //

    /// <summary>Read-only memory space.</summary>
    private readonly byte[] ROM = new byte[64 * 1024];

    private readonly byte[] RamBank0 = new byte[0x1c0];

    private Span<byte> MainRam_0 => RamBank0.AsSpan(0..0x100);

    private short Pc;

    // VMD-39
    private Span<byte> IndirectAddressRegisters => RamBank0.AsSpan(0..0x10);

    // VMD-40, table 2.6
    private SpecialFunctionRegisters SFRs => new(RamBank0);

    /// <summary>LCD video XRAM, bank 0.</summary>
    private Span<byte> XRam_0 => RamBank0.AsSpan(0x180..0x1c0);

    private byte[] RamBank1 = new byte[0x1c0];
    private Span<byte> MainRam_1 => RamBank1.AsSpan(0..0x100);
    /// <summary>LCD video XRAM, bank 1.</summary>
    private Span<byte> XRam_1 => RamBank1.AsSpan(0x180..0x1c0);

    byte[] RamBank2 = new byte[0x1c0];
    /// <summary>LCD video XRAM, bank 2.</summary>
    private Span<byte> XRam_2 => RamBank1.AsSpan(0x180..0x190);

    private void Step()
    {
        // Fetch
        var prefix = ROM[Pc];
        switch (prefix)
        {
            case 0b10000001: // ADD immediate
                break;
        }
        // Decode
        // Execute
    }
}