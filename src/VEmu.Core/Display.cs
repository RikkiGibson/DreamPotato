using System.Diagnostics;

namespace VEmu.Core;

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
        Draw(_bytes);
        return _bytes;
    }

    public Icons GetIcons()
    {
        // LCD is shut off
        if (!cpu.SFRs.Vccr.DisplayControl)
            return Icons.None;

        var xram2 = cpu.Memory.Direct_ReadXram2();
        var icons = ((Icons)xram2[1] & Icons.File)
            | ((Icons)xram2[2] & Icons.Game)
            | ((Icons)xram2[3] & Icons.Clock)
            | ((Icons)xram2[4] & Icons.Flash);
        return icons;
    }

    // TODO: make private, operate only on _bytes
    public void Draw(byte[] display)
    {
        // 48x32 1bpp
        Debug.Assert(display.Length == DisplaySize);

        if (!cpu.SFRs.Vccr.DisplayControl)
        {
            // LCD is shut off, so just draw a blank.
            // TODO: this is generally used to implement "sleep" mode.
            // when we start showing icons etc, it would be good to indicate that we are just sleeping, the game has not crashed.
            // Same, perhaps, with halt mode.
            Array.Clear(display);
            return;
        }

        var xram0 = cpu.Memory.Direct_ReadXram0();
        Debug.Assert(xram0.Length == 0x80);

        int index = 0;
        for (int left = 0; left < 0x80; left += 0x10)
        {
            // skip 4 dead display bytes
            for (int right = 0; right < 0xc; right++, index++)
                display[index] = xram0[left | right];
        }

        var xram1 = cpu.Memory.Direct_ReadXram1();
        Debug.Assert(xram1.Length == 0x80);

        for (int left = 0; left < 0x80; left += 0x10)
        {
            for (int right = 0; right < 0xc; right++, index++)
                display[index] = xram1[left | right];
        }
    }
}