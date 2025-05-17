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
        _cpu.SFRs.Ie = new() { MasterInterruptEnable = true };

        _display = new Display(_cpu);

        // HelloWorld.s_instructions.CopyTo(_cpu.FlashBank0.AsSpan());
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var memopad = File.ReadAllBytes(Path.Join(dir, "memopad.vms"));
        memopad.CopyTo(_cpu.FlashBank0);
        _cpu.SetInstructionBank(Core.SFRs.InstructionBank.FlashBank0);;

        _ticks = Stopwatch.GetTimestamp();
    }

    private void Form1_Paint(object sender, PaintEventArgs e)
    {
    }

    private readonly byte[] _displayBits = new byte[Display.DisplaySize];
    private readonly Rectangle _srcRect = new Rectangle(0, 0, width, height);
    private readonly Rectangle _destRect = new Rectangle(0, 0, width * scale, height * scale);
    private readonly Bitmap _bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);

    private Color ReadColor(byte @byte, byte bit)
        => BitHelpers.ReadBit(@byte, bit) ? Color.Black : Color.White;

    private async void ScreenBox_Paint(object sender, PaintEventArgs e)
    {
        _cpu.Run(targetFramerateTicks);

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

    private void Form1_KeyDown(object sender, KeyEventArgs e)
    {
        // Console.WriteLine($"{e.KeyCode} Down");
        switch (e.KeyCode)
        {
            case Keys.W: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { Up = false }; break;
            case Keys.S: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { Down = false }; break;
            case Keys.A: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { Left = false }; break;
            case Keys.D: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { Right = false }; break;
            case Keys.K: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { ButtonA = false }; break;
            case Keys.L: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { ButtonB = false }; break;
            case Keys.J: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { ButtonMode = false }; break;
            case Keys.I: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { ButtonSleep = false }; break;
        }
    }

    private void Form1_KeyUp(object sender, KeyEventArgs e)
    {
        // Console.WriteLine($"{e.KeyCode} Up");
        switch (e.KeyCode)
        {
            case Keys.W: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { Up = true }; break;
            case Keys.S: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { Down = true }; break;
            case Keys.A: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { Left = true }; break;
            case Keys.D: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { Right = true }; break;
            case Keys.K: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { ButtonA = true }; break;
            case Keys.L: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { ButtonB = true }; break;
            case Keys.J: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { ButtonMode = true }; break;
            case Keys.I: _cpu.SFRs.P3 = _cpu.SFRs.P3 with { ButtonSleep = true }; break;
        }
    }
}
