using System;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using DreamPotato.Core;
using DreamPotato.MonoGame.UI;
using System.Linq;
using System.Collections.Immutable;

namespace DreamPotato.MonoGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly Color[] _vmuScreenData;
    private ColorPalette _colorPalette;
    internal Configuration Configuration;

    internal readonly Vmu Vmu;

    // TODO: eventually, there should be UI to permit a non-constant scale.
    private const int VmuScale = 6;
    private const int ScaledWidth = Display.ScreenWidth * VmuScale;
    private const int ScaledHeight = Display.ScreenHeight * VmuScale;

    private const int IconSize = 64;

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

    private Texture2D _iconFileTexture = null!;
    private Texture2D _iconGameTexture = null!;
    private Texture2D _iconClockTexture = null!;
    private Texture2D _iconIOTexture = null!;
    private Texture2D _iconSleepTexture = null!;
    private Texture2D _iconConnectedTexture = null!;

    private ButtonChecker _buttonChecker = null!;
    private UserInterface _userInterface = null!;

    // Set in LoadContent()
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

        Configuration = Configuration.Load();
        Configuration.Save();

        _colorPalette = ColorPalette.AllPalettes.FirstOrDefault(palette => palette.Name == Configuration.ColorPaletteName) ?? ColorPalette.AllPalettes[0];

        Vmu = new Vmu();
        Vmu.Audio.Volume = Configuration.Volume;

        var date = DateTime.Now;
        Vmu.InitializeFlash(date);
        if (Configuration.AutoInitializeDate)
            Vmu.InitializeDate(date);

        Vmu.RestartMapleServer(Configuration.DreamcastPort);
        _vmuScreenData = new Color[Display.ScreenWidth * Display.ScreenHeight];

        LoadVmuFiles(gameFilePath);
    }

    internal void UpdateWindowTitle(string? vmsOrVmuFilePath)
    {
        if (vmsOrVmuFilePath is null)
        {
            Window.Title = "DreamPotato";
            return;
        }

        // Indicate that vms files are not auto saved
        var prefix = Vmu.HasUnsavedChanges
            ? "* "
            : "";
        Window.Title = $"{prefix}{Path.GetFileName(vmsOrVmuFilePath)} - DreamPotato";
    }

    private void LoadVmuFiles(string? vmsOrVmuFilePath)
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

        Paused = false;
        UpdateWindowTitle(filePath);
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

    internal void Reset()
    {
        Vmu.Reset(Configuration.AutoInitializeDate ? DateTimeOffset.Now : null);
    }

    internal void Configuration_AutoInitializeDateChanged(bool newValue)
    {
        Configuration = Configuration with { AutoInitializeDate = newValue };
    }

    internal void Configuration_AnyButtonWakesFromSleepChanged(bool newValue)
    {
        Configuration = Configuration with { AnyButtonWakesFromSleep = newValue };
    }

    internal void Configuration_VolumeChanged(int newVolume)
    {
        Vmu.Audio.Volume = newVolume;
        Configuration = Configuration with { Volume = newVolume };
    }

    internal void Configuration_PaletteChanged(ColorPalette palette)
    {
        _colorPalette = palette;
        Configuration = Configuration with { ColorPaletteName = palette.Name };
    }

    internal void Configuration_DreamcastPortChanged(DreamcastPort dreamcastPort)
    {
        Configuration = Configuration with { DreamcastPort = dreamcastPort };
        Vmu.RestartMapleServer(dreamcastPort);
    }

    internal void Configuration_DoneEditing()
    {
        Configuration.Save();
    }

    internal void Configuration_DoneEditingKeyMappings(ImmutableArray<KeyMapping> keyMappings)
    {
        Configuration = Configuration with { KeyMappings = keyMappings };
        _buttonChecker = new ButtonChecker(Configuration);
        Configuration.Save();
    }

    internal void Configuration_DoneEditingButtonMappings(ImmutableArray<ButtonMapping> buttonMappings)
    {
        Configuration = Configuration with { ButtonMappings = buttonMappings };
        _buttonChecker = new ButtonChecker(Configuration);
        Configuration.Save();
    }

    protected override void Initialize()
    {
        _iconFileTexture = Content.Load<Texture2D>("VMUIconFile");
        _iconGameTexture = Content.Load<Texture2D>("VMUIconGame");
        _iconClockTexture = Content.Load<Texture2D>("VMUIconClock");
        _iconIOTexture = Content.Load<Texture2D>("VMUIconIO");
        _iconSleepTexture = Content.Load<Texture2D>("VMUIconSleep");
        _iconConnectedTexture = Content.Load<Texture2D>("DreamcastConnectedIcon");

        _userInterface = new UserInterface(this);
        _userInterface.Initialize(_iconConnectedTexture);
        _graphics.ApplyChanges();

        if (Debugger.IsAttached)
        {
            // create window out of the way
            Window.Position = new Point(x: 2200, y: 600);
        }

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _vmuScreenTexture = new Texture2D(_graphics.GraphicsDevice, Display.ScreenWidth, Display.ScreenHeight);

        _buttonChecker = new ButtonChecker(Configuration);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _dynamicSound = new DynamicSoundEffectInstance(Audio.SampleRate, AudioChannels.Mono);
        _dynamicSound.Play();
        Vmu.Audio.AudioBufferReady += Audio_BufferReady;
        Vmu.UnsavedChangesDetected += Vmu_UnsavedChangesDetected;
    }

    private void Vmu_UnsavedChangesDetected()
    {
        UpdateWindowTitle(Vmu.LoadedFilePath);
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


        // Only respect a pause command if VMU is in the ejected state
        if (Vmu.IsEjected && _buttonChecker.IsNewlyPressed(VmuButton.Pause, _previousKeys, keyboard, _previousGamepad, gamepad))
            Paused = !Paused;

        // TODO: system for selecting save slots etc
        if (_buttonChecker.IsNewlyPressed(VmuButton.SaveState, _previousKeys, keyboard, _previousGamepad, gamepad))
            Vmu.SaveState(id: "0");

        if (_buttonChecker.IsNewlyPressed(VmuButton.LoadState, _previousKeys, keyboard, _previousGamepad, gamepad))
        {
            if (Vmu.LoadStateById(id: "0", saveOopsFile: true) is (false, var error))
            {
                _userInterface.ShowToast(error ?? $"An unknown error occurred in {nameof(Vmu.LoadStateById)}.");
            }
        }

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
        if (Configuration.AnyButtonWakesFromSleep && !Vmu._cpu.SFRs.Vccr.DisplayControl && (byte)newP3 != 0xff)
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
        GraphicsDevice.Clear(_colorPalette.Margin);

        var screenData = Vmu.Display.GetBytes();
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

        var vmuIsEjected = Vmu.IsEjected;
        var screenSize = new Point(x: ScaledWidth, y: ScaledHeight);
        var screenRectangle = vmuIsEjected
            ? new Rectangle(new Point(x: SideMargin, y: TopMargin), screenSize)
            : new Rectangle(location: new Point(x: SideMargin, y: TopMargin + IconSize), screenSize);

        _spriteBatch.Draw(
            _vmuScreenTexture,
            destinationRectangle: screenRectangle,
            sourceRectangle: null,
            color: Color.White,
            rotation: 0,
            origin: default,
            effects: vmuIsEjected ? SpriteEffects.None : (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically),
            layerDepth: 0);

        // Draw icons
        int iconsYPos = vmuIsEjected ? TopMargin + ScaledHeight + VmuScale / 2 : TopMargin - VmuScale / 2;
        const int iconSpacing = VmuScale * 2;
        var icons = Vmu.Display.GetIcons();

        var displayOn = Vmu._cpu.SFRs.Vccr.DisplayControl;
        if (displayOn)
        {
            drawIcon(_iconFileTexture, ordinal: 0, enabled: (icons & Icons.File) != 0);
            drawIcon(_iconGameTexture, ordinal: 1, enabled: (icons & Icons.Game) != 0);
            drawIcon(_iconClockTexture, ordinal: 2, enabled: (icons & Icons.Clock) != 0);
            drawIcon(_iconIOTexture, ordinal: 3, enabled: (icons & Icons.Flash) != 0);
        }
        else
        {
            drawIcon(_iconSleepTexture, ordinal: 3, enabled: true);
        }

        void drawIcon(Texture2D texture, int ordinal, bool enabled)
        {
            const int maxPosition = 3;
            Debug.Assert(ordinal is >= 0 and <= maxPosition);

            var iconColor = enabled ? _colorPalette.Icon1 : _colorPalette.Icon0;
            var iconSize = new Point(IconSize);
            var iconRectangle = vmuIsEjected
                ? new Rectangle(location: new Point(x: SideMargin + iconSpacing * ordinal + IconSize * ordinal, y: iconsYPos), iconSize)
                : new Rectangle(location: new Point(x: SideMargin + iconSpacing * (maxPosition - ordinal) + IconSize * (maxPosition - ordinal), y: iconsYPos), iconSize);

            _spriteBatch.Draw(
                texture,
                destinationRectangle: iconRectangle,
                sourceRectangle: null,
                color: iconColor,
                rotation: 0,
                origin: default,
                effects: vmuIsEjected ? SpriteEffects.None : (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically),
                layerDepth: 0);
        }

        _spriteBatch.End();

        _userInterface.Layout(gameTime);

        base.Draw(gameTime);

        Color ReadColor(byte b, byte bitAddress)
        {
            return BitHelpers.ReadBit(b, bitAddress) ? _colorPalette.Screen1 : _colorPalette.Screen0;
        }
    }
}
