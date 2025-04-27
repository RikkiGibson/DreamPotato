using System.Diagnostics;

namespace VEmu.Core;

public class Display(Cpu cpu)
{
    public const int DisplaySize = 48 * 32 / 8;
    public void Draw(byte[] display)
    {
        // 48x32 1bpp
        Debug.Assert(display.Length == DisplaySize);
        cpu.ToString();
        // TODO: read XRAM into 'display'
    }
}