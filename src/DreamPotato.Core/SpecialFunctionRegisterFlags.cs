// This file defines a bunch of structs to expose a structured view of certain SFRs.
// Note that we expect this kind of representation to add little/no overhead because
// the .NET runtime is good at treating structs with a single field as equivalent to the underlying value.

// The definitions here are taken from a combination of digging through VMU.pdf, emulator code, and ElysianVMU docs.

namespace DreamPotato.Core.SFRs;

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

    /// <summary>
    /// "Highest" priority if true, low priority if false.
    /// </summary>
    public bool Int1Priority
    {
        get
        {
            return (PriorityControl1, PriorityControl0) switch
            {
                (false, false) => true,
                _ => false
            };
        }
    }

    /// <summary>
    /// "Highest" priority if true, low priority if false.
    /// </summary>
    public bool Int0Priority
    {
        get
        {
            return (PriorityControl1, PriorityControl0) switch
            {
                (_, false) => true,
                _ => false
            };
        }
    }
}

/// <summary>Interrupt priority control register. Set bits indicate high priority; cleared bits indicate low priority. VMD-148</summary>
public struct Ip
{
    private byte _value;

    public Ip(byte value) => _value = value;
    public static explicit operator byte(Ip value) => value._value;

    public bool Port3
    {
        get => BitHelpers.ReadBit(_value, bit: 7);
        set => BitHelpers.WriteBit(ref _value, bit: 7, value);
    }

    public bool Maple
    {
        get => BitHelpers.ReadBit(_value, bit: 6);
        set => BitHelpers.WriteBit(ref _value, bit: 6, value);
    }

    public bool Sio1
    {
        get => BitHelpers.ReadBit(_value, bit: 5);
        set => BitHelpers.WriteBit(ref _value, bit: 5, value);
    }

    public bool Sio0
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => BitHelpers.WriteBit(ref _value, bit: 4, value);
    }

    public bool T1
    {
        get => BitHelpers.ReadBit(_value, bit: 3);
        set => BitHelpers.WriteBit(ref _value, bit: 3, value);
    }

    public bool T0H
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    public bool Int3_BaseTimer
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    public bool Int2_T0L
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

public enum InstructionBank : byte
{
    ROM = 0,
    FlashBank0 = 1,
    FlashBank1 = 2,
}

/// <summary>External memory control register. Undocumented.</summary>
public struct Ext
{
    private byte _value;

    public Ext(byte value) => _value = value;
    public static explicit operator byte(Ext value) => value._value;

    public InstructionBank InstructionBank
    {
        get
        {
            // Generally Ext3 is expected to be true, i.e. FlashBank1 is not used.
            // Applications will 'not1 EXT,0' then 'jmpf ...' to switch between FlashBank0 and ROM.
            return (Ext3, Ext0) switch
            {
                (_, true) => InstructionBank.FlashBank0,
                (true, false) => InstructionBank.ROM,
                (false, false) => InstructionBank.FlashBank1,
            };
        }
        set
        {
            (Ext3, Ext0) = value switch
            {
                InstructionBank.FlashBank0 => (true, true),
                InstructionBank.ROM => (true, false),
                InstructionBank.FlashBank1 => (false, false),
                _ => throw new ArgumentException(nameof(value))
            };
        }
    }

    public bool Ext3
    {
        get => BitHelpers.ReadBit(_value, bit: 3);
        set => BitHelpers.WriteBit(ref _value, bit: 3, value);
    }

    /// <summary>
    /// Controls whether to execute instructions from ROM or from flash bank 0.
    /// Applications expect to be able to switch back to ROM by flipping this bit.
    /// </summary>
    public bool Ext0
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>LCD contrast control register. VMD-131</summary>
public struct Vccr
{
    private byte _value;

    public Vccr(byte value) => _value = value;
    public static explicit operator byte(Vccr value) => value._value;

    /// <summary>When set to 1, power to the display is activated. When reset to 0, the display is shut off.</summary>
    public bool DisplayControl
    {
        get => BitHelpers.ReadBit(_value, bit: 7);
        set => BitHelpers.WriteBit(ref _value, bit: 7, value);
    }

    /// <summary>When set to 1, XRAM memory access is disabled (read and write). When reset to 0, XRAM memory access is enabled.</summary>
    // TODO: actually block user code from updating XRAM under these conditions, or at least log the problem.
    // TODO: log if XRAM is written while quartz oscillator is used for system clock. The real hardware requires the RC or CF oscillators be used instead.
    public bool XramAccessControl
    {
        get => BitHelpers.ReadBit(_value, bit: 6);
        set => BitHelpers.WriteBit(ref _value, bit: 6, value);
    }
}

/// <summary>SIO0 control register. Controls the sending side of serial transfer. VMD-103</summary>
public struct Scon0
{
    private byte _value;

    public Scon0(byte value) => _value = value;
    public static explicit operator byte(Scon0 value) => value._value;

    public bool PolarityControl
    {
        get => BitHelpers.ReadBit(_value, bit: 7);
        set => BitHelpers.WriteBit(ref _value, bit: 7, value);
    }

    public bool OverrunFlag
    {
        get => BitHelpers.ReadBit(_value, bit: 6);
        set => BitHelpers.WriteBit(ref _value, bit: 6, value);
    }

    /// <summary>
    /// Transfer bit length control.
    /// If true, continuous transfer is used; otherwise 8-bit transfer is used.
    /// </summary>
    public bool ContinuousTransfer
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => BitHelpers.WriteBit(ref _value, bit: 4, value);
    }

    /// <summary>
    /// 0: Stop transfer
    /// 1: Start transfer
    /// </summary>
    public bool TransferControl
    {
        get => BitHelpers.ReadBit(_value, bit: 3);
        set => BitHelpers.WriteBit(ref _value, bit: 3, value);
    }

    /// <summary>
    /// LSB/MSB sequence select.
    /// 0: LSB first
    /// 1: MSB first
    /// </summary>
    public bool MSBFirstSequence
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    /// <summary>
    /// 0: Transfer in progress
    /// 1: Transfer ended
    /// The transfer end flag becomes "1" after 8 bits (1 byte) have been transferred,
    /// regardless of the transfer bit length control setting.
    /// </summary>
    public bool TransferEndFlag
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <summary>
    /// Controls generation of <see cref="Interrupts.SIO0" />.
    /// </summary>
    public bool InterruptEnable
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>SIO1 control register. Controls the receiving side of serial transfer. VMD-103</summary>
public struct Scon1
{
    private byte _value;

    public Scon1(byte value) => _value = value;
    public static explicit operator byte(Scon1 value) => value._value;

    public bool OverrunFlag
    {
        get => BitHelpers.ReadBit(_value, bit: 6);
        set => BitHelpers.WriteBit(ref _value, bit: 6, value);
    }

    /// <summary>
    /// Transfer bit length control.
    /// If true, continuous transfer is used; otherwise 8-bit transfer is used.
    /// </summary>
    public bool ContinuousTransfer
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => BitHelpers.WriteBit(ref _value, bit: 4, value);
    }

    /// <summary>
    /// 0: Stop transfer
    /// 1: Start transfer
    /// </summary>
    public bool TransferControl
    {
        get => BitHelpers.ReadBit(_value, bit: 3);
        set => BitHelpers.WriteBit(ref _value, bit: 3, value);
    }

    /// <summary>
    /// LSB/MSB sequence select.
    /// 0: LSB first
    /// 1: MSB first
    /// </summary>
    public bool MSBFirstSequence
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    /// <summary>
    /// 0: Transfer in progress
    /// 1: Transfer ended
    /// </summary>
    public bool TransferEndFlag
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <summary>
    /// Controls generation of <see cref="Interrupts.SIO1" />.
    /// </summary>
    public bool InterruptEnable
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>Port 1 latch. PWM (sound), serial I/O. VMD-50</summary>
public struct P1
{
    private byte _value;

    public P1(byte value) => _value = value;
    public static explicit operator byte(P1 value) => value._value;

    public bool PulseOutput
    {
        get => BitHelpers.ReadBit(_value, bit: 7);
        set => BitHelpers.WriteBit(ref _value, bit: 7, value);
    }

    /// <summary>SIO1 clock pin</summary>
    public bool SCK1
    {
        get => BitHelpers.ReadBit(_value, bit: 5);
        set => BitHelpers.WriteBit(ref _value, bit: 5, value);
    }

    public bool SB1
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => BitHelpers.WriteBit(ref _value, bit: 4, value);
    }

    /// <summary>SIO1 output pin</summary>
    public bool SO1
    {
        get => BitHelpers.ReadBit(_value, bit: 3);
        set => BitHelpers.WriteBit(ref _value, bit: 3, value);
    }

    /// <summary>SIO0 clock pin</summary>
    public bool SCK0
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    public bool SB0
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <summary>SIO0 output pin</summary>
    public bool SO0
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>Port 1 function control register. VMD-59</summary>
/// <remarks>
/// To use the function assigned to port 1, the corresponding port latch must be reset to "0".
// For example, to use PWM, set P17FCR to "1" and reset P17 to "0".
// The instructions BPC, DBNZ, INC, DEC, SET1, CLR1, NOT1 read port latch data. Other instructions
// read data assigned to the port.
/// </remarks>
public struct P1Fcr
{
    private byte _value;

    public P1Fcr(byte value) => _value = value;
    public static explicit operator byte(P1Fcr value) => value._value;

    /// <summary>
    /// Controls the value of <see cref="P1.PulseOutput"/>.
    /// When true, the sum of PWM signal and port latch is output.
    /// When false, the port latch is output.
    /// </summary>
    public bool SelectPulseOutput
    {
        get => BitHelpers.ReadBit(_value, bit: 7);
        set => BitHelpers.WriteBit(ref _value, bit: 7, value);
    }

    /// <summary>
    /// Controls the clock assigned to P15 for serial transfer 1.
    /// When true, the sum of SCK1 and port latch data is output.
    /// When false, the port latch data is output.
    /// </summary>
    public bool SelectP15
    {
        get => BitHelpers.ReadBit(_value, bit: 5);
        set => BitHelpers.WriteBit(ref _value, bit: 5, value);
    }

    /// <summary>
    /// Controls the data assigned to P14 for serial transfer 1.
    /// When true, the sum of SB1 and port latch data is output.
    /// When false, the port latch data is output.
    /// </summary>
    public bool SelectP14
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => BitHelpers.WriteBit(ref _value, bit: 4, value);
    }

    /// <summary>
    /// Controls the data assigned to P13 for serial transfer 1.
    /// When true, the sum of S01 and port latch data is output.
    /// When false, the port latch data is output.
    /// </summary>
    public bool SelectP13
    {
        get => BitHelpers.ReadBit(_value, bit: 3);
        set => BitHelpers.WriteBit(ref _value, bit: 3, value);
    }

    /// <summary>
    /// Controls the clock assigned to P12 for serial transfer 0.
    /// When true, the sum of SCK0 and port latch data is output.
    /// When false, the port latch data is output.
    /// </summary>
    public bool SelectP12
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    /// <summary>
    /// Controls the data assigned to P11 for serial transfer 0.
    /// When true, the sum of SB0 and port latch data is output.
    /// When false, the port latch data is output.
    /// </summary>
    public bool SelectP11
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <summary>
    /// Controls the data assigned to P10 for serial transfer 1.
    /// When true, the sum of S00 and port latch data is output.
    /// When false, the port latch data is output.
    /// </summary>
    public bool SelectP10
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

    /// <summary>Low voltage detection. When user code raises this to 1, a low voltage reading lowers it to 0 and generates an interrupt.</summary>
    public bool LowVoltage
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <summary>Dreamcast connection detection. 1 is connected and 0 is disconnected.</summary>
    public bool DreamcastConnected
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }

    /// <summary>Is another VMU connected for serial IO?</summary>
    public bool VmuConnected
    {
        get => this is { DreamcastConnected: false, IP0: false, IP1: true };
        set => this = value
            ? this with { DreamcastConnected = false, IP0 = false, IP1 = true }
            : this with { IP1 = false };
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

    /// <summary>Used as the upper bit of the address for flash access, i.e. whether flash bank 0 or 1 is used.</summary>
    public bool FlashAddressBank
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }

    /// <summary>Flash Write Unlock. Used to indicate that the flash unlock sequence is being initiated.</summary>
    public bool FlashWriteUnlock
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

public enum BaseTimerClock
{
    T0Prescaler,
    CycleClock,
    QuartzOscillator,
}

/// <summary>Input signal select. VMD-138</summary>
public struct Isl
{
    private byte _value;

    public Isl(byte value) => _value = value;
    public static explicit operator byte(Isl value) => value._value;

    public BaseTimerClock BaseTimerClock
    {
        get
        {
            return (BaseTimerClockSelect5, BaseTimerClockSelect4) switch
            {
                (true, true) => BaseTimerClock.T0Prescaler,
                (false, true) => BaseTimerClock.CycleClock,
                (_, false) => BaseTimerClock.QuartzOscillator
            };
        }
        set
        {
            (BaseTimerClockSelect5, BaseTimerClockSelect4) = value switch
            {
                BaseTimerClock.T0Prescaler => (true, true),
                BaseTimerClock.CycleClock => (false, true),
                BaseTimerClock.QuartzOscillator => (false, false),
                _ => throw new ArgumentException(nameof(value))
            };
        }
    }

    /// <summary>
    /// VMD-133. Applications should reset ISL5, ISL4 to 0, 0 to ensure the quartz oscillator is used as the input clock.
    /// ISL5,   ISL4    Base timer clock
    /// 1       1       Timer/counter T0 prescaler
    /// 0       1       Cycle clock
    /// X       0       Quartz oscillator
    /// </summary>
    public bool BaseTimerClockSelect5
    {
        get => BitHelpers.ReadBit(_value, bit: 5);
        set => BitHelpers.WriteBit(ref _value, bit: 5, value);
    }

    /// <inheritdoc cref="BaseTimerClockSelect5">
    public bool BaseTimerClockSelect4
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => BitHelpers.WriteBit(ref _value, bit: 4, value);
    }

    /// <summary>
    /// Selects the time constant of the noise filter.
    /// ISL2,   ISL1    Time Constant
    /// 1       1       16 * Tcyc
    /// 0       1       64 * Tcyc
    /// X       0       1 * Tcyc
    /// </summary>
    public bool NoiseFilterTimeConstant2
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    /// <inheritdoc cref="NoiseFilterTimeConstant2"/>
    public bool NoiseFilterTimeConstant1
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <summary>
    /// When reset to 0, the clock source is P72/INT2/T0IN. When set to 1, the source is P73/INT3/T0IN.
    /// </summary>
    public bool T0ClockInputPin
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>Maple Status Word. Contains bits reflecting the status of a Maple transfer request.</summary>
public struct Mplsw
{
    private byte _value;

    public Mplsw(byte value) => _value = value;
    public static explicit operator byte(Mplsw value) => value._value;

    /// <summary>
    /// Set if packet is last.
    /// </summary>
    public bool LastPkt
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => BitHelpers.WriteBit(ref _value, bit: 4, value);
    }

    /// <summary>
    /// Indicates whether hardware is on.
    /// </summary>
    public bool HwEna
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    /// <summary>
    /// Indicates a normal packet is being transmitted.
    /// </summary>
    public bool Tx
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <summary>
    /// Indicates the starting packet is being transmitted.
    /// </summary>
    public bool Txs
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>Maple Start Transfer. Used to control starting and stopping a Maple transfer.</summary>
public struct Mplsta
{
    private byte _value;

    public Mplsta(byte value) => _value = value;
    public static explicit operator byte(Mplsta value) => value._value;

    public bool Unk
    {
        get => BitHelpers.ReadBit(_value, bit: 6);
        set => BitHelpers.WriteBit(ref _value, bit: 6, value);
    }

    /// <summary>
    /// Set if received packet was bad.
    /// </summary>
    public bool Err3
    {
        get => BitHelpers.ReadBit(_value, bit: 5);
        set => BitHelpers.WriteBit(ref _value, bit: 5, value);
    }

    /// <summary>
    /// Set if received packet was bad.
    /// </summary>
    public bool Err2
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => BitHelpers.WriteBit(ref _value, bit: 4, value);
    }

    /// <summary>
    /// Requests the Maple interrupt.
    /// </summary>
    public bool InterruptRequest
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => BitHelpers.WriteBit(ref _value, bit: 2, value);
    }

    /// <summary>
    /// Set if received packet was bad.
    /// </summary>
    public bool Err1
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => BitHelpers.WriteBit(ref _value, bit: 1, value);
    }

    /// <summary>
    /// Set when transmission is done.
    /// </summary>
    public bool TxDone
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => BitHelpers.WriteBit(ref _value, bit: 0, value);
    }
}

/// <summary>Maple Reset. Used to reset the Maple transaction when an error has occurred.</summary>
public struct Mplrst
{
    private byte _value;

    public Mplrst(byte value) => _value = value;
    public static explicit operator byte(Mplrst value) => value._value;

    /// <summary>
    /// Set, then clear, to reset the Maple bus. See American ROM@[1007].
    /// </summary>
    public bool Reset
    {
        get => BitHelpers.ReadBit(_value, bit: 7);
        set => BitHelpers.WriteBit(ref _value, bit: 7, value);
    }
}


public enum Oscillator
{
    /// <summary>
    /// Internal (RC) oscillator: 600 kHz / 10.0us cycle time.
    /// Used when accessing XRAM or flash memory in standalone mode.
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

// TODO: supposedly frequent integer division is faster when the divisor is constant.
// Since it is common to divide by the cpu hz, perhaps a specialized Oscillator.Divide method would be helpful.
// Would want to actually measure things or at least inspect the native code to make sure this would make a difference.
public static class OscillatorHz
{
    public const int Cf = 6_000_000;
    // From the data sheet:
    // Low end of frequency range: 600_000
    // High end of frequency range: 1_200_000
    // Suggested reference value: 879_236
    // The value we use here is based on empirical testing
    public const int Rc = 872_000;

    public const int Quartz = 32_768;
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

    /// <summary>
    /// Number of cycles the CPU executes in 1 second with the current settings.
    /// </summary>
    public int CpuClockHz
    {
        get
        {
            int divisor = ClockGeneratorControl ? 6 : 12;
            int oscillatorFrequency = SystemClockSelector switch
            {
#if DEBUG
                // CPU performance is poor at the real Cf speed when a debugger is attached.
                Oscillator.Cf => OscillatorHz.Rc,
#else
                Oscillator.Cf => OscillatorHz.Cf,
#endif
                Oscillator.Rc => OscillatorHz.Rc,
                Oscillator.Quartz => OscillatorHz.Quartz,
                _ => throw new InvalidOperationException()
            };

            return oscillatorFrequency / divisor;
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
    /// When enabled, a new compare value is sent to the pulse generator when T1Lc is set.
    /// A new compare value is also sent when T1L is stopped and restarted.
    /// User code usually stops T1L when changing the reload/compare audio parameters.
    /// So, in practice, it's very rare for this flag to meaningfully influence audio generation.
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

/// <summary>Work RAM control register. VMD-143</summary>
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

    /// <summary>Serial IO Selector. When set to 1, port 1 is used as a dedicated Maple interface. When reset to 0, it is used as a normal I/O port for serial communication.</summary>
    public bool Siosel
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => _value = BitHelpers.WithBit(_value, bit: 1, value);
    }

    /// <summary>
    /// Vtrbf Address Input Select.
    /// When set to 1, a transfer between Dreamcast and WRAM is currently being carried out, and the VMU cannot access WRAM.
    /// When reset to 0, there is no Maple transfer in progress, and WRAM can be accessed normally by the VMU.
    /// </summary>
    public bool Asel
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => _value = BitHelpers.WithBit(_value, bit: 0, value);
    }
}

/// <summary>Base timer control register. VMD-96</summary>
public struct Btcr
{
    private byte _value;

    public Btcr(byte value) => _value = value;
    public static explicit operator byte(Btcr value) => value._value;

    /// <summary>
    /// When set to 1: 64/fBST (0x40 hex). When reset to 0: 16384/fBST (0x4000 hex).
    /// fBST = base timer clock frequency.
    /// Since the quartz oscillator clocked at 32Khz is supposed to be used for base timer, setting 1 here ensures an interrupt is generated every 0.5 seconds.
    /// If this value is changed, the application needs to take care that the BIOS tick function is still called every 0.5 seconds.
    /// Note that interrupt 0 controls ticking the BIOS clock while interrupt 1 is free for other uses.
    /// </summary>
    public bool Int0CycleControl
    {
        get => BitHelpers.ReadBit(_value, bit: 7);
        set => _value = BitHelpers.WithBit(_value, bit: 7, value);
    }

    /// <summary><see cref="Int0CycleControl"/></summary>
    public int Int0CycleRate => Int0CycleControl ? 0x40 : 0x4000;

    /// <summary>
    /// When set to 1, count operation starts. When reset to 0, count operation stops, and the 14-bit counter is cleared.
    /// This should always be set to 1 to ensure correct BIOS clock operation.
    /// </summary>
    public bool CountEnable
    {
        get => BitHelpers.ReadBit(_value, bit: 6);
        set => _value = BitHelpers.WithBit(_value, bit: 6, value);
    }

    /// <summary>
    /// BTCR7   BTCR5   BTCR4   Base Timer Interrupt 1 Cycle
    /// X       0       0       32/fBST (0x20 hex)
    /// X       0       1       128/fBST (0x80 hex)
    /// 0       1       0       512/fBST (0x200 hex)
    /// 0       1       1       2048/fBST (0x800 hex)
    /// </summary>
    public int Int1CycleRate => (Int0CycleControl, Int1CycleControl5, Int1CycleControl4) switch
    {
        (_, false, false) => 0x20,
        (_, false, true) => 0x80,
        (false, true, false) => 0x200,
        (false, true, true) => 0x800,
        // The hardware manual doesn't define what the cycle rate is in this case.
        _ => throw new InvalidOperationException()
    };

    /// <summary>
    /// Affects Base Timer Interrupt 1 source generation.
    /// </summary>
    public bool Int1CycleControl5
    {
        get => BitHelpers.ReadBit(_value, bit: 5);
        set => _value = BitHelpers.WithBit(_value, bit: 5, value);
    }

    /// <summary>
    /// Affects Base Timer Interrupt 1 source generation.
    /// </summary>
    public bool Int1CycleControl4
    {
        get => BitHelpers.ReadBit(_value, bit: 4);
        set => _value = BitHelpers.WithBit(_value, bit: 4, value);
    }

    public bool Int1Source
    {
        get => BitHelpers.ReadBit(_value, bit: 3);
        set => _value = BitHelpers.WithBit(_value, bit: 3, value);
    }

    public bool Int1Enable
    {
        get => BitHelpers.ReadBit(_value, bit: 2);
        set => _value = BitHelpers.WithBit(_value, bit: 2, value);
    }

    public bool Int0Source
    {
        get => BitHelpers.ReadBit(_value, bit: 1);
        set => _value = BitHelpers.WithBit(_value, bit: 1, value);
    }

    /// <summary>
    /// Enables base timer interrupt 0 generation.
    /// This should always be set to ensure correct BIOS clock operation.
    /// </summary>
    public bool Int0Enable
    {
        get => BitHelpers.ReadBit(_value, bit: 0);
        set => _value = BitHelpers.WithBit(_value, bit: 0, value);
    }
}