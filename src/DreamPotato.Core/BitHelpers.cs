using System.Diagnostics;

namespace DreamPotato.Core;

public static class BitHelpers
{
    public static bool ReadBit(byte operand, int bit)
    {
        Debug.Assert(bit is >= 0 and < 8);
        return (operand & (1 << bit)) != 0;
    }

    public static void WriteBit(ref byte dest, int bit, bool value)
    {
        Debug.Assert(bit is >= 0 and < 8);
        if (value)
            dest = (byte)(dest | 1 << bit);
        else
            dest = (byte)(dest & ~(1 << bit));
    }

    public static byte WithBit(byte operand, int bit, bool value)
    {
        WriteBit(ref operand, bit, value);
        return operand;
    }

    // TODO: BitOperations.IsPow2?
    public static bool IsPowerOfTwo(int value)
    {
        return (value & (value - 1)) == 0;
    }
}