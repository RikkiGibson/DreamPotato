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

    internal readonly Vmu Vmu;

    private const int MinVmuScale = 3;
    private const int ScaledWidth = Display.ScreenWidth * MinVmuScale;
    private const int ScaledHeight = Display.ScreenHeight * MinVmuScale;

    private const int IconSize = 32;

    internal const int MenuBarHeight = 20;
    private const int TopMargin = MinVmuScale * 2;
    private const int SideMargin = MinVmuScale * 3;
    private const int BottomMargin = MinVmuScale * 12;

    internal const int TotalContentWidth = ScaledWidth + SideMargin * 2;
    internal const int TotalContentHeight = ScaledHeight + TopMargin + BottomMargin;

    internal const int MinWidth = TotalContentWidth;
    internal const int MinHeight = TotalContentHeight + MenuBarHeight;

    private const int SleepToggleInsertEjectFrameCount = 60; // 1 second

    // Set in Initialize()
    private SpriteBatch _spriteBatch = null!;
    private Matrix _spriteTransformMatrix;
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

        // Save window size and position on exit
        var viewport = _graphics.GraphicsDevice.Viewport;
        var position = Window.Position;
        Configuration = Configuration with
        {
            ViewportSize = new ViewportSize(viewport.Width, viewport.Height),
            WindowPosition = new WindowPosition(position.X, position.Y)
        };
        Configuration.Save();

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
        Vmu.LoadRom();
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

    internal void Configuration_PreserveAspectRatioChanged(bool newValue)
    {
        Configuration = Configuration with { PreserveAspectRatio = newValue };
        UpdateScaleMatrix();
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
        UpdateScaleMatrix();

        if (Configuration.WindowPosition is { } windowPosition)
        {
            var windowSize = Window.ClientBounds.Size;
            if (_graphics.GraphicsDevice.DisplayMode.TitleSafeArea.Intersects(new Rectangle(windowPosition.X, windowPosition.Y, windowSize.X, windowSize.Y)))
                Window.Position = new Point(windowPosition.X, windowPosition.Y);
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

    private void Window_ClientSizeChanged(object? sender, EventArgs e)
    {
        var viewport = _graphics.GraphicsDevice.Viewport;
        if (viewport.Width < MinWidth || viewport.Height < MinHeight)
            SetWindowSizeMultiple(multiple: 1);

        UpdateScaleMatrix();
    }

    private void UpdateScaleMatrix()
    {
        // Apply transforms:
        // - scale based on window size
        // - translate horizontally to ensure our content is centered
        // - translate vertically to ensure content is below menu bar

        // MinVmuScale is 3, and this is the base scale used for drawing.
        // But, we want to support 4x, 5x, etc. as the user resizes the window.
        // Therefore, get a scale based on the screen size, and round it down to the nearest 1/3 on each axis.
        float widthScale = (float)Math.Floor(
            (float)_graphics.GraphicsDevice.Viewport.Width / TotalContentWidth * MinVmuScale) / MinVmuScale;
        float heightScale = (float)Math.Floor(
            (float)(_graphics.GraphicsDevice.Viewport.Height - MenuBarHeight) / TotalContentHeight * MinVmuScale) / MinVmuScale;

        float minScale = Math.Min(widthScale, heightScale);
        Matrix scaleTransform = Configuration.PreserveAspectRatio
            ? Matrix.CreateScale(minScale, minScale, 1)
            : Matrix.CreateScale(widthScale, heightScale, 1);
        _spriteTransformMatrix = scaleTransform * Matrix.CreateTranslation(xPosition: getTransformXPosition(), yPosition: MenuBarHeight, 1);

        float getTransformXPosition()
        {
            float scale = Configuration.PreserveAspectRatio ? minScale : widthScale;
            float idealWidth = scale * TotalContentWidth;
            float actualWidth = _graphics.GraphicsDevice.Viewport.Width;

            // Center the content horizontally, by shifting it over
            // |--------| actual
            // __|----|__ ideal
            // We are calculating the quantity denoted by '__' in the above sketch
            // Since we're using nearest neighbor scaling, we need to round to nearest integer, to keep it from looking blocky.
            return (float)Math.Round((actualWidth - idealWidth) / 2);
        }
    }

    private void Vmu_UnsavedChangesDetected()
    {
        UpdateWindowTitle();
    }

    internal int? GetWindowSizeMultiple()
    {
        var viewport = _graphics.GraphicsDevice.Viewport;
        if (viewport.Width % TotalContentWidth != 0)
            return null;

        if ((viewport.Height - MenuBarHeight) % TotalContentHeight != 0)
            return null;

        return viewport.Width / TotalContentWidth;
    }

    /// <summary>Sets a window size which is a multiple of <see cref="MinWidth"/>.</summary>
    internal void SetWindowSizeMultiple(int multiple)
    {
        _graphics.PreferredBackBufferWidth = TotalContentWidth * multiple;
        _graphics.PreferredBackBufferHeight = TotalContentHeight * multiple + MenuBarHeight;
        _graphics.ApplyChanges();
        UpdateScaleMatrix();
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

        // Use nearest neighbor scaling for the screen content
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _spriteTransformMatrix);

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
        _spriteBatch.End();

        // Draw icons
        // If we are at an even multiple of the base screen size, or, if the screen is just huge, use nearest neighbor scaling
        var iconsSamplerState = Math.Floor(_spriteTransformMatrix.Down.Y) == _spriteTransformMatrix.Down.Y || _spriteTransformMatrix.Down.Y < -3
            ? SamplerState.PointClamp
            : SamplerState.LinearWrap;
        _spriteBatch.Begin(samplerState: iconsSamplerState, transformMatrix: _spriteTransformMatrix);
        int iconsYPos = vmuIsEjected ? TopMargin + ScaledHeight + MinVmuScale / 2 : TopMargin - MinVmuScale / 2;
        const int iconSpacing = MinVmuScale * 2;
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
