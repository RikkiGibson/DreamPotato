using System;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using DreamPotato.Core;
using DreamPotato.MonoGame.UI;
using System.Linq;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

namespace DreamPotato.MonoGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly string? _initialFilePath;

    internal Vmu PrimaryVmu => _primaryVmuPresenter.Vmu;

    [MemberNotNullWhen(true, nameof(SecondaryVmu))]
    internal bool UseSecondaryVmu => Configuration.ExpansionSlots == ExpansionSlots.Slot1And2;
    internal Vmu? SecondaryVmu => UseSecondaryVmu ? _secondaryVmuPresenter.Vmu : null;

    internal const int MenuBarHeight = 20;


    // Set in Initialize()
    internal Configuration Configuration = null!;
    internal ColorPalette ColorPalette = null!;
    internal RecentFilesInfo RecentFilesInfo = null!;
    private SpriteBatch _spriteBatch = null!;
    private MapleMessageBroker MapleMessageBroker = null!;
    private VmuPresenter _primaryVmuPresenter = null!;
    private VmuPresenter _secondaryVmuPresenter = null!;

    private UserInterface _userInterface = null!;

    // Dynamic state
    private KeyboardState _previousKeys;
    private GamePadState _previousGamepad;
    internal bool Paused;

    /// <summary>Note: comprises the whole screen region which secondary VMU menus can expand into.</summary>
    internal Rectangle SecondaryMenuBarRectangle;
    /// <summary>Only meaningful when <see cref="UseSecondaryVmu"/> is true.</summary>
    internal bool IsHorizontalLayout;

    public Game1(string? gameFilePath)
    {
        _graphics = new GraphicsDeviceManager(this);
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += Window_ClientSizeChanged;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _initialFilePath = gameFilePath;
    }

    [MemberNotNull(nameof(_spriteBatch), nameof(MapleMessageBroker), nameof(_primaryVmuPresenter), nameof(_secondaryVmuPresenter), nameof(_userInterface), nameof(Configuration), nameof(ColorPalette), nameof(RecentFilesInfo))]
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
        MapleMessageBroker = new MapleMessageBroker(LogLevel.Default);
        MapleMessageBroker.RestartServer(Configuration.DreamcastPort);
        var primaryVmu = new Vmu(MapleMessageBroker);
        initializeVmu(primaryVmu);
        primaryVmu.DockOrEject(connect: Configuration.VmuConnectionState is VmuConnectionState.PrimaryDocked or VmuConnectionState.PrimaryAndSecondaryDocked);
        primaryVmu.UnsavedChangesDetected += Vmu_UnsavedChangesDetected;
        RecentFilesInfo = RecentFilesInfo.Load();

        var secondaryVmu = new Vmu(MapleMessageBroker);
        initializeVmu(secondaryVmu);
        secondaryVmu.DockOrEject(connect: Configuration.VmuConnectionState is VmuConnectionState.SecondaryDocked or VmuConnectionState.PrimaryAndSecondaryDocked);

        _primaryVmuPresenter = new VmuPresenter(this, primaryVmu, textures, _graphics, Configuration.PrimaryInput);
        _secondaryVmuPresenter = new VmuPresenter(this, secondaryVmu, textures, _graphics, Configuration.SecondaryInput);
        UpdateScaleMatrix();
        UpdateAudioVolume();
        UpdateVmuExpansionSlots();

        if (Configuration.WindowPosition is { } windowPosition)
        {
            // Do not move the window to the saved position, if doing so would put us outside the bounds of the current display configuration.
            var windowRect = Window.ClientBounds.Size;
            if (_graphics.GraphicsDevice.DisplayMode.TitleSafeArea.Intersects(new Rectangle(windowPosition.X, windowPosition.Y, windowRect.X, windowRect.Y)))
                Window.Position = new Point(windowPosition.X, windowPosition.Y);
        }

        LoadVmuFiles(primaryVmu, _initialFilePath ?? RecentFilesInfo.PrimaryVmuMostRecent);
        LoadVmuFiles(secondaryVmu, _initialFilePath ?? RecentFilesInfo.SecondaryVmuMostRecent);
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        base.Initialize();

        void initializeVmu(Vmu vmu)
        {
            vmu.InitializeFlash(date);
            if (Configuration.AutoInitializeDate)
                vmu.InitializeDate(date);
        }
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        if ((PrimaryVmu.HasUnsavedChanges || SecondaryVmu?.HasUnsavedChanges == true)
            && _userInterface.PendingCommand is not { Kind: PendingCommandKind.Exit, State: ConfirmationState.Confirmed })
        {
            args.Cancel = true;
            _userInterface.ShowConfirmCommandDialog(PendingCommandKind.Exit, vmu: null);
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

    internal void LoadNewVmu(Vmu vmu)
    {
        vmu.LoadNewVmu(date: DateTime.Now, autoInitializeRTCDate: Configuration.AutoInitializeDate);
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

    internal void SaveVmuFileAs(Vmu vmu, string vmuFilePath)
    {
        var extension = Path.GetExtension(vmuFilePath);
        if (!extension.Equals(".vmu", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".bin", StringComparison.OrdinalIgnoreCase))
        {
            vmuFilePath = Path.ChangeExtension(vmuFilePath, ".vmu");
        }

        vmu.SaveVmuAs(vmuFilePath);
        UpdateWindowTitle();
        RecentFilesInfo = RecentFilesInfo.AddPrimaryVmuRecentFile(vmuFilePath);
        RecentFilesInfo.Save();
    }

    internal void Reset(Vmu vmu)
    {
        vmu.Reset(Configuration.AutoInitializeDate ? DateTimeOffset.Now : null);
        Paused = false;
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
        Configuration = Configuration with { Volume = newVolume };
        UpdateAudioVolume();
    }

    internal void Configuration_PaletteChanged(ColorPalette palette)
    {
        Configuration = Configuration with { ColorPaletteName = palette.Name };
        ColorPalette = palette;
    }

    internal void Configuration_DreamcastPortChanged(DreamcastPort dreamcastPort)
    {
        Configuration = Configuration with { DreamcastPort = dreamcastPort };
        MapleMessageBroker.RestartServer(dreamcastPort);
    }

    internal void Configuration_ExpansionSlotsChanged(ExpansionSlots expansionSlots)
    {
        var oldExpansionSlots = Configuration.ExpansionSlots;
        Configuration = Configuration with { ExpansionSlots = expansionSlots };
        var usingBothSlots = expansionSlots == ExpansionSlots.Slot1And2;
        if (usingBothSlots != (oldExpansionSlots == ExpansionSlots.Slot1And2))
        {
            // When going from a single slot to 2 slots or vice-versa, automatically adjust the window size
            if (usingBothSlots)
            {
                _graphics.PreferredBackBufferHeight *= 2;
            }
            else
            {
                if (IsHorizontalLayout)
                    _graphics.PreferredBackBufferWidth /= 2;
                else
                    _graphics.PreferredBackBufferHeight /= 2;
            }

            _graphics.ApplyChanges();
            UpdateScaleMatrix();
        }

        UpdateVmuExpansionSlots();

        // TODO: maple server needs to be able to operate independently of a single VMU
        // That implies contents of 2 VMUs need to be able to be stored/sync'd separately.
        // PrimaryVmu.RestartMapleServer(expansionSlots);
    }

    private void UpdateVmuExpansionSlots()
    {
        var wasDocked = PrimaryVmu.IsDocked;
        if (wasDocked)
            PrimaryVmu.DockOrEject();

        var secondaryWasDocked = SecondaryVmu?.IsDocked == true;
        if (secondaryWasDocked)
            SecondaryVmu!.DockOrEject();

        switch (Configuration.ExpansionSlots)
        {
            case ExpansionSlots.Slot1:
                PrimaryVmu.DreamcastSlot = DreamcastSlot.Slot1;
                break;
            case ExpansionSlots.Slot2:
                PrimaryVmu.DreamcastSlot = DreamcastSlot.Slot2;
                break;
            case ExpansionSlots.Slot1And2:
                Debug.Assert(SecondaryVmu is not null);
                PrimaryVmu.DreamcastSlot = DreamcastSlot.Slot1;
                SecondaryVmu.DreamcastSlot = DreamcastSlot.Slot2;
                break;
        }

        if (wasDocked)
            PrimaryVmu.DockOrEject();

        if (secondaryWasDocked)
            SecondaryVmu!.DockOrEject();
    }

    internal void Configuration_MuteSecondaryVmuAudioChanged(bool newValue)
    {
        Configuration = Configuration with { MuteSecondaryVmuAudio = newValue };
        UpdateAudioVolume();
    }

    internal void Configuration_DoneEditing()
    {
        Configuration.Save();
    }

    internal void Configuration_DoneEditingKeyMappings(ImmutableArray<KeyMapping> keyMappings, bool forPrimary)
    {
        if (forPrimary)
        {
            Configuration = Configuration with { PrimaryInput = Configuration.PrimaryInput with { KeyMappings = keyMappings } };
            _primaryVmuPresenter.UpdateButtonChecker(Configuration.PrimaryInput);
        }
        else
        {
            Configuration = Configuration with { SecondaryInput = Configuration.SecondaryInput with { KeyMappings = keyMappings } };
            _secondaryVmuPresenter.UpdateButtonChecker(Configuration.SecondaryInput);
        }

        Configuration.Save();
    }

    internal void Configuration_DoneEditingButtonMappings(ImmutableArray<ButtonMapping> buttonMappings, bool forPrimary)
    {
        if (forPrimary)
        {
            Configuration = Configuration with { PrimaryInput = Configuration.PrimaryInput with { ButtonMappings = buttonMappings } };
            _primaryVmuPresenter.UpdateButtonChecker(Configuration.PrimaryInput);
        }
        else
        {
            Configuration = Configuration with { SecondaryInput = Configuration.SecondaryInput with { ButtonMappings = buttonMappings } };
            _secondaryVmuPresenter.UpdateButtonChecker(Configuration.SecondaryInput);
        }

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
        IsHorizontalLayout = Configuration.PreserveAspectRatio &&
            (float)contentRectangle.Height / VmuPresenter.TotalContentHeight < (float)contentRectangle.Width / VmuPresenter.TotalContentWidth;

        (var slot1Rectangle, SecondaryMenuBarRectangle, var slot2Rectangle) = IsHorizontalLayout ? layoutHorizontal(contentRectangle) : layoutVertical(contentRectangle);

        _primaryVmuPresenter.UpdateScaleMatrix(slot1Rectangle, Configuration.PreserveAspectRatio);
        _secondaryVmuPresenter.UpdateScaleMatrix(slot2Rectangle, Configuration.PreserveAspectRatio);

        static (Rectangle slot1Rectangle, Rectangle secondaryMenuBarRectangle, Rectangle slot2Rectangle) layoutHorizontal(Rectangle contentRectangle)
        {
            var slot1Rectangle = contentRectangle with { Width = contentRectangle.Width / 2 };
            var secondaryMenuBarRectangle = new Rectangle(x: slot1Rectangle.X + slot1Rectangle.Width, y: 0, width: slot1Rectangle.Width, height: MenuBarHeight + slot1Rectangle.Height);
            var slot2Rectangle = contentRectangle with { Width = contentRectangle.Width / 2, X = slot1Rectangle.X + slot1Rectangle.Width };
            return (slot1Rectangle, secondaryMenuBarRectangle, slot2Rectangle);
        }

        static (Rectangle slot1Rectangle, Rectangle secondaryMenuBarRectangle, Rectangle slot2Rectangle) layoutVertical(Rectangle contentRectangle)
        {
            // When a vertical layout is used, extra space needs to be reserved for a 2nd menu bar
            var slot1Rectangle = contentRectangle with { Height = (contentRectangle.Height - MenuBarHeight) / 2 };
            var secondaryMenuBarRectangle = new Rectangle(x: 0, y: slot1Rectangle.Y + slot1Rectangle.Height, width: slot1Rectangle.Width, height: MenuBarHeight + slot1Rectangle.Height);
            var slot2Rectangle = contentRectangle with { Height = slot1Rectangle.Height, Y = secondaryMenuBarRectangle.Y + MenuBarHeight };
            return (slot1Rectangle, secondaryMenuBarRectangle, slot2Rectangle);
        }
    }

    private void UpdateAudioVolume()
    {
        var volume = Configuration.Volume;
        _primaryVmuPresenter.Vmu.Audio.Volume = volume;
        _secondaryVmuPresenter.Vmu.Audio.Volume = Configuration.MuteSecondaryVmuAudio ? 0 : volume;
    }

    private void Vmu_UnsavedChangesDetected()
    {
        UpdateWindowTitle();
    }

    internal int? GetWindowSizeMultiple()
    {
        // The automatic commands always use a vertical layout, so, we are never "at an integer size multiple" in a horizontal layout.
        if (UseSecondaryVmu && IsHorizontalLayout)
            return null;

        var viewport = _graphics.GraphicsDevice.Viewport;

        // ensure width is an even multiple of 'baseWidth'
        if (viewport.Width % VmuPresenter.TotalContentWidth != 0)
            return null;

        var multiple = UseSecondaryVmu ? 2 : 1;
        var baseVmuContentHeight = multiple * VmuPresenter.TotalContentHeight;
        if ((viewport.Height - multiple * MenuBarHeight) % baseVmuContentHeight != 0)
            return null;

        return viewport.Width / VmuPresenter.TotalContentWidth;
    }

    /// <summary>Sets a window size which is a multiple of <see cref="MinWidth"/>.</summary>
    internal void SetWindowSizeMultiple(int multiple)
    {
        _graphics.PreferredBackBufferWidth = VmuPresenter.TotalContentWidth * multiple;
        var vmuCount = UseSecondaryVmu ? 2 : 1;
        _graphics.PreferredBackBufferHeight = vmuCount * (VmuPresenter.TotalContentHeight * multiple + MenuBarHeight);
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

        if (UseSecondaryVmu)
            _secondaryVmuPresenter.Update(gameTime, _previousKeys, _previousGamepad, keyboard, gamepad);

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
