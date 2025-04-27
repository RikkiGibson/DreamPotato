using System.Diagnostics;

namespace VEmu.Core;

using Ids = SpecialFunctionRegisterIds;

/// <summary>See VMD-40, table 2.6</summary>
public class SpecialFunctionRegisters
{
    public const int Size = 0x80;

    // TODO: Elysian docs state there are 143 SFRs, but, the memory space is only 0x80 (128 bytes).
    // Where are the extra 15?
    private readonly byte[] _rawMemory = new byte[Size];
    private readonly byte[] _workRam;

    public SpecialFunctionRegisters(byte[] workRam)
    {
        Debug.Assert(workRam.Length == 0x200);
        _workRam = workRam;
    }

    public byte Read(byte address)
    {
        Debug.Assert(address < Size);

        // TODO: there are many more special cases for reading/writing SFRs than this.
        switch (address)
        {
            case Ids.Vtrbf:
                return readWorkRam();
            default:
                return _rawMemory[address];
        }

        byte readWorkRam()
        {
            var address = (BitHelpers.ReadBit(Vrmad2, bit: 0) ? 0x100 : 0) | Vrmad1;
            var memory = _workRam[address];
            if (Vsel4_Ince)
            {
                address++;
                Vrmad1 = (byte)address;
                Vrmad2 = (byte)((address & 0x100) != 0 ? 1 : 0);
            }

            return memory;
        }
    }

    public void Write(byte address, byte value)
    {
        Debug.Assert(address < Size);

        switch (address)
        {
            case Ids.Vtrbf:
                writeWorkRam(value);
                return;
            default:
                _rawMemory[address] = value;
                return;
        }

        void writeWorkRam(byte value)
        {
            var address = (BitHelpers.ReadBit(Vrmad2, bit: 0) ? 0x100 : 0) | Vrmad1;
            _workRam[address] = value;

            if (Vsel4_Ince)
            {
                address++;
                Vrmad1 = (byte)address;
                Vrmad2 = (byte)((address & 0x100) != 0 ? 1 : 0);
            }
        }
    }

    // TODO: these all should probably be get/set props delegating to the main Read/Write methods.

    /// <summary>Accumulator. VMD-50</summary>
    public ref byte Acc => ref _rawMemory[Ids.Acc];

    /// <summary>Program status word. VMD-52</summary>
    public ref byte Psw => ref _rawMemory[Ids.Psw];

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
    public ref byte B => ref _rawMemory[Ids.B];
    /// <summary>C register. VMD-51</summary>
    public ref byte C => ref _rawMemory[Ids.C];

    /// <summary>Table reference register lower byte. VMD-54</summary>
    public ref byte Trl => ref _rawMemory[Ids.Trl];
    /// <summary>Table reference register upper byte. VMD-54</summary>
    public ref byte Trh => ref _rawMemory[Ids.Trh];

    /// <summary>Stack pointer. VMD-53</summary>
    /// <remarks>Note that a well-behaved stack pointer always refers to 0x80 of RAM bank 0, growing upwards.</remarks>
    public ref byte Sp => ref _rawMemory[Ids.Sp];

    /// <summary>Power control register. VMD-158</summary>
    public ref byte Pcon => ref _rawMemory[Ids.Pcon];

    /// <summary>Master interrupt enable control register. VMD-138</summary>
    public ref byte Ie => ref _rawMemory[Ids.Ie];

    /// <summary>Interrupt priority control register. VMD-151</summary>
    public ref byte Ip => ref _rawMemory[Ids.Ip];

    /// <summary>External memory control register. No VMD page</summary>
    public ref byte Ext => ref _rawMemory[Ids.Ext];

    /// <summary>Oscillation control register. VMD-156</summary>
    public ref byte Ocr => ref _rawMemory[Ids.Ocr];

    /// <summary>Timer 0 control register. VMD-67</summary>
    public ref byte T0Cnt => ref _rawMemory[Ids.T0Cnt];

    /// <summary>Timer 0 prescaler data. VMD-71</summary>
    public ref byte T0Prr => ref _rawMemory[Ids.T0Prr];

    /// <summary>Timer 0 low. VMD-71</summary>
    public ref byte T0L => ref _rawMemory[Ids.T0L];

    /// <summary>Timer 0 low reload data. VMD-71</summary>
    public ref byte T0Lr => ref _rawMemory[Ids.T0Lr];

    /// <summary>Timer 0 high. VMD-72</summary>
    public ref byte T0H => ref _rawMemory[Ids.T0H];

    /// <summary>Timer 0 high reload data. VMD-72</summary>
    public ref byte T0Hr => ref _rawMemory[Ids.T0Hr];

    /// <summary>Timer 1 control register. VMD-83</summary>
    public ref byte T1Cnt => ref _rawMemory[Ids.T1Cnt];

    /// <summary>Timer 1 low comparison data. VMD-86</summary>
    public ref byte T1Lc => ref _rawMemory[Ids.T1Lc];

    /// <summary>Timer 1 low. VMD-85</summary>
    public ref byte T1L => ref _rawMemory[Ids.T1L];

    /// <summary>Timer 1 low reload data. VMD-85</summary>
    public ref byte T1Lr => throw new NotImplementedException(); //ref RamBank0[N.T1Lr???];

    /// <summary>Timer 1 high comparison data. VMD-87</summary>
    public ref byte T1Hc => ref _rawMemory[Ids.T1Hc];

    /// <summary>Timer 1 high. VMD-86</summary>
    public ref byte T1H => ref _rawMemory[Ids.T1H];

    /// <summary>Timer 1 high reload data. VMD-86</summary>
    public ref byte T1Hr => throw new NotImplementedException(); //ref RamBank0[N.T1Hr];

    /// <summary>Mode control register. VMD-127</summary>
    public ref byte Mcr => ref _rawMemory[Ids.Mcr];

    /// <summary>Start address register. VMD-129</summary>
    public ref byte Stad => ref _rawMemory[Ids.Stad];

    /// <summary>Character count register. VMD-130</summary>
    public ref byte Cnr => ref _rawMemory[Ids.Cnr];

    /// <summary>Time division register. VMD-130</summary>
    public ref byte Tdr => ref _rawMemory[Ids.Tdr];

    /// <summary>Bank address register. Bits 1-0 control whether XRAM bank 0, 1, or 2 is in use. VMD-125</summary>
    public ref byte Xbnk => ref _rawMemory[Ids.Xbnk];

    /// <summary>LCD contrast control register. VMD-131</summary>
    public ref byte Vccr => ref _rawMemory[Ids.Vccr];

    /// <summary>SIO0 control register. VMD-108</summary>
    public ref byte Scon0 => ref _rawMemory[Ids.Scon0];

    /// <summary>SIO0 buffer. VMD-113</summary>
    public ref byte Sbuf0 => ref _rawMemory[Ids.Sbuf0];

    /// <summary>SIO0 baud rate generator. VMD-113</summary>
    public ref byte Sbr => ref _rawMemory[Ids.Sbr];

    /// <summary>SIO1 control register. VMD-111</summary>
    public ref byte Scon1 => ref _rawMemory[Ids.Scon1];

    /// <summary>SIO1 buffer. VMD-113</summary>
    public ref byte Sbuf1 => ref _rawMemory[Ids.Sbuf1];

    /// <summary>Port 1 latch. VMD-58</summary>
    public ref byte P1 => ref _rawMemory[Ids.P1];

    /// <summary>Port 1 data direction register. VMD-58</summary>
    public ref byte P1Ddr => ref _rawMemory[Ids.P1Ddr];

    /// <summary>Port 1 function control register. VMD-59</summary>
    public ref byte P1Fcr => ref _rawMemory[Ids.P1Fcr];

    /// <summary>Port 3 latch. VMD-54</summary>
    public ref byte P3 => ref _rawMemory[Ids.P3];

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
    public ref byte P3Ddr => ref _rawMemory[Ids.P3Ddr];

    /// <summary>Port 3 interrupt function control register. VMD-62</summary>
    public ref byte P3Int => ref _rawMemory[Ids.P3Int];

    /// <summary>Flash Program Register. Undocumented.</summary>
    public ref byte FPR => ref _rawMemory[Ids.FPR];

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
    public ref byte P7 => ref _rawMemory[Ids.P7];

    /// <summary>External interrupt 0, 1 control. VMD-135</summary>
    public ref byte I01Cr => ref _rawMemory[Ids.I01Cr];

    /// <summary>External interrupt 2, 3 control. VMD-137</summary>
    public ref byte I23Cr => ref _rawMemory[Ids.I23Cr];

    /// <summary>Input signal select. VMD-138</summary>
    public ref byte Isl => ref _rawMemory[Ids.Isl];

#region Work RAM
    /// <summary>Control register. VMD-143</summary>
    /// TODO: the application is only supposed to be able to alter bit 4.
    public ref byte Vsel => ref _rawMemory[Ids.Vsel];

    /// <summary>If set, increments Vramad (pair of Vramad1 and Vram</summary>
    public bool Vsel4_Ince
    {
        get => BitHelpers.ReadBit(Vsel, bit: 4);
        set => BitHelpers.WriteBit(ref Vsel, bit: 4, value);
    }

    /// <summary>Bits 0-7 of Vramad (work RAM address). VMD-144</summary>
    public ref byte Vrmad1 => ref _rawMemory[Ids.Vrmad1];

    /// <summary>Bit 8 of Vramad (work RAM address). VMD-144</summary>
    public ref byte Vrmad2 => ref _rawMemory[Ids.Vrmad2];

    /// <summary>Send/receive buffer. VMD-144</summary>
    public ref byte Vtrbf => ref _rawMemory[Ids.Vtrbf];
#endregion
    /// <summary>Base timer control. VMD-101</summary>
    public ref byte Btcr => ref _rawMemory[Ids.Btcr];
}