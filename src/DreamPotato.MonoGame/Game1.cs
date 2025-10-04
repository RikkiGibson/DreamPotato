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
    internal RecentFilesInfo RecentFilesInfo;

    internal Vmu Vmu;

    private const int SleepToggleInsertEjectFrameCount = 60; // 1 second

    // Set in Initialize()
    private SpriteBatch _spriteBatch = null!;
    private VmuPresenter _vmuSlot1Presenter = null!;

    private ButtonChecker _buttonChecker = null!;
    private UserInterface _userInterface = null!;

    // Set in LoadContent()
    private DynamicSoundEffectInstance _dynamicSound = null!;

    // Dynamic state
    private KeyboardState _previousKeys;
    private GamePadState _previousGamepad;
    internal bool Paused;
    internal int SleepHeldFrameCount;

    internal const int TotalScreenWidth = VmuPresenter.TotalScreenWidth;
    internal const int TotalScreenHeight = VmuPresenter.TotalScreenHeight * 2;

    public Game1(string? gameFilePath)
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = TotalScreenWidth;
        _graphics.PreferredBackBufferHeight = TotalScreenHeight; // TODO configurable

        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        Configuration = Configuration.Load();
        Configuration.Save();

        RecentFilesInfo = RecentFilesInfo.Load();

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

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        if (Vmu.HasUnsavedChanges && _userInterface.PendingCommand is not { Kind: PendingCommandKind.Exit, State: ConfirmationState.Confirmed })
        {
            args.Cancel = true;
            _userInterface.ShowConfirmCommandDialog(PendingCommandKind.Exit);
        }

        base.OnExiting(sender, args);
    }

    internal void UpdateWindowTitle()
    {
        var star = Vmu.HasUnsavedChanges
            ? "* "
            : "";

        var fileDesc = Vmu.LoadedFilePath is null
            ? ""
            : $"{Path.GetFileName(Vmu.LoadedFilePath)} - ";

        Window.Title = $"{star}{fileDesc}DreamPotato";
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

        vmsOrVmuFilePath ??= RecentFilesInfo.RecentFiles.FirstOrDefault();
        if (vmsOrVmuFilePath != null)
        {
            LoadAndStartVmsOrVmuFile(vmsOrVmuFilePath);
        }
    }

    internal void LoadNewVmu()
    {
        Vmu.LoadNewVmu(date: DateTime.Now, autoInitializeRTCDate: Configuration.AutoInitializeDate);
        Paused = false;
        UpdateWindowTitle();
        RecentFilesInfo = RecentFilesInfo.PrependRecentFile(newRecentFile: null);
        RecentFilesInfo.Save();
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
        UpdateWindowTitle();
        RecentFilesInfo = RecentFilesInfo.PrependRecentFile(filePath);
        RecentFilesInfo.Save();
    }

    internal void SaveVmuFileAs(string vmuFilePath)
    {
        var extension = Path.GetExtension(vmuFilePath);
        if (!extension.Equals(".vmu", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".bin", StringComparison.OrdinalIgnoreCase))
        {
            vmuFilePath = Path.ChangeExtension(vmuFilePath, ".vmu");
        }

        Vmu.SaveVmuAs(vmuFilePath);
        UpdateWindowTitle();
        RecentFilesInfo = RecentFilesInfo.PrependRecentFile(vmuFilePath);
        RecentFilesInfo.Save();
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
        var textures = new IconTextures
        {
            IconFileTexture = Content.Load<Texture2D>("VMUIconFile"),
            IconGameTexture = Content.Load<Texture2D>("VMUIconGame"),
            IconClockTexture = Content.Load<Texture2D>("VMUIconClock"),
            IconIOTexture = Content.Load<Texture2D>("VMUIconIO"),
            IconSleepTexture = Content.Load<Texture2D>("VMUIconSleep"),
            IconConnectedTexture = Content.Load<Texture2D>("DreamcastConnectedIcon"),
        };

        _userInterface = new UserInterface(this);
        _userInterface.Initialize(textures.IconConnectedTexture);
        _graphics.ApplyChanges();

        if (Debugger.IsAttached)
        {
            // create window out of the way
            Window.Position = new Point(x: 2200, y: 600);
        }

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        // TODO: color palette needs to be synced when it changes
        _vmuSlot1Presenter = new VmuPresenter(Vmu, textures, _graphics) { ColorPalette = _colorPalette };

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
        UpdateWindowTitle();
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

        _previousKeys = keyboard;
        _previousGamepad = gamepad;

        var rate = Paused ? 0 :
            IsFastForwarding ? gameTime.ElapsedGameTime.Ticks * 2 :
            gameTime.ElapsedGameTime.Ticks;

        Vmu._cpu.Run(rate);

        base.Update(gameTime);
    }

    internal bool IsFastForwarding
        => _buttonChecker.IsPressed(VmuButton.FastForward, _previousKeys, _previousGamepad);

    private void Audio_BufferReady(Audio.AudioBufferReadyEventArgs args)
    {
        _dynamicSound.SubmitBuffer(args.Buffer, args.Start, args.Length);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(_colorPalette.Margin);
        _vmuSlot1Presenter.Draw();
        _userInterface.Layout(gameTime);

        base.Draw(gameTime);
    }
}
