
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using DreamPotato.Core;

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
    OpenVms,
    OpenVmu,
    Reset,
    Exit
}

readonly struct PendingCommand
{
    private PendingCommand(ConfirmationState confirmationState, PendingCommandKind kind, VmuPresenter? vmuPresenter)
    {
        // If we are showing dialog or confirming something, there needs to be an actual command
        if (confirmationState is not ConfirmationState.None && kind is PendingCommandKind.None)
            throw new ArgumentException(null, nameof(kind));

        State = confirmationState;
        Kind = kind;
        VmuPresenter = vmuPresenter;
    }

    public ConfirmationState State { get; }
    public PendingCommandKind Kind { get; }
    public VmuPresenter? VmuPresenter { get; }

    public static PendingCommand ShowDialog(PendingCommandKind kind, VmuPresenter? vmuPresenter) => new(ConfirmationState.ShowDialog, kind, vmuPresenter);
    public PendingCommand Confirmed() => new(ConfirmationState.Confirmed, Kind, VmuPresenter);
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

        if (editedMappings is List<KeyMapping> && gamePadIndex > 0)
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

class UserInterface
{
    private readonly Game1 _game;

    private ImGuiRenderer _imGuiRenderer = null!;
    private nint _rawDreamcastConnectedIconTexture;
    private nint _rawVmusConnectedIconTexture;
    private GCHandle _iniFilenameHandle;

    private MappingEditState _mappingEditState;

    private string? _toastMessage;
    private int _toastDisplayFrames;
    private const int ToastMaxDisplayFrames = 2 * 60;
    private const int ToastBeginFadeoutFrames = 30;

    private readonly string _displayVersion;
    private readonly string _commitId;

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
    internal void ShowConfirmCommandDialog(PendingCommandKind commandKind, VmuPresenter? vmuPresenter)
    {
        Pause();
        PendingCommand = PendingCommand.ShowDialog(commandKind, vmuPresenter);
    }

    private void Pause()
        => _game.GlobalPaused = true;

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

    internal void OpenVmsDialog(VmuPresenter presenter)
    {
        var vmu = presenter.Vmu;
        if (vmu.HasUnsavedChanges && PendingCommand is not { Kind: PendingCommandKind.OpenVms, State: ConfirmationState.Confirmed })
        {
            ShowConfirmCommandDialog(PendingCommandKind.OpenVms, presenter);
            return;
        }

        var result = Dialog.FileOpen("vms", defaultPath: null);
        if (result.IsOk)
        {
            _game.LoadAndStartVmsOrVmuFile(presenter, result.Path);
        }
    }

    internal void OpenVmuDialog(VmuPresenter presenter)
    {
        var vmu = presenter.Vmu;
        if (vmu.HasUnsavedChanges && PendingCommand is not { Kind: PendingCommandKind.OpenVmu, State: ConfirmationState.Confirmed })
        {
            ShowConfirmCommandDialog(PendingCommandKind.OpenVmu, presenter);
            return;
        }

        var result = Dialog.FileOpen("vmu,bin", defaultPath: null);
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

    internal void ShowToast(string message, int durationFrames = ToastMaxDisplayFrames)
    {
        _toastMessage = message;
        _toastDisplayFrames = durationFrames;
    }

    private void Unpause()
        => _game.GlobalPaused = false;

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

            var rectangle = presenter.ContentRectangle;
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
        if (_toastDisplayFrames == 0)
            return;

        if (_toastMessage is null)
            throw new InvalidOperationException();

        _toastDisplayFrames--;

        var doFadeout = _toastDisplayFrames < ToastBeginFadeoutFrames;
        if (doFadeout)
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ((float)_toastDisplayFrames) / ToastBeginFadeoutFrames);

        var viewport = _game.GraphicsDevice.Viewport;
        var textSize = ImGui.CalcTextSize(text: _toastMessage, wrapWidth: viewport.Width);
        // TODO probably userinterface should own the const MenuBarHeight
        ImGui.SetNextWindowPos(new Numerics.Vector2(x: 2, y: viewport.Height - textSize.Y - Game1.MenuBarHeight));
        ImGui.SetNextWindowSize(textSize + new Numerics.Vector2(10, 20));
        if (ImGui.Begin("Toast", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.TextWrapped(_toastMessage.Replace("%", "%%"));
        }

        ImGui.End();

        if (doFadeout)
            ImGui.PopStyleVar();
    }

    private void LayoutPrimaryMenuBar()
    {
        var presenter = _game.PrimaryVmuPresenter;
        ImGui.BeginMainMenuBar();
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
        ImGui.EndMainMenuBar();

        if (doOpenSettings)
            OpenPopupAndPause("Settings");
    }

    private void LayoutEmulationMenu(VmuPresenter presenter)
    {
        var vmu = presenter.Vmu;
        if (ImGui.BeginMenu("Emulation"))
        {
            var isDocked = vmu.IsDockedToDreamcast;
            using (new DisabledScope(disabled: isDocked))
            {
                if (ImGui.MenuItem(presenter.LocalPaused ? "Resume" : "Pause"))
                    presenter.LocalPaused = !presenter.LocalPaused;

                if (ImGui.MenuItem("Reset"))
                    Reset(presenter);
            }

            ImGui.Separator();
            if (ImGui.MenuItem(isDocked ? "Eject from Dreamcast" : "Dock to Dreamcast"))
                presenter.DockOrEject();

            using (new DisabledScope(disabled: !_game.UseSecondaryVmu))
            {
                Debug.Assert(!vmu.IsOtherVmuConnected || !vmu.IsDockedToDreamcast);
                Debug.Assert((_game.SecondaryVmu is null && !_game.PrimaryVmu.IsOtherVmuConnected)
                    || _game.PrimaryVmu.IsOtherVmuConnected == _game.SecondaryVmu?.IsOtherVmuConnected);

                if (ImGui.MenuItem(vmu.IsOtherVmuConnected ? "Disconnect VMU-to-VMU" : "Connect VMU-to-VMU"))
                    _game.PrimaryVmu.ConnectOrDisconnectVmu(_game.SecondaryVmu ?? throw new InvalidOperationException());
            }

            ImGui.Separator();
            using (new DisabledScope(vmu.LoadedFilePath is null))
            {
                if (ImGui.MenuItem("Save State"))
                {
                    vmu.SaveState("0");
                }

                if (ImGui.MenuItem("Load State"))
                {
                    if (vmu.LoadStateById(id: "0", saveOopsFile: true) is (false, var error))
                    {
                        ShowToast(error ?? $"An unknown error occurred in '{nameof(Vmu.LoadStateById)}'.");
                    }
                }

                if (ImGui.MenuItem("Undo Load State"))
                {
                    vmu.LoadOopsFile();
                }
            }

            ImGui.Separator();
            if (ImGui.MenuItem("Take Screenshot"))
                presenter.TakeScreenshot();

            ImGui.EndMenu();
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
        ImGui.Begin("SecondaryMenuWindow", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoSavedSettings);
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

        if (ImGui.MenuItem("Open VMS (Game)"))
            OpenVmsDialog(presenter);

        if (ImGui.MenuItem("Open VMU (Memory Card)"))
            OpenVmuDialog(presenter);

        if (ImGui.MenuItem("Save As"))
        {
            var result = Dialog.FileSave(filterList: "vmu,bin", defaultPath: null);
            if (result.IsOk)
            {
                _game.SaveVmuFileAs(presenter.Vmu, result.Path);
            }
        }

        // TODO: list recently opened VMU files?
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
                    _game.Configuration_PaletteChanged(ColorPalette.AllPalettes[selectedIndex]);
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
                var index => $"{_mappingEditState.GamePadIndex+1}: {GamePad.GetCapabilities(index).DisplayName ?? "<not found>"}"
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

                    if (ImGui.Selectable($"{i+1}: {capabilities.DisplayName}"))
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
            PendingCommandKind.OpenVms => "Open VMS without saving?",
            PendingCommandKind.OpenVmu => "Open VMU without saving?",
            PendingCommandKind.Reset when !_game.UseSecondaryVmu => "Reset?",
            PendingCommandKind.Reset when PendingCommand.VmuPresenter == _game.PrimaryVmuPresenter => "Reset slot 1?",
            PendingCommandKind.Reset => "Reset slot 2?",
            PendingCommandKind.Exit => "Exit without saving?",
            _ => throw new InvalidOperationException(),
        };

        ImGui.Begin(title, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoResize);
        {
            ImGui.Text("Unsaved progress will be lost.");

            var confirmLabel = PendingCommand.Kind switch
            {
                PendingCommandKind.NewVmu => "Create",
                PendingCommandKind.OpenVms => "Open",
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
                case PendingCommandKind.OpenVms:
                    OpenVmsDialog(PendingCommand.VmuPresenter ?? throw new InvalidOperationException());
                    PendingCommand = default;
                    break;
                case PendingCommandKind.OpenVmu:
                    OpenVmuDialog(PendingCommand.VmuPresenter ?? throw new InvalidOperationException());
                    PendingCommand = default;
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
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
