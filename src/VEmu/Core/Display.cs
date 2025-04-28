using System.Diagnostics;

namespace VEmu.Core;

public class Display(Cpu cpu)
{
    public const int DisplaySize = 48 * 32 / 8;
    public void Draw(byte[] display)
    {
        // 48x32 1bpp
        Debug.Assert(display.Length == DisplaySize);
        // cpu.ToString();

        var xram0 = cpu.Memory.Direct_ReadXram0();
        Debug.Assert(xram0.Length == 0x60);

        int index = 0;
        for (int left = 0; left < 0x60; left += 0x10)
        {
            for (int right = 0; right < 0xc; right++, index++)
                display[index] = xram0[left | right];
        }

        var xram1 = cpu.Memory.Direct_ReadXram1();
        Debug.Assert(xram1.Length == 0x60);
        // Debug.Assert(index == display.Length / 2);

        for (int left = 0; left < 0x60; left += 0x10)
        {
            for (int right = 0; right < 0xc; right++, index++)
                display[index] = xram1[left | right];
        }
    }
}