namespace VEmu.Core.SFRs;

/// <summary>Program status word. VMD-52</summary>
public struct Psw
{
    private byte _value;

    public Psw(byte value) => _value = value;
    public static explicit operator byte(Psw value) => value._value;

    /// <summary>
    /// Carry flag. VMD-45.
    /// </summary>
    /// <remarks>
    /// For arithmetic operations, carry can be thought of as "unsigned overflow".
    /// If an addition result exceeds 0xff, or a subtraction is less than 0, the carry flag is set.
    /// </remarks>
    public bool Cy
    {
        get => BitHelpers.ReadBit(_value, bit: 7);
        set => BitHelpers.WriteBit(ref _value, bit: 7, value);
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
        get => BitHelpers.ReadBit(_value, bit: 6);
        set => BitHelpers.WriteBit(ref _value, bit: 6, value);
    }

    /// <summary>Indirect address register bank flag 1. VMD-45</summary>
    public bool Irbk1
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => BitHelpers.WriteBit(ref _value, bit: 4, value);
    }

    /// <summary>Indirect address register bank flag 0. VMD-45</summary>
    public bool Irbk0
    {
        get => BitHelpers.ReadBit(_value, bit: 3);
        set => BitHelpers.WriteBit(ref _value, bit: 3, value);
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
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    /// <summary>When true, main memory access uses bank 1, otherwise it uses bank 0. RAM bank flag. VMD-45</summary>
    public bool Rambk0
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <summary>Accumulator (ACC) parity flag. VMD-45</summary>
    public bool P
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>Power control register. VMD-158</summary>
public struct Pcon
{
    private byte _value;

    public Pcon(byte value) => _value = value;
    public static explicit operator byte(Pcon value) => value._value;

    public bool HoldMode
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    public bool HaltMode
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>Master interrupt enable control register. VMD-138</summary>
public struct Ie
{
    private byte _value;

    public Ie(byte value) => _value = value;
    public static explicit operator byte(Ie value) => value._value;

    public bool MasterInterruptEnable
    {
        get => BitHelpers.ReadBit(_value, bit: 7);
        set => BitHelpers.WriteBit(ref _value, bit: 7, value);
    }

    /// <summary>
    /// Controls priority level of external interrupts. VMD-134.
    /// IE1, IE0    INT1 priority   INT0 priority
    /// 0,   0      Highest         Highest
    /// 1,   0      Low             Highest
    /// X,   1      Low             Low
    /// </summary>
    public bool PriorityControl1
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <inheritdoc cref="PriorityControl1">
    public bool PriorityControl0
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>Port 3 latch. Buttons SLEEP, MODE, B, A, directions. VMD-54</summary>
public struct P3
{
    private byte _value;

    public P3(byte value) => _value = value;
    public static explicit operator byte(P3 value) => value._value;

    // NB: application must set a button value to 1. When it is pressed, the bit is reset to 0.
    public bool ButtonSleep
    {
        get => BitHelpers.ReadBit(_value, bit: 7);
        set => BitHelpers.WriteBit(ref _value, bit: 7, value);
    }

    public bool ButtonMode
    {
        get => BitHelpers.ReadBit(_value, bit: 6);
        set => BitHelpers.WriteBit(ref _value, bit: 6, value);
    }

    public bool ButtonB
    {
        get => BitHelpers.ReadBit(_value, bit: 5);
        set => BitHelpers.WriteBit(ref _value, bit: 5, value);
    }

    public bool ButtonA
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => BitHelpers.WriteBit(ref _value, bit: 4, value);
    }

    public bool Right
    {
        get => BitHelpers.ReadBit(_value, bit: 3);
        set => BitHelpers.WriteBit(ref _value, bit: 3, value);
    }

    public bool Left
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    public bool Down
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    public bool Up
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>Port 7 latch. VMD-64</summary>
public struct P7
{
    private byte _value;

    public P7(byte value) => _value = value;
    public static explicit operator byte(P7 value) => value._value;

    /// <summary>External input pin 1</summary>
    public bool IP1
    {
        get => BitHelpers.ReadBit(_value, bit: 3);
        set => BitHelpers.WriteBit(ref _value, bit: 3, value);
    }

    /// <summary>External input pin 0</summary>
    public bool IP0
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    /// <summary>Low voltage detection</summary>
    public bool LowVoltage
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <summary>Dreamcast connection detection</summary>
    public bool DreamcastConnected
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>Port 3 interrupt function control register. VMD-62</summary>
public struct P3Int
{
    private byte _value;

    public P3Int(byte value) => _value = value;
    public static explicit operator byte(P3Int value) => value._value;

    /// <summary>P32INT. Port 3 Interrupt Control Flag.</summary>
    public bool Continuous
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    /// <summary>P31INT. Port 3 Interrupt Source Flag.</summary>
    public bool Source
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <summary>P30INT. Port 3 Interrupt Request Enable Control.</summary>
    public bool Enable
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>Flash Program Register. Undocumented.</summary>
public struct FPR
{
    private byte _value;

    public FPR(byte value) => _value = value;
    public static explicit operator byte(FPR value) => value._value;

    /// <summary>Flash Address Bank. Used as the upper bit of the address for flash access, i.e. whether flash bank 0 or 1 is used.</summary>
    public bool FPR0
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }

    /// <summary>Flash Write Unlock</summary>
    public bool FPR1
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }
}

/// <summary>External interrupt 0, 1 control. VMD-135</summary>
public struct I01Cr
{
    private byte _value;

    public I01Cr(byte value) => _value = value;
    public static explicit operator byte(I01Cr value) => value._value;

    /// <summary>
    /// INT1 detection level/edge select.
    /// I01CR7, I01CR6      INT1 interrupt condition
    /// 0,      0,          Detect falling edge
    /// 0,      1,          Detect low level
    /// 1,      0,          Detect rising edge
    /// 1,      1,          Detect high level
    /// </summary>
    public bool Int1HighTriggered
    {
        get => BitHelpers.ReadBit(_value, bit: 7);
        set => BitHelpers.WriteBit(ref _value, bit: 7, value);
    }

    /// <inheritdoc cref="Int1HighTriggered"/>
    public bool Int1LevelTriggered
    {
        get => BitHelpers.ReadBit(_value, bit: 6);
        set => BitHelpers.WriteBit(ref _value, bit: 6, value);
    }

    public bool Int1Source
    {
        get => BitHelpers.ReadBit(_value, bit: 5);
        set => BitHelpers.WriteBit(ref _value, bit: 5, value);
    }

    public bool Int1Enable
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => BitHelpers.WriteBit(ref _value, bit: 4, value);
    }

    /// <summary>
    /// INT0 detection level/edge select.
    /// I01CR3, I01CR2      INT0 interrupt condition
    /// 0,      0,          Detect falling edge
    /// 0,      1,          Detect low level
    /// 1,      0,          Detect rising edge
    /// 1,      1,          Detect high level
    /// </summary>
    public bool Int0HighTriggered
    {
        get => BitHelpers.ReadBit(_value, bit: 3);
        set => BitHelpers.WriteBit(ref _value, bit: 3, value);
    }

    /// <inheritdoc cref="Int0HighTriggered" />
    public bool Int0LevelTriggered
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    public bool Int0Source
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    public bool Int0Enable
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>External interrupt 2, 3 control. VMD-137</summary>
public struct I23Cr
{
    private byte _value;

    public I23Cr(byte value) => _value = value;
    public static explicit operator byte(I23Cr value) => value._value;

    /// <summary>INT2/T0L enable flag.</summary>
    public bool Int2Enable
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }

    /// <summary>INT2/T0L source flag.</summary>
    public bool Int2Source
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <summary>INT2 falling edge detection flag.</summary>
    public bool Int2FallingEdgeDetection
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    /// <summary>INT2 rising edge detection flag.</summary>
    public bool Int2RisingEdgeDetection
    {
        get => BitHelpers.ReadBit(_value, bit: 3);
        set => BitHelpers.WriteBit(ref _value, bit: 3, value);
    }

    /// <summary>INT3/base timer enable flag.</summary>
    public bool Int3Enable
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => BitHelpers.WriteBit(ref _value, bit: 4, value);
    }

    /// <summary>INT3/base timer source flag.</summary>
    public bool Int3Source
    {
        get => BitHelpers.ReadBit(_value, bit: 5);
        set => BitHelpers.WriteBit(ref _value, bit: 5, value);
    }

    /// <summary>INT3 falling edge detection flag.</summary>
    public bool Int3FallingEdgeDetection
    {
        get => BitHelpers.ReadBit(_value, bit: 6);
        set => BitHelpers.WriteBit(ref _value, bit: 6, value);
    }

    /// <summary>INT3 rising edge detection flag.</summary>
    public bool Int3RisingEdgeDetection
    {
        get => BitHelpers.ReadBit(_value, bit: 7);
        set => BitHelpers.WriteBit(ref _value, bit: 7, value);
    }
}

public enum Oscillator
{
    /// <summary>
    /// Internal (RC) oscillator: 600 kHz / 10.0us cycle time.
    /// Used when accessing flash memory in standalone mode.
    /// </summary>
    Rc,

    /// <summary>
    /// Ceramic (CF) oscillator: 6 MHz / 1.0us cycle time.
    /// Used when connected to console.
    /// </summary>
    Cf,

    /// <summary>
    /// Quartz (X'TAL) oscillator: 32 kHz / 183.0us cycle time.
    /// Used most of the time in standalone mode.
    /// </summary>
    Quartz,
}

/// <summary>
/// Oscillation control register. VMD-156.
/// </summary>
public struct Ocr
{
    private byte _value;

    public Ocr(byte value) => _value = value;
    public static explicit operator byte(Ocr value) => value._value;

    /// <summary>
    /// When set to 1, the cycle clock is 1/6 of the clock source.
    /// When reset to 0, the cycle clock is 1/12 of the clock source.
    /// The following combinations of settings are permitted:
    /// System clock        OCR7
    /// RC oscillator       0 or 1
    /// Quartz oscillator   1
    /// </summary>
    public bool ClockGeneratorControl
    {
        get => BitHelpers.ReadBit(_value, bit: 7);
        set => BitHelpers.WriteBit(ref _value, bit: 7, value);
    }

    public long SystemClockTicks
    {
        get
        {
            return SystemClockSelector switch
            {
                Oscillator.Cf => 1 * TimeSpan.TicksPerMicrosecond,
                Oscillator.Rc => 10 * TimeSpan.TicksPerMicrosecond,
                Oscillator.Quartz => 183 * TimeSpan.TicksPerMicrosecond,
                _ => throw new InvalidOperationException()
            };
        }
    }

    public Oscillator SystemClockSelector
    {
        get
        {
            return (BitHelpers.ReadBit(_value, bit: 5), BitHelpers.ReadBit(_value, bit: 4)) switch
            {
                (false, false) => Oscillator.Rc,
                (_, true) => Oscillator.Cf,
                (true, false) => Oscillator.Quartz
            };
        }
        set
        {
            var (bit5, bit4) = value switch
            {
                Oscillator.Rc => (false, false),
                Oscillator.Cf => (false, true),
                Oscillator.Quartz => (true, false),
                _ => throw new ArgumentException()
            };
            BitHelpers.WriteBit(ref _value, bit: 5, value: bit5);
            BitHelpers.WriteBit(ref _value, bit: 4, value: bit4);
        }
    }

    /// <summary>When set to 1, the RC oscillator is stopped. When reset to 0, the RC oscillator operates.</summary>
    public bool RCOscillatorControl
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <summary>
    /// When set to 1, the CF oscillator is stopped. When reset to 0, the CF oscillator operates.
    /// Note that use of the CF oscillator is only recommended when the VMU is docked in the controller due to high power consumption.
    /// </summary>
    public bool CFOscillatorControl
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>Timer 0 control register. VMD-67</summary>
public struct T0Cnt
{
    private byte _value;

    public T0Cnt(byte value) => _value = value;
    public static explicit operator byte(T0Cnt value) => value._value;

    /// <summary>
    /// When set to 1, starts the T0H counter. When reset to 0, reloads T0H with T0HR.
    /// </summary>
    public bool T0hRun
    {
        get => BitHelpers.ReadBit(_value, bit: 7);
        set => BitHelpers.WriteBit(ref _value, bit: 7, value);
    }

    /// <summary>
    /// When set to 1, starts the T0L counter. When reset to 0, reloads T0L with T0LR.
    /// </summary>
    public bool T0lRun
    {
        get => BitHelpers.ReadBit(_value, bit: 6);
        set => BitHelpers.WriteBit(ref _value, bit: 6, value);
    }

    /// <summary>
    /// When set to 1, 16-bit mode is used. Otherwise 8-bit mode is used.
    /// </summary>
    public bool T0Long
    {
        get => BitHelpers.ReadBit(_value, bit: 5);
        set => BitHelpers.WriteBit(ref _value, bit: 5, value);
    }

    /// <summary>
    /// When set to 1, <see cref="T0L"/> is driven by an external signal determined by <see cref="Isl"/>.
    /// </summary>
    public bool T0lExt
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => BitHelpers.WriteBit(ref _value, bit: 4, value);
    }

    /// <summary>
    /// Set when T0H overflows.
    /// </summary>
    public bool T0hOvf
    {
        get => BitHelpers.ReadBit(_value, bit: 3);
        set => BitHelpers.WriteBit(ref _value, bit: 3, value);
    }

    /// <summary>
    /// Enables interrupt for T0H overflow.
    /// </summary>
    public bool T0hIe
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    /// <summary>
    /// Set when T0L overflows.
    /// </summary>
    public bool T0lOvf
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <summary>
    /// Enables interrupt for T0L overflow.
    /// </summary>
    public bool T0lIe
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>Timer 1 control register. VMD-83</summary>
public struct T1Cnt
{
    private byte _value;

    public T1Cnt(byte value) => _value = value;
    public static explicit operator byte(T1Cnt value) => value._value;

    /// <summary>
    /// When set to 1, starts the T1H counter. When reset to 0, reloads T1H with T1HR.
    /// </summary>
    public bool T1hRun
    {
        get => BitHelpers.ReadBit(_value, bit: 7);
        set => BitHelpers.WriteBit(ref _value, bit: 7, value);
    }

    /// <summary>
    /// When set to 1, starts the T1L counter. When reset to 0, reloads T1L with T1LR.
    /// </summary>
    public bool T1lRun
    {
        get => BitHelpers.ReadBit(_value, bit: 6);
        set => BitHelpers.WriteBit(ref _value, bit: 6, value);
    }

    /// <summary>
    /// When set to 1, 16-bit mode is used. Otherwise 8-bit mode is used.
    /// </summary>
    public bool T1Long
    {
        get => BitHelpers.ReadBit(_value, bit: 5);
        set => BitHelpers.WriteBit(ref _value, bit: 5, value);
    }

    /// <summary>
    /// TODO: this does something with pulse generation.
    /// </summary>
    public bool ELDT1C
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => BitHelpers.WriteBit(ref _value, bit: 4, value);
    }

    /// <summary>
    /// Set when T1H overflows.
    /// </summary>
    public bool T1hOvf
    {
        get => BitHelpers.ReadBit(_value, bit: 3);
        set => BitHelpers.WriteBit(ref _value, bit: 3, value);
    }

    /// <summary>
    /// Enables interrupt for T1H overflow.
    /// </summary>
    public bool T1hIe
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    /// <summary>
    /// Set when T1L overflows.
    /// </summary>
    public bool T1lOvf
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <summary>
    /// Enables interrupt for T1L overflow.
    /// </summary>
    public bool T1lIe
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>Control register. VMD-143</summary>
public struct Vsel
{
    private byte _value;

    public Vsel(byte value) => _value = value;
    public static explicit operator byte(Vsel value) => value._value;

    /// <summary>If set, increments Vramad when <see cref="SpecialFunctionRegisters.Vtrbf"/> is accessed.</summary>
    public bool Ince
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => _value = BitHelpers.WithBit(_value, bit: 4, value);
    }
}