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
using System.Diagnostics.CodeAnalysis;

namespace DreamPotato.MonoGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly string? _initialFilePath;

    internal Vmu PrimaryVmu => _primaryVmuPresenter.Vmu;
    internal bool UseSecondaryVmu => Configuration.ExpansionSlots == ExpansionSlots.Slot1And2;

    internal const int MenuBarHeight = 20;


    // Set in Initialize()
    internal Configuration Configuration = null!;
    internal ColorPalette ColorPalette = null!;
    internal RecentFilesInfo RecentFilesInfo = null!;
    private SpriteBatch _spriteBatch = null!;
    private VmuPresenter _primaryVmuPresenter = null!;
    private VmuPresenter _secondaryVmuPresenter = null!;

    private UserInterface _userInterface = null!;

    // Dynamic state
    private KeyboardState _previousKeys;
    private GamePadState _previousGamepad;
    internal bool Paused;

    public Game1(string? gameFilePath)
    {
        _graphics = new GraphicsDeviceManager(this);
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += Window_ClientSizeChanged;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _initialFilePath = gameFilePath;
    }

    [MemberNotNull(nameof(_spriteBatch), nameof(_primaryVmuPresenter), nameof(_secondaryVmuPresenter), nameof(_userInterface), nameof(Configuration), nameof(ColorPalette), nameof(RecentFilesInfo))]
    protected override void Initialize()
    {
        Configuration = Configuration.Load();
        Configuration.Save();
        ColorPalette = ColorPalette.AllPalettes.FirstOrDefault(palette => palette.Name == Configuration.ColorPaletteName) ?? ColorPalette.AllPalettes[0];

        var windowSize = Configuration.ViewportSize;
        _graphics.PreferredBackBufferWidth = windowSize.Width;
        _graphics.PreferredBackBufferHeight = windowSize.Height;
        _graphics.ApplyChanges();

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

        var date = DateTime.Now;
        var primaryVmu = new Vmu();
        initializeVmu(primaryVmu);
        primaryVmu.DockOrEject(connect: Configuration.VmuConnectionState is VmuConnectionState.PrimaryDocked or VmuConnectionState.PrimaryAndSecondaryDocked);
        primaryVmu.UnsavedChangesDetected += Vmu_UnsavedChangesDetected;
        RecentFilesInfo = RecentFilesInfo.Load();

        // TODO: share/pass in a MapleMessageBroker for 2 VMUs
        var secondaryVmu = new Vmu();
        initializeVmu(secondaryVmu);
        secondaryVmu.DockOrEject(connect: Configuration.VmuConnectionState is VmuConnectionState.SecondaryDocked or VmuConnectionState.PrimaryAndSecondaryDocked);

        _primaryVmuPresenter = new VmuPresenter(this, primaryVmu, textures, _graphics, Configuration.PrimaryInput);
        _secondaryVmuPresenter = new VmuPresenter(this, secondaryVmu, textures, _graphics, Configuration.SecondaryInput);
        UpdateScaleMatrix();

        if (Configuration.WindowPosition is { } windowPosition)
        {
            // Do not move the window to the saved position, if doing so would put us outside the bounds of the current display configuration.
            var windowRect = Window.ClientBounds.Size;
            if (_graphics.GraphicsDevice.DisplayMode.TitleSafeArea.Intersects(new Rectangle(windowPosition.X, windowPosition.Y, windowRect.X, windowRect.Y)))
                Window.Position = new Point(windowPosition.X, windowPosition.Y);
        }

        LoadVmuFiles(primaryVmu, _initialFilePath ?? RecentFilesInfo.PrimaryVmuMostRecent);
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        base.Initialize();

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
            VmuConnectionState = PrimaryVmu.IsDocked ? VmuConnectionState.PrimaryDocked : VmuConnectionState.None,
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

    private void LoadVmuFiles(Vmu vmu, string? vmsOrVmuFilePath)
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
        RecentFilesInfo = RecentFilesInfo.AddPrimaryVmuRecentFile(newRecentFile: null);
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
        RecentFilesInfo = RecentFilesInfo.AddPrimaryVmuRecentFile(filePath);
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
        RecentFilesInfo = RecentFilesInfo.AddPrimaryVmuRecentFile(vmuFilePath);
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
        Configuration = Configuration with { ColorPaletteName = palette.Name };
        ColorPalette = palette;
    }

    internal void Configuration_DreamcastPortChanged(DreamcastPort dreamcastPort)
    {
        Configuration = Configuration with { DreamcastPort = dreamcastPort };
        PrimaryVmu.RestartMapleServer(dreamcastPort);
    }

    internal void Configuration_ExpansionSlotsChanged(ExpansionSlots expansionSlots)
    {
        var oldExpansionSlots = Configuration.ExpansionSlots;
        Configuration = Configuration with { ExpansionSlots = expansionSlots };
        var oldWasUsingBothSlots = oldExpansionSlots == ExpansionSlots.Slot1And2;
        if (oldWasUsingBothSlots != (expansionSlots == ExpansionSlots.Slot1And2))
            UpdateScaleMatrix();

        // TODO: maple server needs to be able to operate independently of a single VMU
        // That implies contents of 2 VMUs need to be able to be stored/sync'd separately.
        // PrimaryVmu.RestartMapleServer(expansionSlots);
    }

    internal void Configuration_DoneEditing()
    {
        Configuration.Save();
    }

    internal void Configuration_DoneEditingKeyMappings(ImmutableArray<KeyMapping> keyMappings)
    {
        Configuration = Configuration with { PrimaryInput = Configuration.PrimaryInput with { KeyMappings = keyMappings } };
        _primaryVmuPresenter.UpdateButtonChecker(Configuration.PrimaryInput);
        Configuration.Save();
    }

    internal void Configuration_DoneEditingButtonMappings(ImmutableArray<ButtonMapping> buttonMappings)
    {
        Configuration = Configuration with { PrimaryInput = Configuration.PrimaryInput with { ButtonMappings = buttonMappings } };
        _primaryVmuPresenter.UpdateButtonChecker(Configuration.PrimaryInput);
        Configuration.Save();
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

        if (!UseSecondaryVmu)
        {
            _primaryVmuPresenter.UpdateScaleMatrix(contentRectangle, Configuration.PreserveAspectRatio);
            return;
        }

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
        var buttonChecker = _primaryVmuPresenter.ButtonChecker;
        if (!PrimaryVmu.IsDocked && buttonChecker.IsNewlyPressed(VmuButton.Pause, _previousKeys, keyboard, _previousGamepad, gamepad))
            Paused = !Paused;

        // TODO: system for selecting save slots etc
        if (buttonChecker.IsNewlyPressed(VmuButton.SaveState, _previousKeys, keyboard, _previousGamepad, gamepad))
            PrimaryVmu.SaveState(id: "0");

        if (buttonChecker.IsNewlyPressed(VmuButton.LoadState, _previousKeys, keyboard, _previousGamepad, gamepad))
        {
            if (PrimaryVmu.LoadStateById(id: "0", saveOopsFile: true) is (false, var error))
            {
                _userInterface.ShowToast(error ?? $"An unknown error occurred in {nameof(PrimaryVmu.LoadStateById)}.");
            }
        }

        _primaryVmuPresenter.Update(gameTime, _previousKeys, _previousGamepad, keyboard, gamepad);
        if (PrimaryVmu.IsDocked)
            Paused = false;

        _previousKeys = keyboard;
        _previousGamepad = gamepad;
        base.Update(gameTime);
    }

    internal bool IsFastForwarding
        => _primaryVmuPresenter.ButtonChecker.IsPressed(VmuButton.FastForward, _previousKeys, _previousGamepad);

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(ColorPalette.Margin);
        _primaryVmuPresenter.Draw(_spriteBatch);
        if (UseSecondaryVmu)
            _secondaryVmuPresenter.Draw(_spriteBatch);

        _userInterface.Layout(gameTime);

        base.Draw(gameTime);
    }
}
