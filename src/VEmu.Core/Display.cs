using System.Diagnostics;

namespace VEmu.Core;

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

    // TODO: make private, operate only on _bytes
    public void Draw(byte[] display)
    {
        // 48x32 1bpp
        Debug.Assert(display.Length == DisplaySize);
        // cpu.ToString();

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