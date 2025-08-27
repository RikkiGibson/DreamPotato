using System.Diagnostics;
using System.Text;

namespace DreamPotato.Core;

[Flags]
public enum Icons : byte
{
    None = 0,
    File = 1 << 6,
    Game = 1 << 4,
    Clock = 1 << 2,
    Flash = 1 << 0,
}

/// <summary>
/// Provides VMU display data in 1-bit-per-pixel format.
/// </summary>
public class Display(Cpu cpu)
{
    public const int ScreenWidth = 48;
    public const int ScreenHeight = 32;
    public const int DisplaySize = ScreenWidth * ScreenHeight / 8;
    private readonly byte[] _bytes = new byte[DisplaySize];

    public ReadOnlySpan<byte> GetBytes()
    {
        Draw();
        return _bytes;
    }

    internal const int FileIconOffset = 1;
    internal const int GameIconOffset = 2;
    internal const int ClockIconOffset = 3;
    internal const int FlashIconOffset = 4;

    public Icons GetIcons()
    {
        // LCD is shut off
        if (!cpu.SFRs.Vccr.DisplayControl)
            return Icons.None;

        var xram2 = cpu.Memory.Direct_AccessXram2();
        var icons = ((Icons)xram2[FileIconOffset] & Icons.File)
            | ((Icons)xram2[GameIconOffset] & Icons.Game)
            | ((Icons)xram2[ClockIconOffset] & Icons.Clock)
            | ((Icons)xram2[FlashIconOffset] & Icons.Flash);
        return icons;
    }

    private void Draw()
    {
        if (!cpu.SFRs.Vccr.DisplayControl)
        {
            // LCD is shut off, so just draw a blank.
            Array.Clear(_bytes);
            return;
        }

        int index = 0;
        var xram0 = cpu.Memory.Direct_AccessXram0();
        for (int left = 0; left < Memory.XramBank01Size; left += 0x10)
        {
            // skip 4 dead display bytes
            for (int right = 0; right < 0xc; right++, index++)
                _bytes[index] = xram0[left | right];
        }

        var xram1 = cpu.Memory.Direct_AccessXram1();
        for (int left = 0; left < Memory.XramBank01Size; left += 0x10)
        {
            for (int right = 0; right < 0xc; right++, index++)
                _bytes[index] = xram1[left | right];
        }
    }

    /// <summary>Get a string representation of the display contents for testing.</summary>
    public string GetBlockString()
    {
        Draw();

        // Each character represents 2 bits, each from a row adjacent vertically.
        var builder = new StringBuilder(capacity: ScreenHeight / 2 * (ScreenWidth + Environment.NewLine.Length));
        const int BytesPerRow = ScreenWidth / 8;

        // Read corresponding bits from 2 rows
        for (int row = 0; row < ScreenHeight; row += 2)
        {
            for (int i = 0; i < BytesPerRow; i++)
            {
                var upper = _bytes[row * BytesPerRow + i];
                var lower = _bytes[(row + 1) * BytesPerRow + i];
                for (int bitAddress = 7; bitAddress >= 0; bitAddress--)
                {
                    var upperBit = BitHelpers.ReadBit(upper, bitAddress);
                    var lowerBit = BitHelpers.ReadBit(lower, bitAddress);
                    var ch = (upperBit, lowerBit) switch
                    {
                        (false, false) => ' ',
                        (false, true) => '▄',
                        (true, false) => '▀',
                        (true, true) => '█'
                    };
                    builder.Append(ch);
                }
            }
            builder.AppendLine();
        }

        return builder.ToString();
    }
}