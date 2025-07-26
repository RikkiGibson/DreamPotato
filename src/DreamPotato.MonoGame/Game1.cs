using System;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Myra.Graphics2D.UI;

using DreamPotato.Core;
using DreamPotato.MonoGame.UI;

namespace DreamPotato.MonoGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly Color[] _vmuScreenData;
    internal Configuration _configuration;

    internal readonly Vmu Vmu;
    private readonly Display _display;

    // TODO: eventually, there should be UI to permit a non-constant scale.
    private const int VmuScale = 6;
    private const int ScaledWidth = Display.ScreenWidth * VmuScale;
    private const int ScaledHeight = Display.ScreenHeight * VmuScale;

    private const int MenuBarHeight = 20;
    private const int TopMargin = VmuScale * 2 + MenuBarHeight;
    private const int SideMargin = VmuScale * 3;
    private const int BottomMargin = VmuScale * 12;

    internal const int TotalScreenWidth = ScaledWidth + SideMargin * 2;
    internal const int TotalScreenHeight = ScaledHeight + TopMargin + BottomMargin;

    private const int SleepToggleInsertEjectFrameCount = 60; // 1 second

    // Set in Initialize()
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _vmuScreenTexture = null!;
    private Texture2D _iconsTexture = null!;
    private ButtonChecker _buttonChecker = null!;
    private UserInterface _userInterface = null!;

    // Set in LoadContent()
    private SpriteFont _font1 = null!;
    private DynamicSoundEffectInstance _dynamicSound = null!;

    // Dynamic state
    private KeyboardState _previousKeys;
    private GamePadState _previousGamepad;
    internal bool Paused;
    internal int SleepHeldFrameCount;

    public Game1(string? gameFilePath)
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = TotalScreenWidth;
        _graphics.PreferredBackBufferHeight = TotalScreenHeight;

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        UpdateWindowTitle(gameFilePath);

        _configuration = Configuration.Load();
        _configuration.Save();

        Vmu = new Vmu();

        var date = DateTime.Now;
        Vmu.InitializeFlash(date);
        if (_configuration.AutoInitializeDate)
            Vmu.InitializeDate(date);

        Vmu.StartMapleServer();
        _display = new Display(Vmu._cpu);
        _vmuScreenData = new Color[Display.ScreenWidth * Display.ScreenHeight];

        LoadVmuFiles(gameFilePath, date: _configuration.AutoInitializeDate ? date : null);
    }

    internal void UpdateWindowTitle(string? gameFilePath)
    {
        Window.Title = gameFilePath is null
            ? "DreamPotato - (new VMU)"
            : $"DreamPotato - {Path.GetFileName(gameFilePath)}";
    }

    private void LoadVmuFiles(string? vmsOrVmuFilePath, DateTimeOffset? date)
    {
        const string romFileName = "american_v1.05.bin";
        var romFilePath = Path.Combine(Vmu.DataFolder, romFileName);
        try
        {
            var bios = File.ReadAllBytes(romFilePath);
            bios.AsSpan().CopyTo(Vmu._cpu.ROM);
        }
        catch (FileNotFoundException ex)
        {
            throw new InvalidOperationException($"'{romFileName}' must be included in '{Vmu.DataFolder}'.", ex);
        }

        if (vmsOrVmuFilePath != null)
        {
            LoadAndStartVmsOrVmuFile(vmsOrVmuFilePath);
        }
    }

    internal void LoadAndStartVmsOrVmuFile(string filePath)
    {
        Paused = false;
        UpdateWindowTitle(filePath);
        var extension = Path.GetExtension(filePath);
        if (extension.Equals(".vms", StringComparison.OrdinalIgnoreCase))
        {
            Vmu.LoadGameVms(filePath, DateTime.Now);
        }
        else if (extension.Equals(".vmu", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bin", StringComparison.OrdinalIgnoreCase))
        {
            Vmu.LoadVmu(filePath, DateTime.Now);
        }
        else
        {
            throw new ArgumentException($"Cannot load '{filePath}' because it is not a '.vms', '.vmu', or '.bin' file.");
        }
    }

    internal void SaveVmuFileAs(string vmuFilePath)
    {
        var extension = Path.GetExtension(vmuFilePath);
        if (!extension.Equals(".vmu", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".bin", StringComparison.OrdinalIgnoreCase))
        {
            vmuFilePath = Path.ChangeExtension(vmuFilePath, ".vmu");
        }

        UpdateWindowTitle(vmuFilePath);
        Vmu.SaveVmuAs(vmuFilePath);
    }

    protected override void Initialize()
    {
        _userInterface = new UserInterface(this);
        _userInterface.Initialize();
        _graphics.ApplyChanges();

        if (Debugger.IsAttached)
        {
            // create window out of the way
            Window.Position = new Point(x: 2200, y: 600);
        }

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _vmuScreenTexture = new Texture2D(_graphics.GraphicsDevice, Display.ScreenWidth, Display.ScreenHeight);

        // TODO: nice icons to resemble those on the real VMU.
        _iconsTexture = new Texture2D(_graphics.GraphicsDevice, ScaledWidth, BottomMargin);

        _buttonChecker = new ButtonChecker(_configuration);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _font1 = Content.Load<SpriteFont>("MyMenuFont");
        _dynamicSound = new DynamicSoundEffectInstance(Audio.SampleRate, AudioChannels.Mono);
        _dynamicSound.Play();
        Vmu.Audio.AudioBufferReady += Audio_BufferReady;
    }

    internal Point WindowSize
    {
        get
        {
            return Window.ClientBounds.Size;
        }
        set
        {
            _graphics.PreferredBackBufferWidth = value.X;
            _graphics.PreferredBackBufferHeight = value.Y;
            _graphics.ApplyChanges();
        }
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var gamepad = GamePad.GetState(PlayerIndex.One);

        if (keyboard.IsKeyDown(Keys.Escape))
            Exit();

        // Only respect a pause command if VMU is in the ejected state
        if (Vmu.IsEjected && _buttonChecker.IsNewlyPressed(VmuButton.Pause, _previousKeys, keyboard, _previousGamepad, gamepad))
            Paused = !Paused;

        // TODO: system for selecting save slots etc
        if (_buttonChecker.IsNewlyPressed(VmuButton.SaveState, _previousKeys, keyboard, _previousGamepad, gamepad))
            Vmu.SaveState(id: "0");

        if (_buttonChecker.IsNewlyPressed(VmuButton.LoadState, _previousKeys, keyboard, _previousGamepad, gamepad))
            Vmu.LoadStateById(id: "0");

        var newP3 = new Core.SFRs.P3()
        {
            Up = !_buttonChecker.IsPressed(VmuButton.Up, keyboard, gamepad),
            Down = !_buttonChecker.IsPressed(VmuButton.Down, keyboard, gamepad),
            Left = !_buttonChecker.IsPressed(VmuButton.Left, keyboard, gamepad),
            Right = !_buttonChecker.IsPressed(VmuButton.Right, keyboard, gamepad),
            ButtonA = !_buttonChecker.IsPressed(VmuButton.A, keyboard, gamepad),
            ButtonB = !_buttonChecker.IsPressed(VmuButton.B, keyboard, gamepad),
            ButtonSleep = !_buttonChecker.IsPressed(VmuButton.Sleep, keyboard, gamepad),
            ButtonMode = !_buttonChecker.IsPressed(VmuButton.Mode, keyboard, gamepad),
        };

        // Holding sleep can be used to toggle insert/eject
        if (!newP3.ButtonSleep)
        {
            if (SleepHeldFrameCount != -1)
            {
                // Sleep button held and frame counter not in post-toggle position
                SleepHeldFrameCount++;
            }
        }
        else
        {
            // Sleep button up. Reset sleep counter.
            SleepHeldFrameCount = 0;
        }

        if (SleepHeldFrameCount >= SleepToggleInsertEjectFrameCount
            || _buttonChecker.IsNewlyPressed(VmuButton.InsertEject, _previousKeys, keyboard, _previousGamepad, gamepad))
        {
            // Do not toggle insert/eject via sleep until sleep button is released and re-pressed
            SleepHeldFrameCount = -1;

            // force unpause when vmu is inserted, as we need to more directly/forcefully manage the vmu state/execution.
            Vmu.InsertOrEject();
            if (!Vmu.IsEjected)
                Paused = false;
        }

        // Let any button press wake the VMU from sleep
        if (_configuration.AnyButtonWakesFromSleep && !Vmu._cpu.SFRs.Vccr.DisplayControl && (byte)newP3 != 0xff)
        {
            newP3 = newP3 with { ButtonSleep = false };
        }

        Vmu._cpu.SFRs.P3 = newP3;

        var rate = Paused ? 0 :
            _buttonChecker.IsPressed(VmuButton.FastForward, keyboard, gamepad) ? gameTime.ElapsedGameTime.Ticks * 2 :
            gameTime.ElapsedGameTime.Ticks;

        Vmu._cpu.Run(rate);

        _previousKeys = keyboard;
        _previousGamepad = gamepad;

        base.Update(gameTime);
    }

    private void Audio_BufferReady(Audio.AudioBufferReadyEventArgs args)
    {
        _dynamicSound.SubmitBuffer(args.Buffer, args.Start, args.Length);
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

        _spriteBatch.Draw(
            _vmuScreenTexture,
            destinationRectangle: new Rectangle(new Point(x: SideMargin, y: TopMargin), new Point(x: ScaledWidth, y: ScaledHeight)),
            sourceRectangle: null,
            color: Color.White,
            rotation: 0,
            origin: default,
            effects: Vmu.IsEjected ? SpriteEffects.None : (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically),
            layerDepth: 0);

        // Draw icons
        var icons = _display.GetIcons();
        var fileIcon = (icons & Icons.File) != 0 ? "File " : "  ";
        var gameIcon = (icons & Icons.Game) != 0 ? "Game " : " ";
        var clockIcon = (icons & Icons.Clock) != 0 ? "Clock " : " ";
        var flashIcon = (icons & Icons.Flash) != 0 ? "Flash " : "  ";
        var sleeping = Vmu._cpu.SFRs.Vccr.DisplayControl ? " " : "(sleep) ";
        var paused = Paused ? "(paused) " : " ";
        var vmuEjected = Vmu.IsEjected ? "(standalone)" : "(plugged-in)";
        var connection = Vmu.IsServerConnected ? "(server connected)" : "(server disconnected)";
        var iconString = $"{fileIcon}{gameIcon}{clockIcon}{flashIcon}{sleeping}{paused}{vmuEjected}{connection}";
        _spriteBatch.DrawString(_font1, iconString, new Vector2(x: SideMargin, y: TopMargin + ScaledHeight), Color.Black);

        _spriteBatch.End();

        _userInterface.Layout(gameTime);

        base.Draw(gameTime);

        static Color ReadColor(byte b, byte bitAddress)
        {
            return BitHelpers.ReadBit(b, bitAddress) ? Color.Black : Color.White;
        }
    }
}
