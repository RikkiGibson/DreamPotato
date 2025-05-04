namespace VEmu.Core.SFRs;

/// <summary>Timer 0 Control Register.</summary>
struct T0Cnt
{
    private byte _value;

    public T0Cnt(byte value) => _value = value;

    // TODO: it may be better to use .Value or some such instead of implicit conv to byte
    public static implicit operator byte(T0Cnt register) => register._value;
    public static implicit operator T0Cnt(byte value) => new T0Cnt(value);

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
