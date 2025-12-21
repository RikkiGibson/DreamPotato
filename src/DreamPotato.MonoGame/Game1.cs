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
    private readonly string? _commandLineFilePath;

    internal Vmu PrimaryVmu => _primaryVmuPresenter.Vmu;
    internal VmuPresenter PrimaryVmuPresenter => _primaryVmuPresenter;

    [MemberNotNullWhen(true, nameof(SecondaryVmu), nameof(SecondaryVmuPresenter))]
    internal bool UseSecondaryVmu => Configuration.ExpansionSlots == ExpansionSlots.Slot1And2;
    internal Vmu? SecondaryVmu => SecondaryVmuPresenter?.Vmu;
    internal VmuPresenter? SecondaryVmuPresenter => UseSecondaryVmu ? _secondaryVmuPresenter : null;
    internal UserInterface UserInterface => _userInterface;

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

    /// <summary>Global pause flag.</summary>
    internal bool GlobalPaused;

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
        _commandLineFilePath = gameFilePath;
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
            IconDreamcastConnectedTexture = Content.Load<Texture2D>("DreamcastConnectedIcon"),
            IconVmusConnectedTexture = Content.Load<Texture2D>("VmusConnectedIcon"),
        };

        _userInterface = new UserInterface(this);
        _userInterface.Initialize(textures.IconDreamcastConnectedTexture, textures.IconVmusConnectedTexture);

        MapleMessageBroker = new MapleMessageBroker(LogLevel.Default);
        MapleMessageBroker.RestartServer(Configuration.DreamcastPort);
        RecentFilesInfo = RecentFilesInfo.Load();

        var primaryVmu = new Vmu(MapleMessageBroker) { DreamcastSlot = Configuration.ExpansionSlots is ExpansionSlots.Slot1 or ExpansionSlots.Slot1And2 ? DreamcastSlot.Slot1 : DreamcastSlot.Slot2 };
        primaryVmu.UnsavedChangesDetected += Vmu_UnsavedChangesDetected;
        _primaryVmuPresenter = new VmuPresenter(this, primaryVmu, textures, _graphics, Configuration.PrimaryInput);

        var secondaryVmu = new Vmu(MapleMessageBroker) { DreamcastSlot = DreamcastSlot.Slot2 };
        _secondaryVmuPresenter = new VmuPresenter(this, secondaryVmu, textures, _graphics, Configuration.SecondaryInput);

        var date = DateTimeOffset.Now;
        initializeVmu(_primaryVmuPresenter, date, _commandLineFilePath ?? RecentFilesInfo.PrimaryVmuMostRecent);
        initializeVmu(_secondaryVmuPresenter, date, RecentFilesInfo.SecondaryVmuMostRecent);

        UpdateScaleMatrix();
        UpdateAudioVolume();

        Debug.Assert(!primaryVmu.IsDockedToDreamcast && !secondaryVmu.IsDockedToDreamcast);
        var connectionState = Configuration.VmuConnectionState;
        primaryVmu.DockOrEjectToDreamcast(connect: connectionState is VmuConnectionState.PrimaryDocked or VmuConnectionState.PrimaryAndSecondaryDocked);
        secondaryVmu.DockOrEjectToDreamcast(connect: connectionState is VmuConnectionState.SecondaryDocked or VmuConnectionState.PrimaryAndSecondaryDocked);
        // Secondary must not be docked if primary is associated with slot 2
        Debug.Assert(!(primaryVmu.DreamcastSlot == DreamcastSlot.Slot2 && secondaryVmu.IsDockedToDreamcast));

        if (Configuration.WindowPosition is { } windowPosition)
        {
            // Do not move the window to the saved position, if doing so would put us outside the bounds of the current display configuration.
            var windowRect = Window.ClientBounds.Size;
            if (_graphics.GraphicsDevice.DisplayMode.TitleSafeArea.Intersects(new Rectangle(windowPosition.X, windowPosition.Y, windowRect.X, windowRect.Y)))
                Window.Position = new Point(windowPosition.X, windowPosition.Y);
        }

        _spriteBatch = new SpriteBatch(GraphicsDevice);

        base.Initialize();

        void initializeVmu(VmuPresenter presenter, DateTimeOffset date, string? vmsOrVmuFilePath)
        {
            var vmu = presenter.Vmu;
            vmu.InitializeFlash(date);
            if (Configuration.AutoInitializeDate)
                vmu.InitializeDate(date);

            vmu.LoadRom();
            if (vmsOrVmuFilePath != null && File.Exists(vmsOrVmuFilePath))
                LoadAndStartVmsOrVmuFile(presenter, vmsOrVmuFilePath);
        }
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        if ((PrimaryVmu.HasUnsavedChanges || SecondaryVmu?.HasUnsavedChanges == true)
            && _userInterface.PendingCommand is not { Kind: PendingCommandKind.Exit, State: ConfirmationState.Confirmed })
        {
            args.Cancel = true;
            _userInterface.ShowConfirmCommandDialog(PendingCommandKind.Exit, vmuPresenter: null);
        }

        // Save window size and position on exit
        var viewport = _graphics.GraphicsDevice.Viewport;
        var position = Window.Position;
        Configuration = Configuration with
        {
            ViewportSize = new ViewportSize(viewport.Width, viewport.Height),
            WindowPosition = new WindowPosition(position.X, position.Y),
            VmuConnectionState = (PrimaryVmu.IsDockedToDreamcast, SecondaryVmu?.IsDockedToDreamcast) switch
            {
                (true, true) => VmuConnectionState.PrimaryAndSecondaryDocked,
                (true, false or null) => VmuConnectionState.PrimaryDocked,
                (false, true) => VmuConnectionState.SecondaryDocked,
                (false, false or null) => VmuConnectionState.None,
            }
        };
        Configuration.Save();

        base.OnExiting(sender, args);
    }

    /// <summary>
    /// This is always based on the primary vmu state.
    /// Secondary indicates its status using Dear ImGui so there isn't a need to proactively go update its state.
    /// </summary>
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

    internal void LoadNewVmu(VmuPresenter presenter)
    {
        var vmu = presenter.Vmu;
        vmu.LoadNewVmu(date: DateTime.Now, autoInitializeRTCDate: Configuration.AutoInitializeDate);
        presenter.LocalPaused = false;
        UpdateWindowTitle();
        RecentFilesInfo = RecentFilesInfo.AddRecentFile(forPrimary: presenter == _primaryVmuPresenter, newRecentFile: null);
        RecentFilesInfo.Save();
    }

    internal void LoadAndStartVmsOrVmuFile(VmuPresenter presenter, string filePath)
    {
        var vmu = presenter.Vmu;
        var extension = Path.GetExtension(filePath);
        if (extension.Equals(".vms", StringComparison.OrdinalIgnoreCase))
        {
            vmu.LoadGameVms(filePath, DateTime.Now);
        }
        else if (extension.Equals(".vmu", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bin", StringComparison.OrdinalIgnoreCase))
        {
            if (!tryLoadVmuFile())
                return;
        }
        else
        {
            throw new ArgumentException($"Cannot load '{filePath}' because it is not a '.vms', '.vmu', or '.bin' file.");
        }

        presenter.LocalPaused = false;
        UpdateWindowTitle();
        RecentFilesInfo = RecentFilesInfo.AddRecentFile(forPrimary: vmu == PrimaryVmu, filePath);
        RecentFilesInfo.Save();

        bool tryLoadVmuFile()
        {
            // We need to enforce that the same VMU file is not opened by both VMUs.
            // Otherwise they could stomp on each others' on-disk content.
            // (Opening the same .vms file is fine.)
            var otherVmu = vmu == PrimaryVmu ? SecondaryVmu : PrimaryVmu;
            if (otherVmu?.LoadedFilePath == filePath)
            {
                _userInterface.ShowToast($"Cannot open {Path.GetFileName(filePath)} because it is already open on the other VMU.");
                return false;
            }

            vmu.LoadVmu(filePath, DateTime.Now);
            return true;
        }
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
        RecentFilesInfo = RecentFilesInfo.AddRecentFile(forPrimary: vmu == PrimaryVmu, vmuFilePath);
        RecentFilesInfo.Save();
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

    internal void Configuration_ExpansionSlotsChanged(ExpansionSlots newExpansionSlots)
    {
        var oldExpansionSlots = Configuration.ExpansionSlots;
        // First eject any docked VMUs, then update settings, then re-insert the VMUs which are still being used.
        // Note that changing 'Configuration.ExpansionSlots' affects the nullability of 'SecondaryVmu'.

        // The secondary VMU must not be docked when the primary is associated with slot 2, otherwise they will stomp on each other's data
        Debug.Assert(!(PrimaryVmu.DreamcastSlot == DreamcastSlot.Slot2 && SecondaryVmu?.IsDockedToDreamcast == true));
        var wasDocked = PrimaryVmu.IsDockedToDreamcast;
        PrimaryVmu.DockOrEjectToDreamcast(connect: false);

        var secondaryWasDocked = SecondaryVmu?.IsDockedToDreamcast == true;
        SecondaryVmu?.DockOrEjectToDreamcast(connect: false);

        Configuration = Configuration with { ExpansionSlots = newExpansionSlots };
        PrimaryVmu.DreamcastSlot = Configuration.ExpansionSlots is ExpansionSlots.Slot1 or ExpansionSlots.Slot1And2 ? DreamcastSlot.Slot1 : DreamcastSlot.Slot2;

        if (wasDocked)
            PrimaryVmu.DockOrEjectToDreamcast();

        if (secondaryWasDocked)
            SecondaryVmu?.DockOrEjectToDreamcast();

        // Adjust the window size if changing from single slot to 2 slots.
        var usingBothSlots = newExpansionSlots == ExpansionSlots.Slot1And2;
        if (usingBothSlots != (oldExpansionSlots == ExpansionSlots.Slot1And2))
        {
            if (usingBothSlots)
            {
                var newHeight = _graphics.PreferredBackBufferHeight * 2;
                var safeArea = _graphics.GraphicsDevice.DisplayMode.TitleSafeArea;
                // TODO: simply doubling the height, as long as it is within screen bounds, isn't really what we want.
                // What we want, is the smallest increase in either dimension, which meets these constraints:
                // - The window remains within the bounds of the user's screen
                // - It preserves the current single VMU size as closely as possible (possibly getting smaller)
                // - It changes the window size as little as possible while still meeting the VMU size constraint.
                if (newHeight < safeArea.Bottom)
                    _graphics.PreferredBackBufferHeight = newHeight;
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

    internal void Configuration_DoneEditingButtonMappings(ImmutableArray<ButtonMapping> buttonMappings, int gamePadIndex, bool forPrimary)
    {
        if (forPrimary)
        {
            Configuration = Configuration with { PrimaryInput = Configuration.PrimaryInput with { ButtonMappings = buttonMappings, GamePadIndex = gamePadIndex } };
            _primaryVmuPresenter.UpdateButtonChecker(Configuration.PrimaryInput);
        }
        else
        {
            Configuration = Configuration with { SecondaryInput = Configuration.SecondaryInput with { ButtonMappings = buttonMappings, GamePadIndex = gamePadIndex } };
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

        if (UseSecondaryVmu)
        {
            // For a VMU-to-VMU connection, the secondary should 'Update' only.
            // In that case, the primary will run both CPUs in sync in its 'UpdateAndRun' call.
            if (PrimaryVmu.IsOtherVmuConnected)
                _secondaryVmuPresenter.Update(_previousKeys, keyboard);
            else
                _secondaryVmuPresenter.UpdateAndRun(gameTime, _previousKeys, keyboard);
        }

        _primaryVmuPresenter.UpdateAndRun(gameTime, _previousKeys, keyboard);

        MapleMessageBroker.RefreshIfNeeded();

        _previousKeys = keyboard;
        base.Update(gameTime);
    }

    /// <summary>Note: fast forwarding is a global command because execution of primary+secondary VMUs are synchronized.</summary>
    internal bool IsFastForwarding
    {
        get
        {
            if (GlobalPaused)
                return false;

            if (_primaryVmuPresenter.ButtonChecker.IsPressed(VmuButton.FastForward, _previousKeys, _primaryVmuPresenter.PreviousGamepad))
                return true;

            if (SecondaryVmuPresenter?.ButtonChecker.IsPressed(VmuButton.FastForward, _previousKeys, SecondaryVmuPresenter.PreviousGamepad) == true)
                return true;

            return false;
        }
    }

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
