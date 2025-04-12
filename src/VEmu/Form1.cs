using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace VEmu;

public partial class Form1 : Form
{
    Bitmap bitmap;

    public Form1()
    {
        InitializeComponent();

        const int width = 48;
        const int height = 32;
        const int stride = width * 4;
        var pixels = new int[height, width];

        //for (int x = 0; x < 5; x++)
        //    for (int y = 0; y < 5; y++)
        //        pixels[x, y] = unchecked((int)0xFF00FF00);


        var rand = new Random();
        for (int x = 0; x < 48; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                pixels[y, x] = BitConverter.ToInt32([(byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255), 255]);
            }
        }

        unsafe
        {
            fixed (int* p = pixels)
            {
                bitmap = new Bitmap(width, height, stride, PixelFormat.Format32bppRgb, (nint)p);
            }
        }

        //ScreenBox.Image = bitmap;
    }

    private void Form1_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
    }

    private void ScreenBox_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.DrawImage(bitmap, destRect: new Rectangle(0, 0, 144, 96), srcRect: new Rectangle(0, 0, 48, 32), GraphicsUnit.Pixel);
    }
}
