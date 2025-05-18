
using System.Diagnostics;

namespace VEmu.Core;

enum InterruptServicingState : byte
{
    /// <summary>
    /// Ready to service an interrupt.
    /// </summary>
    Ready,

    /// <summary>
    /// Returned from an interrupt in the previous instruction.
    /// Not ready to service again until another instruction is executed.
    /// </summary>
    ReturnedFromInterrupt,
}

/// <summary>
/// VMD-144.
/// Flags enumeration of all interrupts which can be requested.
/// Note that higher-priority interrupts have smaller values in this scheme.
/// </summary>
// TODO: DebuggerDisplay
enum Interrupts : ushort
{
    None = 0,

    /// <summary>
    /// External interrupt INT0 (Dreamcast connection status)
    /// </summary>
    INT0 = 1 << 0,

    /// <summary>
    /// External interrupt INT1 (Low voltage detection)
    /// </summary>
    INT1 = 1 << 1,

    /// <summary>
    /// External ID0 or internal Timer 0 Low overflow
    /// </summary>
    INT2_T0L = 1 << 2,

    /// <summary>
    /// External ID1 or internal Base timer overflow
    /// </summary>
    INT3_BT = 1 << 3,

    /// <summary>
    /// Internal interrupt Timer 0 High overflow
    /// </summary>
    T0H = 1 << 4,

    /// <summary>
    /// Internal interrupt timer 1 low or high overflow
    /// </summary>
    T1 = 1 << 5,

    /// <summary>
    /// Internal SIO0 end detect
    /// </summary>
    SIO0 = 1 << 6,

    /// <summary>
    /// Internal SIO1 end detect
    /// </summary>
    SIO1 = 1 << 7,

    /// <summary>
    /// VMU transfer receive end detect
    /// </summary>
    Maple = 1 << 8,

    /// <summary>
    /// Port 3 "L" level detect (player input)
    /// </summary>
    P3 = 1 << 9,
}

static class InterruptsExtensions
{
    public static bool IsHigherPriorityThan(this Interrupts @this, Interrupts other)
    {
        // TODO: this may need to factor in SFRs that control priority
        Debug.Assert(BitHelpers.IsPowerOfTwo((int)@this));
        Debug.Assert(BitHelpers.IsPowerOfTwo((int)other));
        return @this == Interrupts.None ? false :
            other == Interrupts.None ? true :
            @this < other;
    }
}

/// <summary>
/// Addresses of interrupt service routines.
/// </summary>
static class InterruptVectors
{
    /// <summary>
    /// <inheritdoc cref="Interrupts.INT0"/>
    /// </summary>
    public const ushort INT0 = 0x03;

    /// <summary>
    /// <inheritdoc cref="Interrupts.INT1"/>
    /// </summary>
    public const ushort INT1 = 0x0b;

    /// <summary>
    /// <inheritdoc cref="Interrupts.INT2_T0L"/>
    /// </summary>
    public const ushort INT2_T0L = 0x13;

    /// <summary>
    /// <inheritdoc cref="Interrupts.INT3_BT"/>
    /// </summary>
    public const ushort INT3_BT = 0x1B;

    /// <summary>
    /// <inheritdoc cref="Interrupts.T0H"/>
    /// </summary>
    public const ushort T0H = 0x23;

    /// <summary>
    /// <inheritdoc cref="Interrupts.T1"/>
    /// </summary>
    public const ushort T1 = 0x2B;

    /// <summary>
    /// <inheritdoc cref="Interrupts.SIO0"/>
    /// </summary>
    public const ushort SIO0 = 0x33;

    /// <summary>
    /// <inheritdoc cref="Interrupts.SIO1"/>
    /// </summary>
    public const ushort SIO1 = 0x3B;

    /// <summary>
    /// <inheritdoc cref="Interrupts.Maple"/>
    /// </summary>
    public const ushort Maple = 0x43;

    /// <summary>
    /// <inheritdoc cref="Interrupts.P3"/>
    /// </summary>
    public const ushort P3 = 0x4B;
}