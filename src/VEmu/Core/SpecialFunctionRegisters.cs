using System.Diagnostics;

namespace VEmu.Core;

using Ids = SpecialFunctionRegisterIds;

/// <summary>See VMD-40, table 2.6</summary>
class SpecialFunctionRegisters
{
    public const int Size = 0x80;

    // TODO: Elysian docs state there are 143 SFRs, but, the memory space is only 0x80 (128 bytes).
    // Where are the extra 15?
    private readonly byte[] _rawMemory = new byte[Size];
    private readonly Cpu _cpu;
    private readonly byte[] _workRam;
    private readonly Logger _logger;

    public SpecialFunctionRegisters(Cpu cpu, byte[] workRam, Logger logger)
    {
        Debug.Assert(workRam.Length == 0x200);
        _cpu = cpu;
        _workRam = workRam;
        _logger = logger;
    }

    /// <summary>
    /// VMD-40
    /// </summary>
    public void Reset()
    {
        Array.Clear(_rawMemory);
        // NB: Memory owns clearing _workRam

        // Manual indicates that BIOS is typically responsible for setting these values.
        // It's nice to be able to run without a BIOS, so let's set them up here.
        P1Fcr = 0b1011_1111;
        P3Int = 0b1111_1101;
        Isl = 0b1100_0000;
        Vsel = 0b1111_1100;
        Btcr = 0b0100_0001;
        Sp = Memory.StackStart;
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

    /// <summary>Accumulator. VMD-50</summary>
    public byte Acc
    {
        get => Read(Ids.Acc);
        set => Write(Ids.Acc, value);
    }

    /// <summary>Program status word. VMD-52</summary>
    public byte Psw
    {
        get => Read(Ids.Psw);
        set => Write(Ids.Psw, value);
    }

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
        set => Psw = BitHelpers.WithBit(Psw, bit: 7, value);
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
        set => Psw = BitHelpers.WithBit(Psw, bit: 6, value);
    }

    /// <summary>Indirect address register bank flag 1. VMD-45</summary>
    public bool Irbk1
    {
        get => BitHelpers.ReadBit(Psw, bit: 4);
        set => Psw = BitHelpers.WithBit(Psw, bit: 4, value);
    }

    /// <summary>Indirect address register bank flag 0. VMD-45</summary>
    public bool Irbk0
    {
        get => BitHelpers.ReadBit(Psw, bit: 3);
        set => Psw = BitHelpers.WithBit(Psw, bit: 3, value);
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
        set => Psw = BitHelpers.WithBit(Psw, bit: 2, value);
    }

    /// <summary>When true, main memory access uses bank 1, otherwise it uses bank 0. RAM bank flag. VMD-45</summary>
    public bool Rambk0
    {
        get => BitHelpers.ReadBit(Psw, bit: 1);
        set => Psw = BitHelpers.WithBit(Psw, bit: 1, value);
    }

    /// <summary>Accumulator (ACC) parity flag. VMD-45</summary>
    public bool P
    {
        get => BitHelpers.ReadBit(Psw, bit: 0);
        set => Psw = BitHelpers.WithBit(Psw, bit: 0, value);
    }

    /// <summary>B register. VMD-51</summary>
    public byte B
    {
        get => Read(Ids.B);
        set => Write(Ids.B, value);
    }
    /// <summary>C register. VMD-51</summary>
    public byte C
    {
        get => Read(Ids.C);
        set => Write(Ids.C, value);
    }

    /// <summary>Table reference register lower byte. VMD-54</summary>
    public byte Trl
    {
        get => Read(Ids.Trl);
        set => Write(Ids.Trl, value);
    }
    /// <summary>Table reference register upper byte. VMD-54</summary>
    public byte Trh
    {
        get => Read(Ids.Trh);
        set => Write(Ids.Trh, value);
    }

    /// <summary>Stack pointer. VMD-53</summary>
    /// <remarks>Note that a well-behaved stack pointer always refers to 0x80 of RAM bank 0, growing upwards.</remarks>
    public byte Sp
    {
        get => Read(Ids.Sp);
        set => Write(Ids.Sp, value);
    }

    /// <summary>Power control register. VMD-158</summary>
    public byte Pcon
    {
        get => Read(Ids.Pcon);
        set => Write(Ids.Pcon, value);
    }

    /// <summary>Master interrupt enable control register. VMD-138</summary>
    public byte Ie
    {
        get => Read(Ids.Ie);
        set => Write(Ids.Ie, value);
    }

    /// <summary>Master interrupt enable control. VMD-133.</summary>
    public bool Ie7_MasterInterruptEnable
    {
        get => BitHelpers.ReadBit(Ie, bit: 7);
        set => Ie = BitHelpers.WithBit(Ie, bit: 7, value);
    }

    /// <summary>
    /// Controls priority level of external interrupts. VMD-134.
    /// IE1, IE0    INT1 priority   INT0 priority
    /// 0,   0      Highest         Highest
    /// 1,   0      Low             Highest
    /// X,   1      Low             Low
    /// </summary>
    public bool Ie1
    {
        get => BitHelpers.ReadBit(Ie, bit: 1);
        set => Ie = BitHelpers.WithBit(Ie, bit: 1, value);
    }

    /// <inheritdoc cref="Ie1" />
    public bool Ie0
    {
        get => BitHelpers.ReadBit(Ie, bit: 0);
        set => Ie = BitHelpers.WithBit(Ie, bit: 0, value);
    }

    /// <summary>Interrupt priority control register. VMD-151</summary>
    public byte Ip
    {
        get => Read(Ids.Ip);
        set => Write(Ids.Ip, value);
    }

    /// <summary>External memory control register. No VMD page</summary>
    public byte Ext
    {
        get => Read(Ids.Ext);
        set => Write(Ids.Ext, value);
    }

    /// <summary>Oscillation control register. VMD-156</summary>
    public byte Ocr
    {
        get => Read(Ids.Ocr);
        set => Write(Ids.Ocr, value);
    }

    /// <summary>Timer 0 control register. VMD-67</summary>
    public byte T0Cnt
    {
        get => Read(Ids.T0Cnt);
        set => Write(Ids.T0Cnt, value);
    }

    // TODO: not sure if this pattern is scaling nicely. Maybe would be better to use bit consts e.g. T0Cnt = LIE | LOVF | HIE | ...

    /// <summary>
    /// T0LIE. Enables interrupt for T0L overflow.
    /// </summary>
    public bool T0Cnt_LowInterruptEnable
    {
        get => BitHelpers.ReadBit(T0Cnt, bit: 0);
        set => T0Cnt = BitHelpers.WithBit(T0Cnt, bit: 0, value);
    }

    /// <summary>
    /// T0LOVF. Set when T0L overflows.
    /// </summary>
    public bool T0Cnt_LowOverflowFlag
    {
        get => BitHelpers.ReadBit(T0Cnt, bit: 1);
        set => T0Cnt = BitHelpers.WithBit(T0Cnt, bit: 1, value);
    }

    /// <summary>
    /// T0HIE. Enables interrupt for T0H overflow.
    /// </summary>
    public bool T0Cnt_HighInterruptEnable
    {
        get => BitHelpers.ReadBit(T0Cnt, bit: 2);
        set => T0Cnt = BitHelpers.WithBit(T0Cnt, bit: 2, value);
    }

    /// <summary>
    /// T0HOVF. Set when T0H overflows.
    /// </summary>
    public bool T0Cnt_HighOverflowFlag
    {
        get => BitHelpers.ReadBit(T0Cnt, bit: 3);
        set => T0Cnt = BitHelpers.WithBit(T0Cnt, bit: 3, value);
    }

    /// <summary>
    /// T0LEXT. When set to 1, <see cref="T0L"/> is driven by an external signal determined by <see cref="Isl"/>.
    /// </summary>
    public bool T0Cnt_LowInputClockSelect
    {
        get => BitHelpers.ReadBit(T0Cnt, bit: 4);
        set => T0Cnt = BitHelpers.WithBit(T0Cnt, bit: 4, value);
    }

    /// <summary>
    /// T0LONG. When set to 1, 16-bit mode is used. Otherwise 8-bit mode is used.
    /// </summary>
    public bool T0Cnt_BitLengthSpecifier
    {
        get => BitHelpers.ReadBit(T0Cnt, bit: 5);
        set => T0Cnt = BitHelpers.WithBit(T0Cnt, bit: 5, value);
    }

    /// <summary>
    /// T0LRUN: When set to 1, starts the T0L counter. When reset to 0, reloads T0L with T0LR.
    /// </summary>
    public bool T0Cnt_LowRunFlag
    {
        get => BitHelpers.ReadBit(T0Cnt, bit: 6);
        set => T0Cnt = BitHelpers.WithBit(T0Cnt, bit: 6, value);
    }

    /// <summary>
    /// T0HRUN: When set to 1, starts the T0H counter. When reset to 0, reloads T0H with T0HR.
    /// </summary>
    public bool T0Cnt_HighRunFlag
    {
        get => BitHelpers.ReadBit(T0Cnt, bit: 7);
        set => T0Cnt = BitHelpers.WithBit(T0Cnt, bit: 7, value);
    }

    /// <summary>Timer 0 prescaler data. VMD-71</summary>
    public byte T0Prr
    {
        get => Read(Ids.T0Prr);
        set => Write(Ids.T0Prr, value);
    }

    /// <summary>Timer 0 low. VMD-71</summary>
    public byte T0L
    {
        get => Read(Ids.T0L);
        set => Write(Ids.T0L, value);
    }

    /// <summary>Timer 0 low reload data. VMD-71</summary>
    public byte T0Lr
    {
        get => Read(Ids.T0Lr);
        set => Write(Ids.T0Lr, value);
    }

    /// <summary>Timer 0 high. VMD-72</summary>
    public byte T0H
    {
        get => Read(Ids.T0H);
        set => Write(Ids.T0H, value);
    }

    /// <summary>Timer 0 high reload data. VMD-72</summary>
    public byte T0Hr
    {
        get => Read(Ids.T0Hr);
        set => Write(Ids.T0Hr, value);
    }

    /// <summary>Timer 1 control register. VMD-83</summary>
    public byte T1Cnt
    {
        get => Read(Ids.T1Cnt);
        set => Write(Ids.T1Cnt, value);
    }

    /// <summary>Timer 1 low comparison data. VMD-86</summary>
    public byte T1Lc
    {
        get => Read(Ids.T1Lc);
        set => Write(Ids.T1Lc, value);
    }

    /// <summary>Timer 1 low. VMD-85</summary>
    public byte T1L
    {
        get => Read(Ids.T1L);
        set => Write(Ids.T1L, value);
    }

    /// <summary>Timer 1 low reload data. VMD-85</summary>
    public byte T1Lr
    {
        get => Read(Ids.T1Lr);
        set => Write(Ids.T1Lr, value);
    }

    /// <summary>Timer 1 high comparison data. VMD-87</summary>
    public byte T1Hc
    {
        get => Read(Ids.T1Hc);
        set => Write(Ids.T1Hc, value);
    }

    /// <summary>Timer 1 high. VMD-86</summary>
    public byte T1H
    {
        get => Read(Ids.T1H);
        set => Write(Ids.T1H, value);
    }

    /// <summary>Timer 1 high reload data. VMD-86</summary>
    public byte T1Hr
    {
        get => Read(Ids.T1Hr);
        set => Write(Ids.T1Hr, value);
    }

    /// <summary>Mode control register. VMD-127</summary>
    public byte Mcr
    {
        get => Read(Ids.Mcr);
        set => Write(Ids.Mcr, value);
    }

    /// <summary>Start address register. VMD-129</summary>
    public byte Stad
    {
        get => Read(Ids.Stad);
        set => Write(Ids.Stad, value);
    }

    /// <summary>Character count register. VMD-130</summary>
    public byte Cnr
    {
        get => Read(Ids.Cnr);
        set => Write(Ids.Cnr, value);
    }

    /// <summary>Time division register. VMD-130</summary>
    public byte Tdr
    {
        get => Read(Ids.Tdr);
        set => Write(Ids.Tdr, value);
    }

    /// <summary>Bank address register. Bits 1-0 control whether XRAM bank 0, 1, or 2 is in use. VMD-125</summary>
    public byte Xbnk
    {
        get => Read(Ids.Xbnk);
        set => Write(Ids.Xbnk, value);
    }

    /// <summary>LCD contrast control register. VMD-131</summary>
    public byte Vccr
    {
        get => Read(Ids.Vccr);
        set => Write(Ids.Vccr, value);
    }

    /// <summary>SIO0 control register. VMD-108</summary>
    public byte Scon0
    {
        get => Read(Ids.Scon0);
        set => Write(Ids.Scon0, value);
    }

    /// <summary>SIO0 buffer. VMD-113</summary>
    public byte Sbuf0
    {
        get => Read(Ids.Sbuf0);
        set => Write(Ids.Sbuf0, value);
    }

    /// <summary>SIO0 baud rate generator. VMD-113</summary>
    public byte Sbr
    {
        get => Read(Ids.Sbr);
        set => Write(Ids.Sbr, value);
    }

    /// <summary>SIO1 control register. VMD-111</summary>
    public byte Scon1
    {
        get => Read(Ids.Scon1);
        set => Write(Ids.Scon1, value);
    }

    /// <summary>SIO1 buffer. VMD-113</summary>
    public byte Sbuf1
    {
        get => Read(Ids.Sbuf1);
        set => Write(Ids.Sbuf1, value);
    }

    /// <summary>Port 1 latch. VMD-58</summary>
    public byte P1
    {
        get => Read(Ids.P1);
        set => Write(Ids.P1, value);
    }

    /// <summary>Port 1 data direction register. VMD-58</summary>
    public byte P1Ddr
    {
        get => Read(Ids.P1Ddr);
        set => Write(Ids.P1Ddr, value);
    }

    /// <summary>Port 1 function control register. VMD-59</summary>
    public byte P1Fcr
    {
        get => Read(Ids.P1Fcr);
        set => Write(Ids.P1Fcr, value);
    }

    /// <summary>Port 3 latch. Buttons SLEEP, MODE, B, A, directions. VMD-54</summary>
    public byte P3
    {
        get => Read(Ids.P3);
        set => Write(Ids.P3, value);
    }

    // NB: application must set a button value to 1. When it is pressed, the bit is reset to 0.
    public bool ButtonSleep
    {
        get => BitHelpers.ReadBit(P3, bit: 7);
        set => P3 = BitHelpers.WithBit(P3, bit: 7, value);
    }

    public bool ButtonMode
    {
        get => BitHelpers.ReadBit(P3, bit: 6);
        set => P3 = BitHelpers.WithBit(P3, bit: 6, value);
    }

    public bool ButtonB
    {
        get => BitHelpers.ReadBit(P3, bit: 5);
        set => P3 = BitHelpers.WithBit(P3, bit: 5, value);
    }

    public bool ButtonA
    {
        get => BitHelpers.ReadBit(P3, bit: 4);
        set => P3 = BitHelpers.WithBit(P3, bit: 4, value);
    }

    public bool Right
    {
        get => BitHelpers.ReadBit(P3, bit: 3);
        set => P3 = BitHelpers.WithBit(P3, bit: 3, value);
    }

    public bool Left
    {
        get => BitHelpers.ReadBit(P3, bit: 2);
        set => P3 = BitHelpers.WithBit(P3, bit: 2, value);
    }

    public bool Down
    {
        get => BitHelpers.ReadBit(P3, bit: 1);
        set => P3 = BitHelpers.WithBit(P3, bit: 1, value);
    }

    public bool Up
    {
        get => BitHelpers.ReadBit(P3, bit: 0);
        set => P3 = BitHelpers.WithBit(P3, bit: 0, value);
    }

    /// <summary>Port 3 data direction register. VMD-62</summary>
    public byte P3Ddr
    {
        get => Read(Ids.P3Ddr);
        set => Write(Ids.P3Ddr, value);
    }

    /// <summary>Port 3 interrupt function control register. VMD-62</summary>
    public byte P3Int
    {
        get => Read(Ids.P3Int);
        set => Write(Ids.P3Int, value);
    }

    /// <summary>Flash Program Register. Undocumented.</summary>
    public byte FPR
    {
        get => Read(Ids.FPR);
        set => Write(Ids.FPR, value);
    }

    /// <summary>Flash Address Bank. Used as the upper bit of the address for flash access, i.e. whether flash bank 0 or 1 is used.</summary>
    public bool FPR0
    {
        get => BitHelpers.ReadBit(FPR, bit: 0);
        set => FPR = BitHelpers.WithBit(FPR, bit: 0, value);
    }

    /// <summary>Flash Write Unlock</summary>
    public bool FPR1
    {
        get => BitHelpers.ReadBit(FPR, bit: 1);
        set => FPR = BitHelpers.WithBit(FPR, bit: 1, value);
    }

    /// <summary>Port 7 latch. VMD-64</summary>
    public byte P7
    {
        get => Read(Ids.P7);
        set => Write(Ids.P7, value);
    }

    /// <summary>
    /// Dreamcast connection detection
    /// </summary>
    public bool P70
    {
        get => BitHelpers.ReadBit(P7, bit: 0);
        set => P7 = BitHelpers.WithBit(P7, bit: 0, value);
    }

    /// <summary>
    /// Low voltage detection
    /// </summary>
    public bool P71
    {
        get => BitHelpers.ReadBit(P7, bit: 1);
        set => P7 = BitHelpers.WithBit(P7, bit: 1, value);
    }

    /// <summary>External interrupt 0, 1 control. VMD-135</summary>
    public byte I01Cr
    {
        get => Read(Ids.I01Cr);
        set => Write(Ids.I01Cr, value);
    }

    /// <summary>
    /// INT0 enable flag.
    /// </summary>
    public bool I01Cr_Int0Enable
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 0);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 0, value);
    }

    /// <summary>
    /// INT0 source flag.
    /// </summary>
    public bool I01Cr_Int0Source
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 1);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 1, value);
    }

    /// <summary>
    /// INT0 detection level/edge select.
    /// I01CR3, I01CR2      INT0 interrupt condition
    /// 0,      0,          Detect falling edge
    /// 0,      1,          Detect low level
    /// 1,      0,          Detect rising edge
    /// 1,      1,          Detect high level
    /// </summary>
    public bool I01Cr_Int0LevelTriggered
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 2);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 2, value);
    }

    /// <inheritdoc cref="I01Cr_Int0LevelTriggered" />
    public bool I01Cr_Int0HighTriggered
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 3);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 3, value);
    }


    /// <summary>
    /// INT1 enable flag.
    /// </summary>
    public bool I01Cr_Int1Enable
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 4);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 4, value);
    }

    /// <summary>
    /// INT1 source flag.
    /// </summary>
    public bool I01Cr_Int1Source
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 5);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 5, value);
    }

    /// <summary>
    /// INT1 detection level/edge select.
    /// I01CR7, I01CR6      INT1 interrupt condition
    /// 0,      0,          Detect falling edge
    /// 0,      1,          Detect low level
    /// 1,      0,          Detect rising edge
    /// 1,      1,          Detect high level
    /// </summary>
    public bool I01Cr_Int1LevelTriggered
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 6);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 6, value);
    }

    /// <inheritdoc cref="I01Cr_Int1LevelTriggered"/>
    public bool I01Cr_Int1HighTriggered
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 7);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 7, value);
    }

    /// <summary>External interrupt 2, 3 control. VMD-137</summary>
    public byte I23Cr
    {
        get => Read(Ids.I23Cr);
        set => Write(Ids.I23Cr, value);
    }

    /// <summary>INT2/T0L enable flag.</summary>
    public bool I23Cr_Int2Enable
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 0);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 0, value);
    }

    /// <summary>INT2/T0L source flag.</summary>
    public bool I23Cr_Int2Source
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 1);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 1, value);
    }

    /// <summary>INT2 falling edge detection flag.</summary>
    public bool I23Cr_Int2FallingEdgeDetection
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 2);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 2, value);
    }

    /// <summary>INT2 rising edge detection flag.</summary>
    public bool I23Cr_Int2RisingEdgeDetection
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 3);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 3, value);
    }

    /// <summary>INT3/base timer enable flag.</summary>
    public bool I23Cr_Int3Enable
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 4);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 4, value);
    }

    /// <summary>INT3/base timer source flag.</summary>
    public bool I23Cr_Int3Source
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 5);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 5, value);
    }

    /// <summary>INT3 falling edge detection flag.</summary>
    public bool I23Cr_Int3FallingEdgeDetection
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 6);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 6, value);
    }

    /// <summary>INT3 rising edge detection flag.</summary>
    public bool I23Cr_Int3RisingEdgeDetection
    {
        get => BitHelpers.ReadBit(I01Cr, bit: 7);
        set => I01Cr = BitHelpers.WithBit(I01Cr, bit: 7, value);
    }

    /// <summary>Input signal select. VMD-138</summary>
    public byte Isl
    {
        get => Read(Ids.Isl);
        set => Write(Ids.Isl, value);
    }

#region Work RAM
    /// <summary>Control register. VMD-143</summary>
    /// TODO: the application is only supposed to be able to alter bit 4.
    public byte Vsel
    {
        get => Read(Ids.Vsel);
        set => Write(Ids.Vsel, value);
    }

    /// <summary>If set, increments Vramad (pair of Vramad1 and Vram</summary>
    public bool Vsel4_Ince
    {
        get => BitHelpers.ReadBit(Vsel, bit: 4);
        set => Vsel = BitHelpers.WithBit(Vsel, bit: 4, value);
    }

    /// <summary>Bits 0-7 of Vramad (work RAM address). VMD-144</summary>
    public byte Vrmad1
    {
        get => Read(Ids.Vrmad1);
        set => Write(Ids.Vrmad1, value);
    }

    /// <summary>Bit 8 of Vramad (work RAM address). VMD-144</summary>
    public byte Vrmad2
    {
        get => Read(Ids.Vrmad2);
        set => Write(Ids.Vrmad2, value);
    }

    /// <summary>Send/receive buffer. VMD-144</summary>
    public byte Vtrbf
    {
        get => Read(Ids.Vtrbf);
        set => Write(Ids.Vtrbf, value);
    }
#endregion
    /// <summary>Base timer control. VMD-101</summary>
    public byte Btcr
    {
        get => Read(Ids.Btcr);
        set => Write(Ids.Btcr, value);
    }
}