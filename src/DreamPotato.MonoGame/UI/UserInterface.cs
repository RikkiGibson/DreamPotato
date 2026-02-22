
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using DreamPotato.Core;
using DreamPotato.Core.SFRs;

using Humanizer;

using ImGuiNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using NativeFileDialogSharp;

using Numerics = System.Numerics;

namespace DreamPotato.MonoGame.UI;

enum ConfirmationState
{
    None,
    ShowDialog,
    Confirmed,
}

enum PendingCommandKind
{
    None,
    NewVmu,
    OpenVmu,
    OpenRecent,
    Reset,
    Exit
}

readonly struct PendingCommand
{
    private PendingCommand(ConfirmationState confirmationState, PendingCommandKind kind, VmuPresenter? vmuPresenter, string? filePath)
    {
        // If we are showing dialog or confirming something, there needs to be an actual command
        if (confirmationState is not ConfirmationState.None && kind is PendingCommandKind.None)
            throw new ArgumentException(null, nameof(kind));

        State = confirmationState;
        Kind = kind;
        VmuPresenter = vmuPresenter;
        FilePath = filePath;
    }

    public ConfirmationState State { get; }
    public PendingCommandKind Kind { get; }
    public VmuPresenter? VmuPresenter { get; }
    public string? FilePath { get; }

    public static PendingCommand ShowDialog(PendingCommandKind kind, VmuPresenter? vmuPresenter, string? filePath) => new(ConfirmationState.ShowDialog, kind, vmuPresenter, filePath);
    public PendingCommand Confirmed() => new(ConfirmationState.Confirmed, Kind, VmuPresenter, FilePath);
}

class SaveStateInfo
{
    public SaveStateInfo(GraphicsDevice graphics, ImGuiRenderer imGuiRenderer)
    {
        ThumbnailTexture = new Texture2D(graphics, Display.ScreenWidth, Display.ScreenHeight);
        RawThumbnailTexture = imGuiRenderer.BindTexture(ThumbnailTexture);
        StateTimeDescription = "";
    }

    public Texture2D ThumbnailTexture { get; }
    public nint RawThumbnailTexture { get; }

    public void InvalidateThumbnail() => StateFilePath = "<<unset>>";
    public string? StateFilePath { get; set; } = "<<unset>>";
    public string StateTimeDescription { get; set; }
    public bool Exists { get; set; }
}

class ToastInfo
{
    public ToastInfo(GraphicsDevice graphics, ImGuiRenderer imGuiRenderer)
    {
        ImageTexture = new Texture2D(graphics, Display.ScreenWidth, Display.ScreenHeight);
        RawImageTexture = imGuiRenderer.BindTexture(ImageTexture);
    }

    public string? Message { get; set; }
    public Texture2D ImageTexture { get; }
    public bool ShowImage { get; set; }
    public nint RawImageTexture { get; }
    public int DisplayFrames { get; set; }

    public const int DefaultDurationFrames = 3 * 60;
    public const int BeginFadeoutFrames = 30;
}

struct MappingEditState
{
    private object? _editedMappings;

    /// <summary>What gamepad is currently selected in the combo box? (<see cref="InputMappings.GamePadIndex_None"/> when editing keyboard mappings)</summary>
    public int GamePadIndex;

    /// <summary>Which preset is currently selected in the combo box?</summary>
    public int PresetIndex;

    /// <summary>Which element of '_editedMappings' are we currently editing the key or button for?</summary>
    public int EditedIndex;

    /// <summary>Which VMU are we editing mappings for?</summary>
    public Vmu? TargetVmu { get; }

    private MappingEditState(object editedMappings, int gamePadIndex, Vmu targetVmu)
    {
        if (editedMappings is not (List<KeyMapping> or List<ButtonMapping>))
            throw new ArgumentException(null, nameof(editedMappings));

        if (editedMappings is List<KeyMapping> && gamePadIndex != InputMappings.GamePadIndex_None)
            throw new ArgumentOutOfRangeException(nameof(gamePadIndex));

        _editedMappings = editedMappings;
        GamePadIndex = gamePadIndex;
        PresetIndex = 0;
        EditedIndex = -1;
        TargetVmu = targetVmu;
    }

    public static MappingEditState EditKeyMappings(ImmutableArray<KeyMapping> mappings, Vmu targetVmu)
        => new(mappings.ToList(), gamePadIndex: InputMappings.GamePadIndex_None, targetVmu);

    public static MappingEditState EditButtonMappings(ImmutableArray<ButtonMapping> mappings, int gamePadIndex, Vmu targetVmu)
        => new(mappings.ToList(), gamePadIndex, targetVmu);

    public List<KeyMapping>? KeyMappings { get => _editedMappings as List<KeyMapping>; set => _editedMappings = value; }
    public List<ButtonMapping>? ButtonMappings { get => _editedMappings as List<ButtonMapping>; set => _editedMappings = value; }
}

partial class UserInterface
{
    private readonly Game1 _game;

    // Set in Initialize()
    //
    private ImGuiRenderer _imGuiRenderer = null!;
    private SaveStateInfo _primarySaveStateInfo = null!;
    private SaveStateInfo _secondarySaveStateInfo = null!;
    private ToastInfo _primaryToastInfo = null!;
    private ToastInfo _secondaryToastInfo = null!;

    private nint _rawDreamcastConnectedIconTexture;
    private nint _rawVmusConnectedIconTexture;
    private GCHandle _iniFilenameHandle;
    // ---

    private MappingEditState _mappingEditState;

    private readonly string _displayVersion;
    private readonly string _commitId;

    // Debugger UI state

    private static readonly uint Debug_ColorPc = ImGui.GetColorU32(new Numerics.Vector4(99.0f/255, 92.0f/255, 31.0f/255, 1));
    private static readonly uint Debug_ColorStack = ImGui.GetColorU32(new Numerics.Vector4(53.0f/255, 50.0f/255, 18.0f/255, 1));

    internal bool Debugger_Show = false;
    private int _debugger_ScrollToInstructionIndex = -1;

    internal PendingCommand PendingCommand { get; private set; }

    public UserInterface(Game1 game)
    {
        _game = game;
        var versionAttribute = typeof(UserInterface).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (versionAttribute is null)
        {
            _displayVersion = "?";
            _commitId = "?";
        }
        else
        {
            var informationalVersion = versionAttribute.InformationalVersion;
            var plusIndex = informationalVersion.IndexOf('+');
            _displayVersion = plusIndex == -1 ? informationalVersion : informationalVersion[..plusIndex];

            var commitStartIndex = plusIndex + 1;
            const int commitMaxDisplayLength = 7;
            var commitLength = Math.Min(commitMaxDisplayLength, informationalVersion.Length - commitStartIndex);
            _commitId = plusIndex == -1 ? "?" : informationalVersion.Substring(commitStartIndex, length: commitLength);
        }
    }

    [MemberNotNull(nameof(_imGuiRenderer), nameof(_primarySaveStateInfo), nameof(_secondarySaveStateInfo), nameof(_primaryToastInfo), nameof(_secondaryToastInfo))]
    internal void Initialize(Texture2D dreamcastConnectedIconTexture, Texture2D vmusConnectedIconTexture)
    {
        _imGuiRenderer = new ImGuiRenderer(_game);
        _imGuiRenderer.RebuildFontAtlas();

        unsafe
        {
            // Note: if we supported tearing down the UserInterface then we would want to take care to dispose of the handle.
            var iniFilename = Encoding.UTF8.GetBytes($"{AppContext.BaseDirectory}/Data/imgui.ini\0");
            _iniFilenameHandle = GCHandle.Alloc(iniFilename, GCHandleType.Pinned);
            fixed (byte* ptr = iniFilename)
            {
                // Using the ptr where it will outlive the fixed block is only fine because we hold a GCHandle with type Pinned for it.
                ImGui.GetIO().NativePtr->IniFilename = ptr;
            }
        }

        _rawDreamcastConnectedIconTexture = _imGuiRenderer.BindTexture(dreamcastConnectedIconTexture);
        _rawVmusConnectedIconTexture = _imGuiRenderer.BindTexture(vmusConnectedIconTexture);

        var graphicsDevice = _game.GraphicsDevice;
        _primarySaveStateInfo = new SaveStateInfo(graphicsDevice, _imGuiRenderer);
        _secondarySaveStateInfo = new SaveStateInfo(graphicsDevice, _imGuiRenderer);
        _primaryToastInfo = new ToastInfo(graphicsDevice, _imGuiRenderer);
        _secondaryToastInfo = new ToastInfo(graphicsDevice, _imGuiRenderer);
    }

    internal void Layout(GameTime gameTime)
    {
        // Call BeforeLayout first to set things up
        _imGuiRenderer.BeforeLayout(gameTime);

        // Draw our UI
        LayoutImpl();

        // Call AfterLayout now to finish up and draw all the things
        _imGuiRenderer.AfterLayout();
    }

    /// <summary>Show a modal asking user to confirm a "destructive" command</summary>
    internal void ShowConfirmCommandDialog(PendingCommandKind commandKind, VmuPresenter? vmuPresenter, string? filePath = null)
    {
        Pause();
        PendingCommand = PendingCommand.ShowDialog(commandKind, vmuPresenter, filePath);
    }

    private void Pause()
        => _game.UIPaused = true;

    internal void NewVmu(VmuPresenter presenter)
    {
        var vmu = presenter.Vmu;
        if (vmu.HasUnsavedChanges && PendingCommand is not { Kind: PendingCommandKind.NewVmu, State: ConfirmationState.Confirmed })
        {
            ShowConfirmCommandDialog(PendingCommandKind.NewVmu, presenter);
            return;
        }

        _game.LoadNewVmu(presenter);
    }

    internal void OpenRecentFile(VmuPresenter presenter, string filePath)
    {
        var vmu = presenter.Vmu;
        if (vmu.HasUnsavedChanges && PendingCommand is not { Kind: PendingCommandKind.OpenRecent, State: ConfirmationState.Confirmed })
        {
            ShowConfirmCommandDialog(PendingCommandKind.OpenRecent, presenter);
            return;
        }

        _game.LoadAndStartVmsOrVmuFile(presenter, filePath);
    }

    internal void OpenVmuDialog(VmuPresenter presenter)
    {
        var vmu = presenter.Vmu;
        if (vmu.HasUnsavedChanges && PendingCommand is not { Kind: PendingCommandKind.OpenVmu, State: ConfirmationState.Confirmed })
        {
            ShowConfirmCommandDialog(PendingCommandKind.OpenVmu, presenter);
            return;
        }

        var result = Dialog.FileOpen("vmu,bin,vms", defaultPath: null);
        if (result.IsOk)
        {
            _game.LoadAndStartVmsOrVmuFile(presenter, result.Path);
        }
    }

    internal void Reset(VmuPresenter presenter)
    {
        if (PendingCommand is not { Kind: PendingCommandKind.Reset, State: ConfirmationState.Confirmed })
        {
            ShowConfirmCommandDialog(PendingCommandKind.Reset, presenter);
            return;
        }

        presenter.Reset();
    }

    private void OpenPopupAndPause(string name)
    {
        Pause();
        ImGui.OpenPopup(name);
    }

    internal void ShowToast(VmuPresenter presenter, string message, int durationFrames = ToastInfo.DefaultDurationFrames)
    {
        var toastInfo = presenter == _game.PrimaryVmuPresenter ? _primaryToastInfo : _secondaryToastInfo;
        toastInfo.Message = message;
        toastInfo.DisplayFrames = durationFrames;
        toastInfo.ShowImage = false;
    }

    internal void ShowScreenshotToast(VmuPresenter presenter, string message, int durationFrames = ToastInfo.DefaultDurationFrames)
    {
        var toastInfo = presenter == _game.PrimaryVmuPresenter ? _primaryToastInfo : _secondaryToastInfo;
        toastInfo.Message = message;
        toastInfo.DisplayFrames = durationFrames;
        presenter.UpdateScreenTexture(toastInfo.ImageTexture);
        toastInfo.ShowImage = true;
    }

    private void Unpause()
        => _game.UIPaused = false;

    private void ClosePopupAndUnpause()
    {
        Unpause();
        ImGui.CloseCurrentPopup();
    }

    private void LayoutImpl()
    {
        LayoutPrimaryMenuBar();
        LayoutSecondaryMenuBar();

        LayoutSettings();
        LayoutDebugger();

        LayoutKeyMapping();
        LayoutEditKey();

        LayoutButtonMapping();
        LayoutEditButton();

        LayoutPendingCommandDialog();

        LayoutFastForwardOrPauseIndicators();
        LayoutToast();
    }


    private void LayoutFastForwardOrPauseIndicators()
    {
        layoutOneIndicator(_game.PrimaryVmuPresenter, "PrimaryFastForwardOrPauseIndicator");
        if (_game.SecondaryVmuPresenter is { } secondaryPresenter)
            layoutOneIndicator(secondaryPresenter, "SecondaryFastForwardOrPauseIndicator");

        static void layoutOneIndicator(VmuPresenter presenter, string windowName)
        {
            var message = presenter.EffectivePaused ? "||"
                : presenter.EffectiveFastForwarding ? ">>"
                : null;
            if (message is null)
                return;

            var rectangle = presenter.Bounds;
            var textSize = ImGui.CalcTextSize(message, wrapWidth: rectangle.Width);
            ImGui.SetNextWindowPos(new Numerics.Vector2(x: rectangle.X + 2, y: rectangle.Y + rectangle.Height - textSize.Y - Game1.MenuBarHeight));
            ImGui.SetNextWindowSize(textSize + new Numerics.Vector2(10, 20));
            if (ImGui.Begin(windowName, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar))
            {
                ImGui.TextWrapped(message);
            }

            ImGui.End();
        }
    }

    private void LayoutToast()
    {
        layoutOne("PrimaryToast", _game.PrimaryVmuPresenter, _primaryToastInfo);
        if (_game.UseSecondaryVmu)
            layoutOne("SecondaryToast", _game.SecondaryVmuPresenter, _secondaryToastInfo);

        void layoutOne(string id, VmuPresenter presenter, ToastInfo toastInfo)
        {
            if (toastInfo.DisplayFrames == 0)
                return;

            if (toastInfo.Message is null)
                throw new InvalidOperationException();

            toastInfo.DisplayFrames--;

            var doFadeout = toastInfo.DisplayFrames < ToastInfo.BeginFadeoutFrames;
            if (doFadeout)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ((float)toastInfo.DisplayFrames) / ToastInfo.BeginFadeoutFrames * 0.8f);
            else
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.8f);

            const int padding = 20;
            var bounds = presenter.Bounds;
            var maxTextWidth = toastInfo.ShowImage
                ? bounds.Width - Display.ScreenWidth - 2 * padding
                : bounds.Width - padding;
            var textSize = ImGui.CalcTextSize(text: toastInfo.Message, wrapWidth: maxTextWidth);
            var toastWidth = Display.ScreenWidth + textSize.X + 2 * padding;
            var toastHeight = Math.Max(Display.ScreenHeight, textSize.Y) + padding;
            ImGui.SetNextWindowPos(new Numerics.Vector2(x: 2, y: bounds.Bottom - toastHeight - 2));
            ImGui.SetNextWindowSize(new Numerics.Vector2(x: toastWidth, y: toastHeight));
            if (ImGui.Begin(id, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar))
            {
                if (!doFadeout)
                    ImGui.PopStyleVar();

                if (toastInfo.ShowImage)
                {
                    var imageSize = new Numerics.Vector2(x: Display.ScreenWidth, y: Display.ScreenHeight);
                    ImGui.Image(toastInfo.RawImageTexture, imageSize);
                    ImGui.SameLine();
                }
                ImGui.TextWrapped(toastInfo.Message.Replace("%", "%%"));
            }

            ImGui.End();

            if (doFadeout)
                ImGui.PopStyleVar();
        }
    }

    private void LayoutPrimaryMenuBar()
    {
        var presenter = _game.PrimaryVmuPresenter;
        var rectangle = _game.PrimaryMenuBarRectangle;
        ImGui.SetNextWindowPos(new Numerics.Vector2(0, 0));
        ImGui.SetNextWindowSize(new Numerics.Vector2(rectangle.Width, rectangle.Height));
        ImGui.Begin("PrimaryMenuWindow", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus);
        ImGui.BeginMenuBar();
        if (ImGui.BeginMenu("File"))
        {
            LayoutNewOpenSaveMenuItems(presenter);
            ImGui.Separator();
            if (ImGui.MenuItem(_game.UseSecondaryVmu ? "Close Slot 2 VMU" : "Open Slot 2 VMU"))
            {
                _game.Configuration_ExpansionSlotsChanged(
                    _game.UseSecondaryVmu ? ExpansionSlots.Slot1 : ExpansionSlots.Slot1And2);
            }

            ImGui.Separator();
            if (ImGui.MenuItem("Quit"))
                _game.Exit();

            ImGui.EndMenu();
        }

        LayoutEmulationMenu(presenter);

        bool doOpenSettings = false;
        if (ImGui.BeginMenu("Settings"))
        {
            if (ImGui.MenuItem("General"))
            {
                // Workaround to delay calling OpenPopup: https://github.com/ocornut/imgui/issues/331#issuecomment-751372071
                doOpenSettings = true;
            }

            if (ImGui.MenuItem("Keyboard Config"))
            {
                Pause();
                _mappingEditState = MappingEditState.EditKeyMappings(_game.Configuration.PrimaryInput.KeyMappings, _game.PrimaryVmu);
            }

            if (ImGui.MenuItem("Gamepad Config"))
            {
                Pause();
                var input = _game.Configuration.PrimaryInput;
                _mappingEditState = MappingEditState.EditButtonMappings(input.ButtonMappings, input.GamePadIndex, _game.PrimaryVmu);
            }

            if (ImGui.MenuItem("Open Data Folder"))
            {
                new Process()
                {
                    StartInfo = new ProcessStartInfo(Vmu.DataFolder)
                    {
                        UseShellExecute = true,
                    }
                }.Start();
            }

            if (ImGui.BeginMenu("Set Window Size"))
            {
                var currentMultiple = _game.GetWindowSizeMultiple();

                if (ImGui.MenuItem("3x (144x96)", enabled: currentMultiple != 1))
                    _game.SetWindowSizeMultiple(1);

                if (ImGui.MenuItem("6x (288x192)", enabled: currentMultiple != 2))
                    _game.SetWindowSizeMultiple(2);

                if (ImGui.MenuItem("9x (432x288)", enabled: currentMultiple != 3))
                    _game.SetWindowSizeMultiple(3);

                if (ImGui.MenuItem("12x (576x384)", enabled: currentMultiple != 4))
                    _game.SetWindowSizeMultiple(4);

                ImGui.EndMenu();
            }

            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("About"))
        {
            ImGui.Text($"Version: {_displayVersion}");
            ImGui.Text($"Commit: {_commitId}");
            ImGui.Separator();

            if (ImGui.MenuItem("Copy to clipboard"))
                ImGui.SetClipboardText($"{_displayVersion} ({_commitId})");

            ImGui.EndMenu();
        }

        if (_game.PrimaryVmu.IsServerConnected)
        {
            ImGui.Image(_rawDreamcastConnectedIconTexture, new Numerics.Vector2(18));
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Dreamcast connected over TCP");
                ImGui.EndTooltip();
            }
        }

        LayoutVmusConnectedIcon();
        ImGui.EndMenuBar();
        ImGui.End();

        if (doOpenSettings)
            OpenPopupAndPause("Settings");
    }

    private void LayoutEmulationMenu(VmuPresenter presenter)
    {
        var vmu = presenter.Vmu;
        if (ImGui.BeginMenu("Emulation"))
        {
            var isDockedToDreamcast = vmu.IsDockedToDreamcast;
            var isOtherVmuConnected = vmu.IsOtherVmuConnected;
            using (new DisabledScope(disabled: isDockedToDreamcast))
            {
                if (ImGui.MenuItem(presenter.EffectivePaused ? "Resume" : "Pause"))
                {
                    if (isOtherVmuConnected)
                    {
                        Debug.Assert(_game.SecondaryVmuPresenter is not null);
                        Debug.Assert(_game.PrimaryVmuPresenter.LocalPaused == _game.SecondaryVmuPresenter.LocalPaused);
                        _game.PrimaryVmuPresenter.ToggleLocalPause();
                        _game.SecondaryVmuPresenter.ToggleLocalPause();
                    }
                    else
                    {
                        presenter.ToggleLocalPause();
                    }
                }
            }

            using (new DisabledScope(disabled: isDockedToDreamcast || isOtherVmuConnected))
            {
                if (ImGui.MenuItem("Reset"))
                    Reset(presenter);
            }

            ImGui.Separator();
            if (ImGui.MenuItem(isDockedToDreamcast ? "Eject from Dreamcast" : "Dock to Dreamcast"))
                presenter.DockOrEject();

            using (new DisabledScope(disabled: !_game.UseSecondaryVmu))
            {
                Debug.Assert(!vmu.IsOtherVmuConnected || !vmu.IsDockedToDreamcast);
                Debug.Assert((_game.SecondaryVmu is null && !_game.PrimaryVmu.IsOtherVmuConnected)
                    || _game.PrimaryVmu.IsOtherVmuConnected == _game.SecondaryVmu?.IsOtherVmuConnected);

                if (ImGui.MenuItem(vmu.IsOtherVmuConnected ? "Disconnect VMU-to-VMU" : "Connect VMU-to-VMU"))
                {
                    Debug.Assert(_game.UseSecondaryVmu);
                    _game.PrimaryVmu.ConnectOrDisconnectVmu(_game.SecondaryVmu);
                    _game.PrimaryVmuPresenter.LocalPaused = false;
                    _game.SecondaryVmuPresenter.LocalPaused = false;
                }
            }

            ImGui.Separator();
            using (new DisabledScope(vmu.LoadedFilePath is null || vmu.IsOtherVmuConnected))
            {
                if (ImGui.MenuItem("Save State"))
                    SaveStateWithThumbnail(presenter);

                var stateInfo = vmu == _game.PrimaryVmu ? _primarySaveStateInfo : _secondarySaveStateInfo;
                UpdateThumbnail(vmu, stateInfo);
                if (ImGui.MenuItem("Load State", enabled: stateInfo.Exists))
                {
                    if (vmu.LoadStateById(id: _game.Configuration.CurrentSaveStateSlot.ToString(), saveOopsFile: true) is (false, var error))
                    {
                        ShowToast(presenter, error ?? $"An unknown error occurred in '{nameof(Vmu.LoadStateById)}'.");
                    }
                }

                if (ImGui.MenuItem("Undo Load State"))
                {
                    vmu.LoadOopsFile();
                }

                {
                    // Select Save Slot
                    ImGui.BeginGroup();
                    ImGui.Image(stateInfo.RawThumbnailTexture, image_size: new Numerics.Vector2(Display.ScreenWidth, Display.ScreenHeight));

                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 1); // make the prev/next buttons align nicely with the thumbnail.
                    if (ImGui.ArrowButton(str_id: "PrevSaveState", dir: ImGuiDir.Left))
                    {
                        var newSlot = BitHelpers.ModPositive(_game.Configuration.CurrentSaveStateSlot - 1, 10);
                        _game.Configuration_CurrentSaveStateSlotChanged(newSlot);
                    }

                    ImGui.SameLine();
                    if (ImGui.ArrowButton(str_id: "NextSaveState", dir: ImGuiDir.Right))
                    {
                        var newSlot = BitHelpers.ModPositive(_game.Configuration.CurrentSaveStateSlot + 1, 10);
                        _game.Configuration_CurrentSaveStateSlotChanged(newSlot);
                    }
                    ImGui.EndGroup();

                    ImGui.SameLine();
                    ImGui.BeginGroup();
                    ImGui.Text($"Slot {_game.Configuration.CurrentSaveStateSlot + 1}");
                    // Note: this 13 is kind of a magic number. It would need to be adjusted if the names of other menu items changed.
                    ImGui.Text(BreakLines(stateInfo.StateTimeDescription, maxLineLength: 13));
                    ImGui.EndGroup();
                }
            }

            ImGui.Separator();
            if (ImGui.MenuItem("Take Screenshot"))
                presenter.TakeScreenshot();

            if (ImGui.MenuItem(Debugger_Show ? "Close Debugger" : "Open Debugger"))
            {
                Debugger_Show = !Debugger_Show;
                if (Debugger_Show)
                    _game.InitializeDebugInfo();

                _game.UpdateScaleMatrix();
            }

            ImGui.EndMenu();
        }
    }

    internal void SaveStateWithThumbnail(VmuPresenter presenter)
    {
        presenter.Vmu.SaveState(_game.Configuration.CurrentSaveStateSlot.ToString());
        var stateInfo = presenter == _game.PrimaryVmuPresenter ? _primarySaveStateInfo : _secondarySaveStateInfo;
        stateInfo.InvalidateThumbnail();
        ShowScreenshotToast(presenter, $"Saved state to slot {_game.Configuration.CurrentSaveStateSlot + 1}", durationFrames: 2 * 60);
    }

    private void UpdateThumbnail(Vmu vmu, SaveStateInfo stateInfo)
    {
        var filePath = vmu.LoadedFilePath is null ? null : Vmu.GetSaveStatePath(vmu.LoadedFilePath, id: _game.Configuration.CurrentSaveStateSlot.ToString());

        // thumbnail up to date
        if (stateInfo.StateFilePath == filePath)
            return;

        var thumbnailData = getScreenData();
        stateInfo.ThumbnailTexture.SetData(thumbnailData);
        stateInfo.StateFilePath = filePath;
        (stateInfo.Exists, stateInfo.StateTimeDescription) = File.Exists(filePath) ? (true, File.GetLastWriteTime(filePath).Humanize()) : (false, "");

        Color[] getScreenData()
        {
            var vmuScreenData = new Color[Display.ScreenWidth * Display.ScreenHeight];
            Array.Fill(vmuScreenData, _game.ColorPalette.Screen0);
            if (filePath is null || !File.Exists(filePath))
                return vmuScreenData;

            try
            {
                using var stateFile = File.OpenRead(filePath);
                using var zip = new ZipArchive(stateFile);

                if (zip.GetEntry(Vmu.SaveState_ThumbnailFile) is not { } thumbnailEntry)
                    return vmuScreenData;

                var vmuBytes = new byte[Display.ScreenWidth * Display.ScreenHeight / 8];
                using var thumbnailStream = thumbnailEntry.Open();
                thumbnailStream.ReadExactly(vmuBytes);
                VmuPresenter.UpdateScreenData(vmuScreenData, vmuBytes, _game.ColorPalette);
                return vmuScreenData;
            }
            catch (InvalidDataException)
            {
                return vmuScreenData;
            }
        }
    }

    private void LayoutSecondaryMenuBar()
    {
        if (_game.SecondaryVmuPresenter is not { } presenter)
            return;

        var vmu = presenter.Vmu;
        var rectangle = _game.SecondaryMenuBarRectangle;
        ImGui.SetNextWindowPos(new Numerics.Vector2(rectangle.X, rectangle.Y));
        ImGui.SetNextWindowSize(new Numerics.Vector2(rectangle.Width, rectangle.Height));
        ImGui.Begin("SecondaryMenuWindow", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus);
        ImGui.BeginMenuBar();
        if (ImGui.BeginMenu(vmu.HasUnsavedChanges ? "* File" : "File"))
        {
            if (vmu.LoadedFilePath is not null)
            {
                ImGui.TextUnformatted(Path.GetFileName(vmu.LoadedFilePath.AsSpan()));
                ImGui.Separator();
            }

            LayoutNewOpenSaveMenuItems(presenter);
            ImGui.Separator();
            if (ImGui.MenuItem("Close Slot 2 VMU"))
                _game.Configuration_ExpansionSlotsChanged(ExpansionSlots.Slot1);

            ImGui.EndMenu();
        }

        LayoutEmulationMenu(presenter);

        if (ImGui.BeginMenu("Settings"))
        {
            var configuration = _game.Configuration;
            var muteSecondaryVmuAudio = configuration.MuteSecondaryVmuAudio;
            if (Checkbox("Mute Audio", ref muteSecondaryVmuAudio, paddingY: 0))
                _game.Configuration_MuteSecondaryVmuAudioChanged(muteSecondaryVmuAudio);

            ImGui.Separator();
            if (ImGui.MenuItem("Keyboard Config"))
            {
                Pause();
                _mappingEditState = MappingEditState.EditKeyMappings(_game.Configuration.SecondaryInput.KeyMappings, vmu);
            }

            if (ImGui.MenuItem("Gamepad Config"))
            {
                Pause();
                var input = _game.Configuration.SecondaryInput;
                _mappingEditState = MappingEditState.EditButtonMappings(input.ButtonMappings, input.GamePadIndex, vmu);
            }

            ImGui.EndMenu();
        }

        ImGui.EndMenuBar();
        ImGui.End();
    }

    private void LayoutVmusConnectedIcon()
    {
        if (_game.PrimaryVmu.IsOtherVmuConnected)
        {
            Debug.Assert(_game.SecondaryVmu!.IsOtherVmuConnected);
            ImGui.Image(_rawVmusConnectedIconTexture, new Numerics.Vector2(x: 36, y: 18));
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("VMU-to-VMU connection active");
                ImGui.EndTooltip();
            }
        }
    }

    private bool Checkbox(string label, ref bool v, float paddingY)
    {
        ImGui.PushStyleVarY(ImGuiStyleVar.FramePadding, paddingY);
        var changed = ImGui.Checkbox(label, ref v);
        ImGui.PopStyleVar();
        return changed;
    }

    private void LayoutNewOpenSaveMenuItems(VmuPresenter presenter)
    {
        if (ImGui.MenuItem("New VMU"))
            NewVmu(presenter);

        if (ImGui.MenuItem("Open"))
            OpenVmuDialog(presenter);

        var (displayFileNames, recentFilePaths) = calcRecentFilesInfo();
        if (ImGui.BeginMenu("Open Recent", enabled: recentFilePaths.Any()))
        {
            for (int i = 0; i < recentFilePaths.Length; i++)
            {
                var recentFilePath = recentFilePaths[i];
                if (recentFilePath is null)
                    continue;

                var fileName = displayFileNames[i];
                if (ImGui.MenuItem(fileName))
                    OpenRecentFile(presenter, recentFilePath);
            }

            ImGui.EndMenu();
        }

        if (ImGui.MenuItem("Save As"))
        {
            var result = Dialog.FileSave(filterList: "vmu,bin", defaultPath: null);
            if (result.IsOk)
            {
                _game.SaveVmuFileAs(presenter.Vmu, result.Path);
            }
        }

        (string[] displayFileNames, ImmutableArray<string> recentFiles) calcRecentFilesInfo()
        {
            var recentFiles = _game.RecentFilesInfo.RecentFiles;
            const int maxFileNameLength = 18;
            string[] displayFileNames = new string[recentFiles.Length];
            for (var i = 0; i < recentFiles.Length; i++)
            {
                var fileName = Path.GetFileName(recentFiles[i]);
                var oversize = fileName.Length > maxFileNameLength;
                displayFileNames[i] = oversize ? BreakLines(fileName, maxFileNameLength) : fileName;
            }

            return (displayFileNames, recentFiles);
        }
    }

    private static string BreakLines(ReadOnlySpan<char> span, int maxLineLength)
    {
        var builder = new StringBuilder();
        var i = 0;
        var previousMatchIndex = 0;
        var pattern = BreakLinesPattern();
        foreach (ValueMatch match in pattern.EnumerateMatches(span))
        {
            if (match.Index > i + maxLineLength)
            {
                // chunk must be non-empty
                var endIndex = i == previousMatchIndex ? match.Index : previousMatchIndex;
                builder.Append(span[i..endIndex]);
                if (i != endIndex)
                    i = endIndex;

                if (i != span.Length)
                    builder.AppendLine();
            }

            previousMatchIndex = match.Index;
        }

        if (i != span.Length)
            builder.Append(span[i..]);

        return builder.ToString();
    }

    private static readonly string[] AllDreamcastSlotNames = ["A", "B", "C", "D"];

    private void LayoutSettings()
    {
        if (ImGui.BeginPopupModal("Settings"))
        {
            if (ImGui.Button("Done"))
            {
                _game.Configuration_DoneEditing();
                ClosePopupAndUnpause();
            }

            var configuration = _game.Configuration;
            var autoInitializeDate = configuration.AutoInitializeDate;
            if (Checkbox("Auto-initialize date on startup", ref autoInitializeDate, paddingY: 1))
                _game.Configuration_AutoInitializeDateChanged(autoInitializeDate);

            var anyButtonWakesFromSleep = configuration.AnyButtonWakesFromSleep;
            if (Checkbox("Any button wakes from sleep", ref anyButtonWakesFromSleep, paddingY: 1))
                _game.Configuration_AnyButtonWakesFromSleepChanged(anyButtonWakesFromSleep);

            var preserveAspectRatio = configuration.PreserveAspectRatio;
            if (Checkbox("Preserve aspect ratio", ref preserveAspectRatio, paddingY: 1))
                _game.Configuration_PreserveAspectRatioChanged(preserveAspectRatio);

            var autoDockEject = configuration.AutoDockEject;
            if (Checkbox("Dock/eject when Flycast connected", ref autoDockEject, paddingY: 1))
                _game.Configuration_AutoDockEjectChanged(autoDockEject);

            // Volume
            ImGui.Text("Volume");
            ImGui.SameLine();

            var sliderVolume = configuration.Volume;
            ImGui.PushID("VolumeSlider");
            ImGui.SliderInt(label: "", ref sliderVolume, v_min: Audio.MinVolume, v_max: Audio.MaxVolume);
            ImGui.PopID();
            if (configuration.Volume != sliderVolume)
                _game.Configuration_VolumeChanged(sliderVolume);

            // Color Palette
            {
                ImGui.Text("Color Palette");
                ImGui.SameLine();

                var palette = configuration.ColorPaletteName;
                var paletteIndex = Array.IndexOf(ColorPalette.AllPaletteNames, palette);
                var selectedIndex = paletteIndex == -1 ? 0 : paletteIndex;
                ImGui.SetNextItemWidth(CalcComboWidth(ColorPalette.AllPaletteNames[1]));
                ImGui.PushID("ColorPaletteCombo");
                ImGui.Combo(label: "", ref selectedIndex, items: ColorPalette.AllPaletteNames, items_count: ColorPalette.AllPaletteNames.Length);
                ImGui.PopID();
                if (paletteIndex != selectedIndex)
                {
                    _game.Configuration_PaletteChanged(ColorPalette.AllPalettes[selectedIndex]);
                    _primarySaveStateInfo.InvalidateThumbnail();
                    _secondarySaveStateInfo.InvalidateThumbnail();
                }
            }

            // Dreamcast Port
            {
                ImGui.Text("Dreamcast controller port");
                ImGui.SameLine();

                var port = configuration.DreamcastPort;
                var selectedIndex = (int)port;
                ImGui.SetNextItemWidth(CalcComboWidth(longestItem: AllDreamcastSlotNames[0]));
                ImGui.PushID("DreamcastPortCombo");
                ImGui.Combo(label: "", ref selectedIndex, items: AllDreamcastSlotNames, items_count: AllDreamcastSlotNames.Length);
                ImGui.PopID();
                if ((int)port != selectedIndex)
                    _game.Configuration_DreamcastPortChanged((DreamcastPort)selectedIndex);
            }

            ImGui.EndPopup();
        }

        static float CalcComboWidth(string longestItem)
        {
            // text size + padding + arrow button size
            return ImGui.CalcTextSize(longestItem).X + ImGui.GetStyle().FramePadding.X * 2.0f + ImGui.GetFrameHeight();
        }
    }

    private void LayoutDebugger()
    {
        var localDebuggerShow = Debugger_Show;
        if (!localDebuggerShow)
            return;

        var debugInfo = _game.PrimaryVmu.LazyDebugInfo;
        Debug.Assert(debugInfo is not null);

        if (ImGui.Begin("Debugger", ref Debugger_Show, ImGuiWindowFlags.NoScrollbar))
        {
            if (ImGui.BeginTabBar("InstructionBanks"))
            {
                // Note: We need to drop down into unsafe code, to pass `p_open: null`, and also pass `flags`.
                // See https://github.com/ImGuiNET/ImGui.NET/issues/135
                unsafe
                {
                    fixed (byte* label = "ROM"u8)
                    {
                        var flags = _debugger_ScrollToInstructionIndex != -1 && _game.PrimaryVmu._cpu.CurrentInstructionBankId == InstructionBank.ROM ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
                        if (ImGuiNative.igBeginTabItem(label, p_open: null, flags) != 0)
                        {
                            layoutTab(InstructionBank.ROM);
                            ImGui.EndTabItem();
                        }
                    }

                    fixed (byte* label = "FlashBank0"u8)
                    {
                        var flags = _debugger_ScrollToInstructionIndex != -1 && _game.PrimaryVmu._cpu.CurrentInstructionBankId == InstructionBank.FlashBank0 ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
                        if (ImGuiNative.igBeginTabItem(label, p_open: null, flags) != 0)
                        {
                            layoutTab(InstructionBank.FlashBank0);
                            ImGui.EndTabItem();
                        }
                    }
                }

                ImGui.EndTabBar();
            }
        }

        if (localDebuggerShow != Debugger_Show)
            _game.UpdateScaleMatrix();

        // Note: End() is called even when Begin() returned false to handle collapsed state
        ImGui.End();


        void layoutTab(InstructionBank bankId)
        {
            if (ImGui.BeginTable(bankId.ToString(), columns: 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Disassembly", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Tools");
                ImGui.TableHeadersRow();

                ImGui.TableNextColumn();
                layoutDisasm(bankId);

                ImGui.TableNextColumn();
                layoutControls();
                ImGui.Separator();
                layoutBreakpoints(bankId);
                layoutStack();

                ImGui.EndTable();
            }
        }

        void layoutDisasm(InstructionBank bankId)
        {
            if (ImGui.BeginTable("disasm", columns: 3, flags: ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.ScrollY))
            {
                ImGui.TableSetupColumn("breakpoints", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("addresses", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("instructions");

                var cpu = _game.PrimaryVmu._cpu;
                var executingInThisBank = bankId == cpu.CurrentInstructionBankId;
                var bankInfo = debugInfo.GetBankInfo(bankId);
                var disasm = bankInfo.Instructions;

                // Render only the visible list items
                var clipperData = new ImGuiListClipper();
                ImGuiListClipperPtr clipper;
                unsafe
                {
                    clipper = new ImGuiListClipperPtr(&clipperData);
                }

                clipper.Begin(disasm.Count);

                bool doScrollToInstruction = _debugger_ScrollToInstructionIndex != -1 && executingInThisBank;
                if (doScrollToInstruction)
                    clipper.IncludeItemByIndex(_debugger_ScrollToInstructionIndex);

                while (clipper.Step())
                {
                    for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        ImGui.PushID(i);
                        ImGui.TableNextColumn();
                        var inst = disasm[i];

                        // Set background color
                        if (executingInThisBank && _game.PrimaryVmu._cpu.ProgramCounter == inst.Offset)
                        {
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, Debug_ColorPc);
                        }
                        else
                        {
                            foreach (var entry in _game.PrimaryVmu._cpu.StackData)
                            {
                                if (entry.Kind == StackValueKind.Push)
                                    continue;

                                var callAddr = entry.Source;
                                if (inst.Offset == callAddr)
                                {
                                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, Debug_ColorStack);
                                    break;
                                }
                            }
                        }

                        ImGui.PushID("breakpoint");

                        var bpIndex = bankInfo.Breakpoints.FindIndex(bp => bp.Offset == inst.Offset);
                        var breakpointExists = bpIndex != -1;
                        if (ImGui.Checkbox("", ref breakpointExists))
                        {
                            if (breakpointExists) // Create new
                            {
                                bankInfo.Breakpoints.Add(new BreakpointInfo { Enabled = true, Offset = inst.Offset });
                            }
                            else if (bpIndex != -1) // Remove
                            {
                                bankInfo.Breakpoints.RemoveAt(bpIndex);
                            }
                        }

                        ImGui.PopID();

                        ImGui.TableNextColumn();
                        ImGui.Text(inst.Offset.ToString("X4"));

                        ImGui.TableNextColumn();
                        ImGui.Text(inst.DisplayInstruction());
                        ImGui.PopID();

                        if (doScrollToInstruction && i == _debugger_ScrollToInstructionIndex)
                        {
                            ImGui.SetScrollHereY();
                            _debugger_ScrollToInstructionIndex = -1;
                        }

                        if (ImGui.IsItemHovered())
                        {
                            var argumentValues = inst.DisplayArgumentValues(_game.PrimaryVmu._cpu);
                            if (argumentValues.Length != 0 && ImGui.BeginTooltip())
                            {
                                ImGui.Text(inst.DisplayArgumentValues(_game.PrimaryVmu._cpu));
                                ImGui.EndTooltip();
                            }
                        }
                    }
                }

                ImGui.EndTable();
            }
        }

        void layoutControls()
        {
            var bankInfo = debugInfo.CurrentBankInfo;
            var breakState = debugInfo.DebuggingState == DebuggingState.Break;
            if (ImGui.Checkbox("Break", ref breakState))
                debugInfo.ToggleDebugBreak();

            if (ImGui.Button("Step In"))
                debugInfo.StepIn();

            if (ImGui.Button("Step Out"))
                debugInfo.StepOut();
        }

        void layoutBreakpoints(InstructionBank bankId)
        {
            ImGui.Text("Breakpoints");
            ImGui.Separator();
            if (ImGui.BeginTable("Breakpoints", columns: 3, flags: ImGuiTableFlags.BordersInnerV))
            {
                ImGui.TableSetupColumn("breakpoints", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("addresses", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("symbols", ImGuiTableColumnFlags.WidthStretch);

                var bankInfo = debugInfo.GetBankInfo(bankId);
                var breakpoints = bankInfo.Breakpoints;
                for (var i = 0; i < breakpoints.Count; i++)
                {
                    ImGui.PushID(i);
                    ImGui.TableNextColumn();

                    bool enabled = breakpoints[i].Enabled;
                    if (ImGui.Checkbox("", ref enabled))
                        breakpoints[i].Enabled = enabled;

                    ImGui.TableNextColumn();
                    ImGui.Text(breakpoints[i].Offset.ToString("X4"));

                    // If you place a breakpoint at an offset,
                    // it means you think the offset has executable code in it
                    var inst = bankInfo.GetOrLoadInstruction(breakpoints[i].Offset);
                    ImGui.TableNextColumn();
                    ImGui.Text(inst.DisplayInstruction());
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }

        void layoutStack()
        {
            ImGui.Text("Stack");
            ImGui.Separator();
            if (ImGui.BeginTable("stack", columns: 3, flags: ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.ScrollY))
            {
                ImGui.TableSetupColumn("breakpoints", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("addresses", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("instructions");

                var cpu = _game.PrimaryVmu._cpu;
                var stackData = cpu.StackData;
                // always show a stack entry for "where we are right now"
                layoutStackEntry(
                    stackData.Count,
                    new StackEntry(
                        StackValueKind.CallReturn,
                        Source: cpu.ProgramCounter,
                        Value: 0,
                        Offset: 0,
                        cpu.CurrentInstructionBankId));
                for (var i = stackData.Count - 1; i >= 0; i--)
                    layoutStackEntry(i, stackData[i]);

                ImGui.EndTable();
            }
            
            void layoutStackEntry(int i, StackEntry entry)
            {
                if (entry.Kind == StackValueKind.Push)
                    return; // TODO2: display these

                ImGui.PushID(i);
                ImGui.TableNextColumn();

                var bankInfo = debugInfo.GetBankInfo(entry.BankId);
                var callAddr = entry.Source;
                var bpIndex = bankInfo.Breakpoints.FindIndex(bp => bp.Offset == callAddr);
                var breakpointExists = bpIndex != -1;

                ImGui.PushID("breakpoint");
                if (ImGui.Checkbox("", ref breakpointExists))
                {
                    if (breakpointExists) // Create new
                    {
                        bankInfo.Breakpoints.Add(new BreakpointInfo { Enabled = true, Offset = callAddr });
                    }
                    else if (bpIndex != -1) // Remove
                    {
                        bankInfo.Breakpoints.RemoveAt(bpIndex);
                    }
                }
                ImGui.PopID();

                ImGui.TableNextColumn();

                var index = bankInfo.BinarySearchInstructions(callAddr);
                if (index < 0)
                {
                    // If we got here, it means the code we were returning to,
                    // was overwritten in flash, while we were still going to return to it.
                    ImGui.Text("ERROR");
                    ImGui.TableNextColumn();
                    ImGui.Text("");
                }
                else
                {
                    var inst = bankInfo.Instructions[index];
                    if (ImGui.Selectable(inst.Offset.ToString("X4")))
                    {
                        // TODO2: needs to jump to appropriate bank
                        _debugger_ScrollToInstructionIndex = index;
                    }
                    ImGui.TableNextColumn();
                    ImGui.Text(inst.DisplayInstruction());
                }

                ImGui.PopID();
            }
        }
    }

    internal void OnDebugBreak(InstructionDebugInfo info)
    {
        var debugInfo = _game.PrimaryVmu.LazyDebugInfo;
        Debug.Assert(debugInfo is not null);
        var bankInfo = debugInfo.CurrentBankInfo;
        var index = bankInfo.BinarySearchInstructions(info.Offset);
        _debugger_ScrollToInstructionIndex = index;
    }

    private void LayoutKeyMapping()
    {
        if (_mappingEditState.KeyMappings is null)
            return;

        bool doOpenEditKey = false;

        var editingPrimaryVmu = _mappingEditState.TargetVmu == _game.PrimaryVmu;
        var title = (_game.UseSecondaryVmu, editingPrimaryVmu) switch
        {
            (false, _) => "Keyboard Config",
            (true, true) => "Slot 1 Keyboard Config",
            (true, false) => "Slot 2 Keyboard Config"
        };
        ImGui.Begin(title, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.Modal);
        {
            if (ImGui.Button("Save"))
            {
                _game.Configuration_DoneEditingKeyMappings(_mappingEditState.KeyMappings.ToImmutableArray(), forPrimary: _mappingEditState.TargetVmu == _game.PrimaryVmu);
                _mappingEditState = default;
                Unpause();
                ImGui.End();
                return;
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                _mappingEditState = default;
                Unpause();
                ImGui.End();
                return;
            }

            ImGui.SeparatorText("Presets");
            ImGui.PushID("KeyPresetCombo");
            ImGui.SetNextItemWidth(80);
            var keyPresets = editingPrimaryVmu ? Configuration.AllPrimaryKeyPresets : Configuration.AllSecondaryKeyPresets;
            if (ImGui.BeginCombo(label: "", preview_value: keyPresets[_mappingEditState.PresetIndex].name))
            {
                for (int i = 0; i < keyPresets.Length; i++)
                {
                    if (ImGui.Selectable(keyPresets[i].name))
                        _mappingEditState.PresetIndex = i;

                    if (ImGui.BeginItemTooltip())
                    {
                        ImGui.Text(keyPresets[i].description);
                        ImGui.EndTooltip();
                    }
                }

                ImGui.EndCombo();
            }

            if (ImGui.BeginItemTooltip())
            {
                ImGui.Text(keyPresets[_mappingEditState.PresetIndex].description);
                ImGui.EndTooltip();
            }

            ImGui.PopID();
            ImGui.SameLine();

            if (ImGui.Button("Apply"))
            {
                var preset = keyPresets[_mappingEditState.PresetIndex].mappings;
                _mappingEditState.KeyMappings = [.. preset];
            }

            ImGui.SeparatorText("Mappings");

            if (ImGui.BeginTable("Key Mappings", columns: 2, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn(label: "", flags: ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn(label: "", flags: ImGuiTableColumnFlags.WidthStretch);

                for (int i = 0; i < _mappingEditState.KeyMappings.Count; i++)
                {
                    ImGui.PushID(i);

                    var mapping = _mappingEditState.KeyMappings[i];
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(mapping.TargetButton.ToString());

                    ImGui.TableSetColumnIndex(1);
                    if (ImGui.Button(mapping.SourceKey.ToString()))
                    {
                        _mappingEditState.EditedIndex = i;
                        doOpenEditKey = true;
                    }

                    ImGui.PopID();
                }
                ImGui.EndTable();
            }

            ImGui.End();
        }

        if (doOpenEditKey)
            ImGui.OpenPopup("Edit Key");
    }

    private void LayoutButtonMapping()
    {
        if (_mappingEditState.ButtonMappings is null)
            return;

        bool doOpenEditKey = false;

        var title = _game.UseSecondaryVmu switch
        {
            false => "Gamepad Config",
            true when _mappingEditState.TargetVmu == _game.PrimaryVmu => "Slot 1 Gamepad Config",
            _ => "Slot 2 Gamepad Config"
        };
        ImGui.Begin(title, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.Modal);
        {
            if (ImGui.Button("Save"))
            {
                _game.Configuration_DoneEditingButtonMappings(
                    _mappingEditState.ButtonMappings.ToImmutableArray(),
                    gamePadIndex: _mappingEditState.GamePadIndex,
                    forPrimary: _mappingEditState.TargetVmu == _game.PrimaryVmu);
                _mappingEditState = default;
                Unpause();
                ImGui.End();
                return;
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                _mappingEditState = default;
                Unpause();
                ImGui.End();
                return;
            }

            ImGui.SeparatorText("Gamepads");
            ImGui.PushID("GamepadsCombo");
            var previewValue = _mappingEditState.GamePadIndex switch
            {
                InputMappings.GamePadIndex_None => "None",
                var index => $"{_mappingEditState.GamePadIndex + 1}: {GamePad.GetCapabilities(index).DisplayName ?? "<not found>"}"
            };
            if (ImGui.BeginCombo(label: "", previewValue))
            {
                if (ImGui.Selectable("None"))
                {
                    _mappingEditState.GamePadIndex = InputMappings.GamePadIndex_None;
                }

                for (int i = 0; i < GamePad.MaximumGamePadCount; i++)
                {
                    var capabilities = GamePad.GetCapabilities(i);
                    if (!capabilities.IsConnected)
                        continue;

                    if (ImGui.Selectable($"{i + 1}: {capabilities.DisplayName}"))
                        _mappingEditState.GamePadIndex = i;
                }

                ImGui.EndCombo();
            }
            ImGui.PopID();

            ImGui.SeparatorText("Presets");
            ImGui.PushID("ButtonPresetCombo");
            ImGui.SetNextItemWidth(80);
            if (ImGui.BeginCombo(label: "", preview_value: Configuration.AllButtonPresets[_mappingEditState.PresetIndex].name))
            {
                for (int i = 0; i < Configuration.AllButtonPresets.Length; i++)
                {
                    if (ImGui.Selectable(Configuration.AllButtonPresets[i].name))
                        _mappingEditState.PresetIndex = i;

                    if (ImGui.BeginItemTooltip())
                    {
                        ImGui.Text(Configuration.AllButtonPresets[i].description);
                        ImGui.EndTooltip();
                    }
                }

                ImGui.EndCombo();
            }

            if (ImGui.BeginItemTooltip())
            {
                ImGui.Text(Configuration.AllButtonPresets[_mappingEditState.PresetIndex].description);
                ImGui.EndTooltip();
            }

            ImGui.PopID();
            ImGui.SameLine();

            if (ImGui.Button("Apply"))
            {
                var preset = Configuration.AllButtonPresets[_mappingEditState.PresetIndex].mappings;
                _mappingEditState.ButtonMappings = [.. preset];
            }

            ImGui.SeparatorText("Mappings");

            if (ImGui.BeginTable("Button Mappings", columns: 2, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn(label: "", flags: ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn(label: "", flags: ImGuiTableColumnFlags.WidthStretch);

                for (int i = 0; i < _mappingEditState.ButtonMappings.Count; i++)
                {
                    ImGui.PushID(i);

                    var mapping = _mappingEditState.ButtonMappings[i];
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(mapping.TargetButton.ToString());

                    ImGui.TableSetColumnIndex(1);
                    if (ImGui.Button(mapping.SourceButton.ToString()))
                    {
                        _mappingEditState.EditedIndex = i;
                        doOpenEditKey = true;
                    }

                    ImGui.PopID();
                }
                ImGui.EndTable();
            }

            ImGui.End();
        }

        if (doOpenEditKey)
            ImGui.OpenPopup("Edit Button");
    }

    private void LayoutEditKey()
    {
        if (ImGui.BeginPopup("Edit Key"))
        {
            if (_mappingEditState.KeyMappings is not { } keyMappings)
                throw new InvalidOperationException();

            ImGui.Text("Press a key.");
            ImGui.Text($"Target button: {keyMappings[_mappingEditState.EditedIndex].TargetButton}");

            if (ImGui.Button("Cancel (ESC)"))
                ImGui.CloseCurrentPopup();

            ImGui.SameLine();
            if (ImGui.Button("Unmap (DEL)"))
                mapKey(Keys.None);

            var keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Keys.Escape))
                ImGui.CloseCurrentPopup();
            else if (keyboard.IsKeyDown(Keys.Delete))
                mapKey(Keys.None);
            else if (keyboard.GetPressedKeyCount() == 1)
                mapKey(keyboard.GetPressedKeys()[0]);

            ImGui.EndPopup();

            void mapKey(Keys key)
            {
                keyMappings[_mappingEditState.EditedIndex] = keyMappings[_mappingEditState.EditedIndex] with { SourceKey = key };
                ImGui.CloseCurrentPopup();
            }
        }

    }

    private void LayoutEditButton()
    {
        if (ImGui.BeginPopup("Edit Button"))
        {
            if (_mappingEditState.ButtonMappings is not { } buttonMappings)
                throw new InvalidOperationException();

            ImGui.Text("Press a button.");
            ImGui.Text($"Target button: {buttonMappings[_mappingEditState.EditedIndex].TargetButton}");

            if (ImGui.Button("Cancel (ESC)"))
                ImGui.CloseCurrentPopup();

            ImGui.SameLine();
            if (ImGui.Button("Unmap (DEL)"))
                mapButton(Buttons.None);

            var keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Keys.Escape))
                ImGui.CloseCurrentPopup();

            if (keyboard.IsKeyDown(Keys.Delete))
                mapButton(Buttons.None);

            var gamepad = GamePad.GetState(_mappingEditState.GamePadIndex);
            var sourceButton = Enum.GetValues<Buttons>().FirstOrDefault(b => gamepad.IsButtonDown(b));
            if (sourceButton != default)
                mapButton(sourceButton);

            ImGui.EndPopup();

            void mapButton(Buttons button)
            {
                buttonMappings[_mappingEditState.EditedIndex] = buttonMappings[_mappingEditState.EditedIndex] with { SourceButton = button };
                ImGui.CloseCurrentPopup();
            }
        }
    }

    private void LayoutPendingCommandDialog()
    {
        if (PendingCommand.State is not ConfirmationState.ShowDialog)
            return;

        var title = PendingCommand.Kind switch
        {
            PendingCommandKind.NewVmu => "Create new VMU without saving?",
            PendingCommandKind.OpenVmu or PendingCommandKind.OpenRecent => "Open without saving?",
            PendingCommandKind.Reset when !_game.UseSecondaryVmu => "Reset?",
            PendingCommandKind.Reset when PendingCommand.VmuPresenter == _game.PrimaryVmuPresenter => "Reset slot 1?",
            PendingCommandKind.Reset => "Reset slot 2?",
            PendingCommandKind.Exit => "Exit without saving?",
            _ => throw new InvalidOperationException(),
        };

        // Center the dialog on the current VMU when it first appears
        if (PendingCommand.VmuPresenter is { Bounds: var bounds })
            ImGui.SetNextWindowPos(new Numerics.Vector2((bounds.Left + bounds.Right) / 2, bounds.Top + 50), cond: ImGuiCond.Appearing, pivot: new Numerics.Vector2(0.5f, 0.5f));

        if (ImGui.Begin(title, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoResize))
        {
            ImGui.Text("Unsaved progress will be lost.");

            var confirmLabel = PendingCommand.Kind switch
            {
                PendingCommandKind.NewVmu => "Create",
                PendingCommandKind.OpenVmu => "Open",
                PendingCommandKind.Reset => "Reset",
                PendingCommandKind.Exit => "Exit",
                _ => throw new InvalidOperationException(),
            };

            if (ImGui.Button(confirmLabel))
            {
                PendingCommand = PendingCommand.Confirmed();
                performCommand();
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                PendingCommand = default;
                Unpause();
            }

            ImGui.End();
        }

        void performCommand()
        {
            Debug.Assert(PendingCommand.State == ConfirmationState.Confirmed);
            Unpause();
            switch (PendingCommand.Kind)
            {
                case PendingCommandKind.Exit:
                    _game.Exit();
                    // Ideally, we always reset the PendingCommand state after 'performCommand()'.
                    // But, OnExiting is not called until after this tick.
                    // So, we specifically need to keep PendingCommand around for this command.
                    break;
                case PendingCommandKind.Reset:
                    Reset(PendingCommand.VmuPresenter ?? throw new InvalidOperationException());
                    PendingCommand = default;
                    break;
                case PendingCommandKind.NewVmu:
                    NewVmu(PendingCommand.VmuPresenter ?? throw new InvalidOperationException());
                    PendingCommand = default;
                    break;
                case PendingCommandKind.OpenVmu:
                    OpenVmuDialog(PendingCommand.VmuPresenter ?? throw new InvalidOperationException());
                    PendingCommand = default;
                    break;
                case PendingCommandKind.OpenRecent:
                    OpenRecentFile(
                        PendingCommand.VmuPresenter ?? throw new InvalidOperationException(nameof(PendingCommand.VmuPresenter)),
                        PendingCommand.FilePath ?? throw new InvalidOperationException(nameof(PendingCommand.FilePath)));
                    PendingCommand = default;
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }
    }

    [GeneratedRegex(@"\b|_")]
    private static partial Regex BreakLinesPattern();
}

/// <summary>
/// Wraps a scope where UI elements should be disabled according to some condition.
/// Note that these aren't re-entrant. Don't try to nest them.
/// </summary>
struct DisabledScope : IDisposable
{
    private readonly bool _disabled;

    public DisabledScope(bool disabled)
    {
        _disabled = disabled;
        if (disabled)
            ImGui.BeginDisabled();
    }

    public void Dispose()
    {
        if (_disabled)
            ImGui.EndDisabled();
    }
}
