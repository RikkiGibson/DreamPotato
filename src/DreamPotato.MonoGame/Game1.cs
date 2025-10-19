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
    private ColorPalette _colorPalette;
    internal Configuration Configuration;
    internal RecentFilesInfo RecentFilesInfo;

    internal Vmu PrimaryVmu { get; }

    /// <summary>Used only for the slot 2 VMU and only when configuration says to use both slot 1 and 2.</summary>
    internal Vmu? SecondaryVmu { get; private set; }

    internal const int MenuBarHeight = 20;

    private const int SleepToggleInsertEjectFrameCount = 60; // 1 second

    // Set in Initialize()
    private SpriteBatch _spriteBatch = null!;
    private VmuPresenter _primaryVmuPresenter = null!;
    private VmuPresenter _secondaryVmuPresenter = null!;

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
        Configuration = Configuration.Load();
        Configuration.Save();

        _graphics = new GraphicsDeviceManager(this);
        var windowSize = Configuration.ViewportSize;
        _graphics.PreferredBackBufferWidth = windowSize.Width;
        _graphics.PreferredBackBufferHeight = windowSize.Height;

        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += Window_ClientSizeChanged;

        Content.RootDirectory = "Content";
        IsMouseVisible = true;


        RecentFilesInfo = RecentFilesInfo.Load();

        _colorPalette = ColorPalette.AllPalettes.FirstOrDefault(palette => palette.Name == Configuration.ColorPaletteName) ?? ColorPalette.AllPalettes[0];

        var date = DateTime.Now;

        PrimaryVmu = new Vmu();
        initializeVmu(PrimaryVmu);
        PrimaryVmu.DockOrEject(connect: Configuration.VmuConnectionState is VmuConnectionState.Slot1Docked or VmuConnectionState.Slot1And2Docked);
        LoadVmuFiles(PrimaryVmu, gameFilePath, RecentFilesInfo);

        SecondaryVmu = new Vmu();
        initializeVmu(SecondaryVmu);
        SecondaryVmu.DockOrEject(connect: Configuration.VmuConnectionState is VmuConnectionState.Slot2Docked or VmuConnectionState.Slot1And2Docked);

        void initializeVmu(Vmu vmu)
        {
            vmu.Audio.Volume = Configuration.Volume;
            vmu.InitializeFlash(date);
            if (Configuration.AutoInitializeDate)
                vmu.InitializeDate(date);

            vmu.RestartMapleServer(Configuration.DreamcastPort);
        }
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        if (PrimaryVmu.HasUnsavedChanges && _userInterface.PendingCommand is not { Kind: PendingCommandKind.Exit, State: ConfirmationState.Confirmed })
        {
            args.Cancel = true;
            _userInterface.ShowConfirmCommandDialog(PendingCommandKind.Exit);
        }

        // Save window size and position on exit
        var viewport = _graphics.GraphicsDevice.Viewport;
        var position = Window.Position;
        Configuration = Configuration with
        {
            ViewportSize = new ViewportSize(viewport.Width, viewport.Height),
            WindowPosition = new WindowPosition(position.X, position.Y),
            VmuConnectionState = PrimaryVmu.IsDocked ? VmuConnectionState.Slot1Docked : VmuConnectionState.None,
        };
        Configuration.Save();

        base.OnExiting(sender, args);
    }

    internal void UpdateWindowTitle()
    {
        var star = PrimaryVmu.HasUnsavedChanges
            ? "* "
            : "";

        var fileDesc = PrimaryVmu.LoadedFilePath is null
            ? ""
            : $"{Path.GetFileName(PrimaryVmu.LoadedFilePath)} - ";

        Window.Title = $"{star}{fileDesc}DreamPotato";
    }

    private void LoadVmuFiles(Vmu vmu, string? vmsOrVmuFilePath, RecentFilesInfo? recentFilesInfo)
    {
        vmu.LoadRom();
        vmsOrVmuFilePath ??= RecentFilesInfo.RecentFiles.FirstOrDefault();
        if (vmsOrVmuFilePath != null)
        {
            LoadAndStartVmsOrVmuFile(vmu, vmsOrVmuFilePath);
        }
    }

    internal void LoadNewVmu()
    {
        PrimaryVmu.LoadNewVmu(date: DateTime.Now, autoInitializeRTCDate: Configuration.AutoInitializeDate);
        Paused = false;
        UpdateWindowTitle();
        RecentFilesInfo = RecentFilesInfo.PrependRecentFile(newRecentFile: null);
        RecentFilesInfo.Save();
    }

    internal void LoadAndStartVmsOrVmuFile(Vmu vmu, string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (extension.Equals(".vms", StringComparison.OrdinalIgnoreCase))
        {
            vmu.LoadGameVms(filePath, DateTime.Now);
        }
        else if (extension.Equals(".vmu", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bin", StringComparison.OrdinalIgnoreCase))
        {
            vmu.LoadVmu(filePath, DateTime.Now);
        }
        else
        {
            throw new ArgumentException($"Cannot load '{filePath}' because it is not a '.vms', '.vmu', or '.bin' file.");
        }

        Paused = false;
        // TODO(spi): what to do with these for secondary VMU?
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

        PrimaryVmu.SaveVmuAs(vmuFilePath);
        UpdateWindowTitle();
        RecentFilesInfo = RecentFilesInfo.PrependRecentFile(vmuFilePath);
        RecentFilesInfo.Save();
    }

    internal void Reset()
    {
        PrimaryVmu.Reset(Configuration.AutoInitializeDate ? DateTimeOffset.Now : null);
    }

    internal void Configuration_AutoInitializeDateChanged(bool newValue)
    {
        Configuration = Configuration with { AutoInitializeDate = newValue };
    }

    internal void Configuration_AnyButtonWakesFromSleepChanged(bool newValue)
    {
        Configuration = Configuration with { AnyButtonWakesFromSleep = newValue };
    }

    internal void Configuration_PreserveAspectRatioChanged(bool newValue)
    {
        Configuration = Configuration with { PreserveAspectRatio = newValue };
        UpdateScaleMatrix();
    }

    internal void Configuration_VolumeChanged(int newVolume)
    {
        PrimaryVmu.Audio.Volume = newVolume;
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
        PrimaryVmu.RestartMapleServer(dreamcastPort);
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
        _primaryVmuPresenter = new VmuPresenter(PrimaryVmu, textures, _graphics) { ColorPalette = _colorPalette };

        // TODO(spi): configuration for when to actually have a secondary vmu
        _secondaryVmuPresenter = new VmuPresenter(SecondaryVmu!, textures, _graphics) { ColorPalette = _colorPalette };
        UpdateScaleMatrix();

        if (Configuration.WindowPosition is { } windowPosition)
        {
            var windowSize = Window.ClientBounds.Size;
            if (_graphics.GraphicsDevice.DisplayMode.TitleSafeArea.Intersects(new Rectangle(windowPosition.X, windowPosition.Y, windowSize.X, windowSize.Y)))
                Window.Position = new Point(windowPosition.X, windowPosition.Y);
        }

        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _buttonChecker = new ButtonChecker(Configuration);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _dynamicSound = new DynamicSoundEffectInstance(Audio.SampleRate, AudioChannels.Mono);
        _dynamicSound.Play();
        PrimaryVmu.Audio.AudioBufferReady += Audio_BufferReady;
        PrimaryVmu.UnsavedChangesDetected += Vmu_UnsavedChangesDetected;
    }

    private void Window_ClientSizeChanged(object? sender, EventArgs e)
    {
        const int MinHeight = VmuPresenter.TotalContentHeight + MenuBarHeight;

        var viewport = _graphics.GraphicsDevice.Viewport;
        _graphics.PreferredBackBufferWidth = Math.Max(viewport.Width, VmuPresenter.TotalContentWidth);
        _graphics.PreferredBackBufferHeight = Math.Max(viewport.Height, MinHeight);
        _graphics.ApplyChanges();
        UpdateScaleMatrix();
    }

    private void UpdateScaleMatrix()
    {
        var viewport = _graphics.GraphicsDevice.Viewport;
        var contentRectangle = viewport.Bounds;
        contentRectangle.Height -= MenuBarHeight;
        contentRectangle.Y += MenuBarHeight;

        // Prefer a horizontal layout for multiple VMUs, when the viewport is proportionally wider than the VMU content dimensions.
        // Otherwise, prefer a vertical layout.
        var layoutHorizontal = (float)contentRectangle.Height / VmuPresenter.TotalContentHeight
            < (float)contentRectangle.Width / VmuPresenter.TotalContentWidth;

        var slot1Rectangle = layoutHorizontal
            ? contentRectangle with { Width = contentRectangle.Width / 2 }
            : contentRectangle with { Height = contentRectangle.Height / 2 };

        var slot2Rectangle = layoutHorizontal
            ? contentRectangle with { Width = contentRectangle.Width / 2, X = slot1Rectangle.X + slot1Rectangle.Width }
            : contentRectangle with { Height = contentRectangle.Height / 2, Y = slot1Rectangle.Y + slot1Rectangle.Height };

        _primaryVmuPresenter.UpdateScaleMatrix(slot1Rectangle, Configuration.PreserveAspectRatio);
        _secondaryVmuPresenter.UpdateScaleMatrix(slot2Rectangle, Configuration.PreserveAspectRatio);
    }

    private void Vmu_UnsavedChangesDetected()
    {
        UpdateWindowTitle();
    }

    internal int? GetWindowSizeMultiple()
    {
        var viewport = _graphics.GraphicsDevice.Viewport;
        if (viewport.Width % VmuPresenter.TotalContentWidth != 0)
            return null;

        if ((viewport.Height - MenuBarHeight) % VmuPresenter.TotalContentHeight != 0)
            return null;

        return viewport.Width / VmuPresenter.TotalContentWidth;
    }

    /// <summary>Sets a window size which is a multiple of <see cref="MinWidth"/>.</summary>
    internal void SetWindowSizeMultiple(int multiple)
    {
        _graphics.PreferredBackBufferWidth = VmuPresenter.TotalContentWidth * multiple;
        _graphics.PreferredBackBufferHeight = VmuPresenter.TotalContentHeight * multiple + MenuBarHeight;
        _graphics.ApplyChanges();
        UpdateScaleMatrix();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var gamepad = GamePad.GetState(PlayerIndex.One);

        // TODO: most operations which are performed on a single VMU, should be extracted to VmuPresenter.

        // Only respect a pause command if VMU is in the ejected state
        if (!PrimaryVmu.IsDocked && _buttonChecker.IsNewlyPressed(VmuButton.Pause, _previousKeys, keyboard, _previousGamepad, gamepad))
            Paused = !Paused;

        // TODO: system for selecting save slots etc
        if (_buttonChecker.IsNewlyPressed(VmuButton.SaveState, _previousKeys, keyboard, _previousGamepad, gamepad))
            PrimaryVmu.SaveState(id: "0");

        if (_buttonChecker.IsNewlyPressed(VmuButton.LoadState, _previousKeys, keyboard, _previousGamepad, gamepad))
        {
            if (PrimaryVmu.LoadStateById(id: "0", saveOopsFile: true) is (false, var error))
            {
                _userInterface.ShowToast(error ?? $"An unknown error occurred in {nameof(PrimaryVmu.LoadStateById)}.");
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
            PrimaryVmu.DockOrEject();
            if (PrimaryVmu.IsDocked)
                Paused = false;
        }

        // Let any button press wake the VMU from sleep
        if (Configuration.AnyButtonWakesFromSleep && !PrimaryVmu._cpu.SFRs.Vccr.DisplayControl && (byte)newP3 != 0xff)
        {
            newP3 = newP3 with { ButtonSleep = false };
        }

        PrimaryVmu._cpu.SFRs.P3 = newP3;

        _previousKeys = keyboard;
        _previousGamepad = gamepad;

        var rate = Paused ? 0 :
            IsFastForwarding ? gameTime.ElapsedGameTime.Ticks * 2 :
            gameTime.ElapsedGameTime.Ticks;

        PrimaryVmu._cpu.Run(rate);

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
        _primaryVmuPresenter.Draw(_spriteBatch);
        _secondaryVmuPresenter.Draw(_spriteBatch);
        _userInterface.Layout(gameTime);

        base.Draw(gameTime);
    }
}
