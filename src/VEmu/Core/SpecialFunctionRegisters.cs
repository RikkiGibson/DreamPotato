namespace VEmu.Core;

/// <summary>See VMD-40, table 2.6</summary>
/// <remarks>this could be a 'readonly struct' once some language rules are relaxed, but, it doesn't really matter.</remarks>
public class SpecialFunctionRegisters(byte[] RamBank0)
{
    // TODO: these probably need to turn into get/set props.
    /// <summary>Accumulator. VMD-50</summary>
    public ref byte Acc => ref RamBank0[0x100];

    /// <summary>Program status word. VMD-52</summary>
    public ref byte Psw => ref RamBank0[0x101];

    /// <summary>
    /// Carry flag. VMD-45.
    /// </summary>
    /// <remarks>
    /// For arithmetic operations, carry can be thought of as "unsigned overflow".
    /// If an addition result exceeds 0xff, or a subtraction is less than 0, the carry flag is set.
    /// </remarks>
    public bool Cy
    {
        get => BitHelpers.ReadBit(Psw, bit: 7);
        set => BitHelpers.WriteBit(ref Psw, bit: 7, value);
    }

    /// <summary>
    /// Auxiliary carry flag. VMD-45.
    /// </summary>
    /// <remarks>
    /// This flag considers only the lower 4 bits of the operands. i.e. those bits which are retained by (value & 0xf).
    /// When an addition of the lower 4 bits of all operands exceeds 0xf, or a subtraction of the same is less than 0, the auxiliary carry flag is set.
    /// </remarks>
    public bool Ac
    {
        get => BitHelpers.ReadBit(Psw, bit: 6);
        set => BitHelpers.WriteBit(ref Psw, bit: 6, value);
    }

    /// <summary>Indirect address register bank flag 1. VMD-45</summary>
    public bool Irbk1
    {
        get => BitHelpers.ReadBit(Psw, bit: 4);
        set => BitHelpers.WriteBit(ref Psw, bit: 4, value);
    }

    /// <summary>Indirect address register bank flag 0. VMD-45</summary>
    public bool Irbk0
    {
        get => BitHelpers.ReadBit(Psw, bit: 3);
        set => BitHelpers.WriteBit(ref Psw, bit: 3, value);
    }

    /// <summary>
    /// Overflow flag. VMD-45.
    /// </summary>
    /// <remarks>
    /// This flag indicates whether "signed overflow" occurred in an arithmetic operation.
    /// (Keep in mind that whether operands between 128 and 255 are signed, i.e. are really between -1 and -128, depends on caller's interpretation.)
    /// It is set when the operation causes the accumulator to "travel across" from 127 to -128 of the signed range, in either direction.
    ///
    /// For example, imagine the operation as occurring on a number line, where A (accumulator) initially has value 127, and op has value 3.
    /// The result A1, if viewed as a signed number, has value -126. "Signed overflow" has occurred, so Ov is set.
    /// The same can occur for subtraction. for example if A is -128 and op is 1, then the result is 127, so Ov is set.
    ///  ... 127  -128  -127  -126 ...
    /// <-    -     -     -     -   ->
    ///       A                A1
    ///       op -------------->
    ///
    ///       A1    A
    ///       <---- op
    ///
    /// The same can occur when one or both operands are negative, e.g.
    /// for (-128) + (-1) = 127, or, for 126 - (-2) = -128.
    /// </remarks>
    public bool Ov
    {
        get => BitHelpers.ReadBit(Psw, bit: 2);
        set => BitHelpers.WriteBit(ref Psw, bit: 2, value);
    }

    /// <summary>RAM bank flag. VMD-45</summary>
    public bool Rambk0
    {
        get => BitHelpers.ReadBit(Psw, bit: 1);
        set => BitHelpers.WriteBit(ref Psw, bit: 1, value);
    }

    /// <summary>Accumulator (ACC) parity flag. VMD-45</summary>
    public bool P
    {
        get => BitHelpers.ReadBit(Psw, bit: 0);
        set => BitHelpers.WriteBit(ref Psw, bit: 0, value);
    }

    /// <summary>B register. VMD-51</summary>
    public ref byte B => ref RamBank0[0x102];
    /// <summary>C register. VMD-51</summary>
    public ref byte C => ref RamBank0[0x103];

    /// <summary>Table reference register lower byte. VMD-54</summary>
    public ref byte Trl => ref RamBank0[0x104];
    /// <summary>Table reference register upper byte. VMD-54</summary>
    public ref byte Trh => ref RamBank0[0x105];

    /// <summary>Stack pointer. VMD-53</summary>
    /// <remarks>Note that a well-behaved stack pointer always refers to 0x80 of RAM bank 0, growing upwards.</remarks>
    public ref byte Sp => ref RamBank0[0x106];

    /// <summary>Power control register. VMD-158</summary>
    public ref byte Pcon => ref RamBank0[0x107];

    /// <summary>Master interrupt enable control register. VMD-138</summary>
    public ref byte Ie => ref RamBank0[0x108];

    /// <summary>Interrupt priority control register. VMD-151</summary>
    public ref byte Ip => ref RamBank0[0x109];

    /// <summary>External memory control register. No VMD page</summary>
    public ref byte Ext => ref RamBank0[0x10d];

    /// <summary>Oscillation control register. VMD-156</summary>
    public ref byte Ocr => ref RamBank0[0x10e];

    /// <summary>Timer 0 control register. VMD-67</summary>
    public ref byte T0Cnt => ref RamBank0[0x110];

    /// <summary>Timer 0 prescaler data. VMD-71</summary>
    public ref byte T0Prr => ref RamBank0[0x111];

    /// <summary>Timer 0 low. VMD-71</summary>
    public ref byte T0L => ref RamBank0[0x112];

    /// <summary>Timer 0 low reload data. VMD-71</summary>
    public ref byte T0Lr => ref RamBank0[0x113];

    /// <summary>Timer 0 high. VMD-72</summary>
    public ref byte T0H => ref RamBank0[0x114];

    /// <summary>Timer 0 high reload data. VMD-72</summary>
    public ref byte T0Hr => ref RamBank0[0x115];

    /// <summary>Timer 1 control register. VMD-83</summary>
    public ref byte T1Cnt => ref RamBank0[0x118];

    /// <summary>Timer 1 low comparison data. VMD-86</summary>
    public ref byte T1Lc => ref RamBank0[0x11a];

    /// <summary>Timer 1 low. VMD-85</summary>
    public ref byte T1L => ref RamBank0[0x11b];

    /// <summary>Timer 1 low reload data. VMD-85</summary>
    public ref byte T1Lr => throw new NotImplementedException(); //ref RamBank0[0x???];

    /// <summary>Timer 1 high comparison data. VMD-87</summary>
    public ref byte T1Hc => ref RamBank0[0x11c];

    /// <summary>Timer 1 high. VMD-86</summary>
    public ref byte T1H => ref RamBank0[0x11d];

    /// <summary>Timer 1 high reload data. VMD-86</summary>
    public ref byte T1Hr => throw new NotImplementedException(); //ref RamBank0[0x];

    /// <summary>Mode control register. VMD-127</summary>
    public ref byte Mcr => ref RamBank0[0x120];

    /// <summary>Start address register. VMD-129</summary>
    public ref byte Stad => ref RamBank0[0x122];

    /// <summary>Character count register. VMD-130</summary>
    public ref byte Cnr => ref RamBank0[0x123];

    /// <summary>Time division register. VMD-130</summary>
    public ref byte Tdr => ref RamBank0[0x124];

    /// <summary>Bank address register. VMD-130</summary>
    public ref byte Xbnk => ref RamBank0[0x125];

    /// <summary>LCD contrast control register. VMD-131</summary>
    public ref byte Vccr => ref RamBank0[0x127];

    /// <summary>SIO0 control register. VMD-108</summary>
    public ref byte Scon0 => ref RamBank0[0x130];

    /// <summary>SIO0 buffer. VMD-113</summary>
    public ref byte Sbuf0 => ref RamBank0[0x131];

    /// <summary>SIO0 baud rate generator. VMD-113</summary>
    public ref byte Sbr => ref RamBank0[0x132];

    /// <summary>SIO1 control register. VMD-111</summary>
    public ref byte Scon1 => ref RamBank0[0x134];

    /// <summary>SIO1 buffer. VMD-113</summary>
    public ref byte Sbuf1 => ref RamBank0[0x135];

    /// <summary>Port 1 latch. VMD-58</summary>
    public ref byte P1 => ref RamBank0[0x144];

    /// <summary>Port 1 data direction register. VMD-58</summary>
    public ref byte P1Ddr => ref RamBank0[0x145];

    /// <summary>Port 1 function control register. VMD-59</summary>
    public ref byte P1Fcr => ref RamBank0[0x146];

    /// <summary>Port 3 latch. VMD-54</summary>
    public ref byte P3 => ref RamBank0[0x14C];

    // NB: application must set a button value to 1. When it is pressed, the bit is reset to 0.
    public bool ButtonSleep
    {
        get => BitHelpers.ReadBit(P3, bit: 7);
        set => BitHelpers.WriteBit(ref P3, bit: 7, value);
    }

    public bool ButtonMode
    {
        get => BitHelpers.ReadBit(P3, bit: 6);
        set => BitHelpers.WriteBit(ref P3, bit: 6, value);
    }

    public bool ButtonB
    {
        get => BitHelpers.ReadBit(P3, bit: 5);
        set => BitHelpers.WriteBit(ref P3, bit: 5, value);
    }

    public bool ButtonA
    {
        get => BitHelpers.ReadBit(P3, bit: 4);
        set => BitHelpers.WriteBit(ref P3, bit: 4, value);
    }

    public bool Right
    {
        get => BitHelpers.ReadBit(P3, bit: 3);
        set => BitHelpers.WriteBit(ref P3, bit: 3, value);
    }

    public bool Left
    {
        get => BitHelpers.ReadBit(P3, bit: 2);
        set => BitHelpers.WriteBit(ref P3, bit: 2, value);
    }

    public bool Down
    {
        get => BitHelpers.ReadBit(P3, bit: 1);
        set => BitHelpers.WriteBit(ref P3, bit: 1, value);
    }

    public bool Up
    {
        get => BitHelpers.ReadBit(P3, bit: 0);
        set => BitHelpers.WriteBit(ref P3, bit: 0, value);
    }

    /// <summary>Port 3 data direction register. VMD-62</summary>
    public ref byte P3Ddr => ref RamBank0[0x14D];

    /// <summary>Port 3 interrupt function control register. VMD-62</summary>
    public ref byte P3Int => ref RamBank0[0x14E];

    /// <summary>Flash Program Register. Undocumented.</summary>
    public ref byte FPR => ref RamBank0[0x154];

    /// <summary>Flash Address Bank. Used as the upper bit of the address for flash access, i.e. whether flash bank 0 or 1 is used.</summary>
    public bool FPR0
    {
        get => BitHelpers.ReadBit(FPR, bit: 0);
        set => BitHelpers.WriteBit(ref FPR, bit: 0, value);
    }

    /// <summary>Flash Write Unlock</summary>
    public bool FPR1
    {
        get => BitHelpers.ReadBit(FPR, bit: 1);
        set => BitHelpers.WriteBit(ref FPR, bit: 1, value);
    }

    /// <summary>Port 7 latch. VMD-64</summary>
    public ref byte P7 => ref RamBank0[0x15C];

    /// <summary>External interrupt 0, 1 control. VMD-135</summary>
    public ref byte I01Cr => ref RamBank0[0x15D];

    /// <summary>External interrupt 2, 3 control. VMD-137</summary>
    public ref byte I23Cr => ref RamBank0[0x15E];

    /// <summary>Input signal select. VMD-138</summary>
    public ref byte Isl => ref RamBank0[0x15F];

#region Work RAM
    /// <summary>Control register. VMD-143</summary>
    /// TODO: the application is only supposed to be able to alter bit 4.
    public ref byte Vsel => ref RamBank0[0x163];

    /// <summary>If set, increments Vramad (pair of Vramad1 and Vram</summary>
    public bool Vsel4_Ince
    {
        get => BitHelpers.ReadBit(Vsel, bit: 4);
        set => BitHelpers.WriteBit(ref Vsel, bit: 4, value);
    }

    /// <summary>Bits 0-7 of Vramad (work RAM address). VMD-144</summary>
    public ref byte Vrmad1 => ref RamBank0[0x164];

    /// <summary>Bit 8 of Vramad (work RAM address). VMD-144</summary>
    public ref byte Vrmad2 => ref RamBank0[0x165];

    /// <summary>Send/receive buffer. VMD-144</summary>
    public ref byte Vtrbf => ref RamBank0[0x166];
#endregion
    /// <summary>Base timer control. VMD-101</summary>
    public ref byte Btcr => ref RamBank0[0x17F];
}