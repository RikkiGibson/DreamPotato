namespace VEmu.Core.SFRs;

/// <summary>External interrupt 0, 1 control. VMD-135</summary>
struct I01Cr
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

/// <summary>Timer 0 control register. VMD-67</summary>
struct T0Cnt
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
