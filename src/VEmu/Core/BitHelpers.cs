using System.Diagnostics;

static class BitHelpers
{
    public static bool ReadBit(byte operand, byte bit)
    {
        Debug.Assert(bit is >= 0 and < 8);
        return (operand & (1 << bit)) != 0;
    }

    public static void WriteBit(ref byte dest, byte bit, bool value)
    {
        Debug.Assert(bit is >= 0 and < 8);
        if (value)
            dest = (byte)(dest | 1 << bit);
        else
            dest = (byte)(dest & ~(1 << bit));
    }

    public static byte WithBit(byte operand, byte bit, bool value)
    {
        WriteBit(ref operand, bit, value);
        return operand;
    }
}