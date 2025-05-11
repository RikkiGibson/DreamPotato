using System;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using VEmu.Core;

namespace VEmu.MonoGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly Color[] _vmuScreenData;
    private readonly Cpu _cpu;
    private readonly Display _display;

    private const int VmuScale = 6;
    private const int ScaledWidth = Display.ScreenWidth * VmuScale;
    private const int ScaledHeight = Display.ScreenHeight * VmuScale;

    // Set in Initialize()
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _vmuScreenTexture = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _cpu = new Cpu();
        _cpu.Reset();

        _display = new Display(_cpu);
        _vmuScreenData = new Color[Display.ScreenWidth * Display.ScreenHeight];
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = ScaledWidth;
        _graphics.PreferredBackBufferHeight = ScaledHeight;
        _graphics.ApplyChanges();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _vmuScreenTexture = new Texture2D(_graphics.GraphicsDevice, Display.ScreenWidth, Display.ScreenHeight);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        // TODO: UI for picking a vmu file
        var memopad = File.ReadAllBytes(@"C:\Users\rikki\src\VMU-MISC-CODE\memopad.vms");
        memopad.AsSpan().CopyTo(_cpu.FlashBank0);
        _cpu.CurrentROMBank = _cpu.FlashBank0;
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
            Exit();

        _cpu.SFRs.P3 = new()
        {
            Up =            !keyboard.IsKeyDown(Keys.W),
            Down =          !keyboard.IsKeyDown(Keys.S),
            Left =          !keyboard.IsKeyDown(Keys.A),
            Right =         !keyboard.IsKeyDown(Keys.D),
            ButtonA =       !keyboard.IsKeyDown(Keys.K),
            ButtonB =       !keyboard.IsKeyDown(Keys.L),
            ButtonSleep =   !keyboard.IsKeyDown(Keys.J),
            ButtonMode =    !keyboard.IsKeyDown(Keys.I),
        };

        _cpu.Run(gameTime.ElapsedGameTime.Ticks);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        var screenData = _display.GetBytes();
        int i = 0;
        foreach (byte b in screenData)
        {
            _vmuScreenData[i++] = ReadColor(b, 7);
            _vmuScreenData[i++] = ReadColor(b, 6);
            _vmuScreenData[i++] = ReadColor(b, 5);
            _vmuScreenData[i++] = ReadColor(b, 4);
            _vmuScreenData[i++] = ReadColor(b, 3);
            _vmuScreenData[i++] = ReadColor(b, 2);
            _vmuScreenData[i++] = ReadColor(b, 1);
            _vmuScreenData[i++] = ReadColor(b, 0);
        }
        _vmuScreenTexture.SetData(_vmuScreenData);

        // Use nearest neighbor scaling
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _spriteBatch.Draw(_vmuScreenTexture, new Rectangle(Point.Zero, new Point(x: ScaledWidth, y: ScaledHeight)), color: Color.White);
        _spriteBatch.End();

        // TODO: Add your drawing code here

        base.Draw(gameTime);

        static Color ReadColor(byte b, byte bitAddress)
        {
            return BitHelpers.ReadBit(b, bitAddress) ? Color.Black : Color.White;
        }
    }
}
