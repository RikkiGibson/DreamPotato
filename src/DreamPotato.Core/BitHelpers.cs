using System.Diagnostics;
using System.Text;

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

    public static int AsBinary(this bool value) => value ? 1 : 0;

    /// <summary>Display bytes as hex rows of length 0x10 for debugging</summary>
    public static List<string> AsHexRows(this ReadOnlySpan<byte> bytes)
    {
        Debug.Assert((bytes.Length % 0x10) == 0);

        List<string> ret = [];
        var rows = bytes.Length / 0x10;

        // aligner
        {
            var builder = new StringBuilder();
            builder.Append("   | ");
            for (int addr = 0; addr < 0x10; addr++)
            {
                builder.Append($"{addr:X2} ");
            }
            ret.Add(builder.ToString());
        }

        for (int i = 0; i < rows; i++)
        {
            // start of row
            var builder = new StringBuilder();
            builder.Append($"{i:X2} | ");

            for (int addr = i * 0x10; addr < (i + 1) * 0x10; addr++)
            {
                builder.Append($"{bytes[addr]:X2} ");
            }
            ret.Add(builder.ToString());
        }
        return ret;
    }

    public static string AsHexBlock(this ReadOnlySpan<byte> bytes)
    {
        return string.Join('\n', AsHexRows(bytes));
    }

    public static int ModPositive(int x, int m)
    {
        Debug.Assert(m > 0);
        return (x % m + m) % m;
    }
}