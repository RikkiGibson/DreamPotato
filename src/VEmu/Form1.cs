using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;

using VEmu.Core;

namespace VEmu;

public partial class Form1 : Form
{
    private readonly Cpu _cpu;
    private readonly Display _display;
    private long _ticks;
    const long targetFramerateTicks = TimeSpan.TicksPerSecond / 60; // 60 fps
    const int width = 48;
    const int height = 32;
    const int scale = 6;

    public Form1()
    {
        InitializeComponent();

        _cpu = new Cpu();
        _cpu.Reset();

        _display = new Display(_cpu);

        // HelloWorld.s_instructions.CopyTo(_cpu.FlashBank0.AsSpan());
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var memopad = File.ReadAllBytes(Path.Join(dir, "memopad.vms"));
        memopad.CopyTo(_cpu.FlashBank0);
        _cpu.CurrentROMBank = _cpu.FlashBank0;

        _ticks = Stopwatch.GetTimestamp();
    }

    private void Form1_Paint(object sender, PaintEventArgs e)
    {
    }

    private readonly byte[] _displayBits = new byte[Display.DisplaySize];
    private readonly Rectangle _srcRect = new Rectangle(0, 0, width, height);
    private readonly Rectangle _destRect = new Rectangle(0, 0, width*scale, height*scale);
    private readonly Bitmap _bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);

    private Color ReadColor(byte @byte, byte bit)
        => BitHelpers.ReadBit(@byte, bit) ? Color.Black : Color.White;

    private async void ScreenBox_Paint(object sender, PaintEventArgs e)
    {
        _cpu.Run(100);

        _display.Draw(_displayBits);

        int displayIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width;)
            {
                var b = _displayBits[displayIndex++];
                _bitmap.SetPixel(x++, y, ReadColor(b, 7));
                _bitmap.SetPixel(x++, y, ReadColor(b, 6));
                _bitmap.SetPixel(x++, y, ReadColor(b, 5));
                _bitmap.SetPixel(x++, y, ReadColor(b, 4));
                _bitmap.SetPixel(x++, y, ReadColor(b, 3));
                _bitmap.SetPixel(x++, y, ReadColor(b, 2));
                _bitmap.SetPixel(x++, y, ReadColor(b, 1));
                _bitmap.SetPixel(x++, y, ReadColor(b, 0));
            }
        }

        var graphics = e.Graphics;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        e.Graphics.DrawImage(_bitmap, destRect: _destRect, srcRect: _srcRect, GraphicsUnit.Pixel);

        var newTicks = Stopwatch.GetTimestamp();
        var elapsed = newTicks - _ticks;
        _ticks = newTicks;

        var nextFrameTime = TimeSpan.FromTicks(Math.Max(0, targetFramerateTicks - elapsed));
        // Console.WriteLine($"Elapsed {TimeSpan.FromTicks(elapsed)}. Next frame time {nextFrameTime}");

        await Task.Delay(nextFrameTime);
        Refresh();
    }
}
