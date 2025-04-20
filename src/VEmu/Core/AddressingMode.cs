namespace VEmu.Core;

internal enum AddressingMode : byte
{
    /// <summary>Immediate addressing. The instruction operand is embedded in the next byte of the instruction.</summary>
    Immediate = 0b001, // ooooommm iiiiiiii

    /// <summary>Direct addressing with 0 for the MSB of the address. The instruction operand is at the address indicated by 0b0_dddd_dddd.</summary>
    Direct0 = 0b010, // ooooommd dddddddd

    /// <summary>Direct addressing with 0 for the MSB of the address. The instruction operand is at the address indicated by 0b1_dddd_dddd.</summary>
    Direct1 = 0b011,

    /// <summary>Indirect addressing using indirect address register @R0.</summary>
    Indirect0 = 0b100, // ooooomjj

    /// <summary>Indirect addressing using indirect address register @R1.</summary>
    Indirect1 = 0b101,

    /// <summary>Indirect addressing using indirect address register @R2.</summary>
    Indirect2 = 0b110, // ooooomjj

    /// <summary>Indirect addressing using indirect address register @R3.</summary>
    Indirect3 = 0b111, // ooooomjj
}
