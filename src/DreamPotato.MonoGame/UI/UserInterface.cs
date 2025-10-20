
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
    private PendingCommand(ConfirmationState confirmationState, PendingCommandKind kind, Vmu? vmu)
    {
        // If we are showing dialog or confirming something, there needs to be an actual command
        if (confirmationState is not ConfirmationState.None && kind is PendingCommandKind.None)
            throw new ArgumentException(null, nameof(kind));

        State = confirmationState;
        Kind = kind;
        Vmu = vmu;
    }

    public ConfirmationState State { get; }
    public PendingCommandKind Kind { get; }
    public Vmu? Vmu { get; }

    public static PendingCommand ShowDialog(PendingCommandKind kind, Vmu? vmu) => new(ConfirmationState.ShowDialog, kind, vmu);
    public PendingCommand Confirmed() => new(ConfirmationState.Confirmed, Kind, Vmu);
}

struct MappingEditState
{
    private object? _editedMappings;

    /// <summary>Which preset is currently selected in the combo box?</summary>
    public int PresetIndex;

    /// <summary>Which element of '_editedMappings' are we currently editing the key or button for?</summary>
    public int EditedIndex;

    /// <summary>Which VMU are we editing mappings for?</summary>
    public Vmu? TargetVmu { get; }

    private MappingEditState(object editedMappings, Vmu targetVmu)
    {
        if (editedMappings is not (List<KeyMapping> or List<ButtonMapping>))
            throw new ArgumentException(null, nameof(editedMappings));

        _editedMappings = editedMappings;
        PresetIndex = 0;
        EditedIndex = -1;
        TargetVmu = targetVmu;
    }

    public static MappingEditState EditKeyMappings(ImmutableArray<KeyMapping> mappings, Vmu targetVmu)
        => new(mappings.ToList(), targetVmu);

    public static MappingEditState EditButtonMappings(ImmutableArray<ButtonMapping> mappings, Vmu targetVmu)
        => new(mappings.ToList(), targetVmu);

    public List<KeyMapping>? KeyMappings { get => _editedMappings as List<KeyMapping>; set => _editedMappings = value; }
    public List<ButtonMapping>? ButtonMappings { get => _editedMappings as List<ButtonMapping>; set => _editedMappings = value; }
}

class UserInterface
{
    private readonly Game1 _game;

    private ImGuiRenderer _imGuiRenderer = null!;
    private nint _rawIconConnectedTexture;
    private GCHandle _iniFilenameHandle;

    private bool _pauseWhenClosed;

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

    internal void Initialize(Texture2D iconConnectedTexture)
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

        _rawIconConnectedTexture = _imGuiRenderer.BindTexture(iconConnectedTexture);
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
    internal void ShowConfirmCommandDialog(PendingCommandKind commandKind, Vmu? vmu)
    {
        Pause();
        PendingCommand = PendingCommand.ShowDialog(commandKind, vmu);
    }

    private void Pause()
    {
        if (_game.Paused)
            return;

        // TODO: if user presses insert/eject while in menus we can end up unpausing when we shouldn't.
        _pauseWhenClosed = _game.Paused;
        _game.Paused = true;
    }

    internal void NewVmu(Vmu vmu)
    {
        if (vmu.HasUnsavedChanges && PendingCommand is not { Kind: PendingCommandKind.NewVmu, State: ConfirmationState.Confirmed })
        {
            ShowConfirmCommandDialog(PendingCommandKind.NewVmu, vmu);
            return;
        }

        _game.LoadNewVmu(vmu);
    }

    internal void OpenVmsDialog(Vmu vmu)
    {
        if (vmu.HasUnsavedChanges && PendingCommand is not { Kind: PendingCommandKind.OpenVms, State: ConfirmationState.Confirmed })
        {
            ShowConfirmCommandDialog(PendingCommandKind.OpenVms, vmu);
            return;
        }

        var result = Dialog.FileOpen("vms", defaultPath: null);
        if (result.IsOk)
        {
            _game.LoadAndStartVmsOrVmuFile(vmu, result.Path);
        }
    }

    internal void OpenVmuDialog(Vmu vmu)
    {
        if (vmu.HasUnsavedChanges && PendingCommand is not { Kind: PendingCommandKind.OpenVmu, State: ConfirmationState.Confirmed })
        {
            ShowConfirmCommandDialog(PendingCommandKind.OpenVmu, vmu);
            return;
        }

        var result = Dialog.FileOpen("vmu,bin", defaultPath: null);
        if (result.IsOk)
        {
            _game.LoadAndStartVmsOrVmuFile(vmu, result.Path);
        }
    }

    internal void Reset(Vmu vmu)
    {
        if (PendingCommand is not { Kind: PendingCommandKind.Reset, State: ConfirmationState.Confirmed })
        {
            ShowConfirmCommandDialog(PendingCommandKind.Reset, vmu);
            return;
        }

        _game.Reset(vmu);
    }

    private void OpenPopupAndPause(string name)
    {
        Pause();
        ImGui.OpenPopup(name);
    }

    internal void ShowToast(string message)
    {
        _toastMessage = message;
        _toastDisplayFrames = ToastMaxDisplayFrames;
    }

    private void Unpause()
    {
        _game.Paused = _pauseWhenClosed;
    }

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

        LayoutFastForwardOrPauseIndicator();
        LayoutToast();
    }

    private void LayoutFastForwardOrPauseIndicator()
    {
        var message = _game.Paused ? "||" :
            _game.IsFastForwarding ? ">>" :
            null;
        if (message is null)
            return;

        var viewport = _game.GraphicsDevice.Viewport;
        var textSize = ImGui.CalcTextSize(message, wrapWidth: viewport.Width);
        ImGui.SetNextWindowPos(new Numerics.Vector2(x: 2, y: viewport.Height - textSize.Y - Game1.MenuBarHeight));
        ImGui.SetNextWindowSize(textSize + new Numerics.Vector2(10, 20));
        if (ImGui.Begin("FastForwardOrPauseIndicator", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.TextWrapped(message);
        }

        ImGui.End();
    }

    private void LayoutToast()
    {
        if (_toastDisplayFrames == 0)
            return;

        _toastDisplayFrames--;

        var doFadeout = _toastDisplayFrames < ToastBeginFadeoutFrames;
        if (doFadeout)
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ((float)_toastDisplayFrames) / ToastBeginFadeoutFrames);

        var viewport = _game.GraphicsDevice.Viewport;
        var textSize = ImGui.CalcTextSize(_toastMessage, wrapWidth: viewport.Width);
        // TODO probably userinterface should own the const MenuBarHeight
        ImGui.SetNextWindowPos(new Numerics.Vector2(x: 2, y: viewport.Height - textSize.Y - Game1.MenuBarHeight));
        ImGui.SetNextWindowSize(textSize + new Numerics.Vector2(10, 20));
        if (ImGui.Begin("Toast", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.TextWrapped(_toastMessage);
        }

        ImGui.End();

        if (doFadeout)
            ImGui.PopStyleVar();
    }

    private void LayoutPrimaryMenuBar()
    {
        var vmu = _game.PrimaryVmu;
        ImGui.BeginMainMenuBar();
        if (ImGui.BeginMenu("File"))
        {
            LayoutNewOpenSaveMenuItems(vmu);
            ImGui.Separator();

            if (ImGui.MenuItem("Quit"))
                _game.Exit();

            ImGui.EndMenu();
        }

        bool doOpenSettings = false;
        if (ImGui.BeginMenu("Emulation"))
        {
            var isDocked = vmu.IsDocked;
            using (new DisabledScope(disabled: isDocked))
            {
                if (ImGui.MenuItem(_game.Paused ? "Resume" : "Pause"))
                    _game.Paused = !_game.Paused;

                if (ImGui.MenuItem("Reset"))
                    Reset(vmu);
            }

            if (ImGui.MenuItem(isDocked ? "Eject VMU" : "Dock VMU"))
                vmu.DockOrEject();

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

            ImGui.EndMenu();
        }

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
                _mappingEditState = MappingEditState.EditKeyMappings(_game.Configuration.PrimaryInput.KeyMappings, vmu);
            }

            if (ImGui.MenuItem("Gamepad Config"))
            {
                Pause();
                _mappingEditState = MappingEditState.EditButtonMappings(_game.Configuration.PrimaryInput.ButtonMappings, vmu);
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
            ImGui.Image(_rawIconConnectedTexture, new Numerics.Vector2(18));
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Dreamcast connected over TCP");
                ImGui.EndTooltip();
            }
        }

        ImGui.EndMainMenuBar();

        if (doOpenSettings)
            OpenPopupAndPause("Settings");
    }

    private void LayoutSecondaryMenuBar()
    {
        if (_game.SecondaryVmu is not { } vmu)
            return;

        var rectangle = _game.SecondaryMenuBarRectangle;
        ImGui.SetNextWindowPos(new Numerics.Vector2(rectangle.X, rectangle.Y));
        ImGui.SetNextWindowSize(new Numerics.Vector2(rectangle.Width, rectangle.Height));
        ImGui.Begin("SecondaryMenuWindow", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoSavedSettings);
        ImGui.BeginMenuBar();
        if (ImGui.BeginMenu(vmu.HasUnsavedChanges ? "* File" : "File"))
        {
            if (vmu.LoadedFilePath is not null)
            {
                ImGui.Text(Path.GetFileName(vmu.LoadedFilePath.AsSpan()));
                ImGui.Separator();
            }

            LayoutNewOpenSaveMenuItems(vmu);
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Emulation"))
        {
            if (ImGui.MenuItem("Reset"))
                Reset(vmu);

            if (ImGui.MenuItem(vmu.IsDocked ? "Eject VMU" : "Dock VMU"))
                vmu.DockOrEject();

            ImGui.EndMenu();
        }

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
                _mappingEditState = MappingEditState.EditButtonMappings(_game.Configuration.SecondaryInput.ButtonMappings, vmu);
            }

            ImGui.EndMenu();
        }

        ImGui.EndMenuBar();
        ImGui.End();
    }

    private bool Checkbox(string label, ref bool v, float paddingY)
    {
        ImGui.PushStyleVarY(ImGuiStyleVar.FramePadding, paddingY);
        var changed = ImGui.Checkbox(label, ref v);
        ImGui.PopStyleVar();
        return changed;
    }

    private void LayoutNewOpenSaveMenuItems(Vmu vmu)
    {
        if (ImGui.MenuItem("New VMU"))
            NewVmu(vmu);

        if (ImGui.MenuItem("Open VMS (Game)"))
            OpenVmsDialog(vmu);

        if (ImGui.MenuItem("Open VMU (Memory Card)"))
            OpenVmuDialog(vmu);

        if (ImGui.MenuItem("Save As"))
        {
            var result = Dialog.FileSave(filterList: "vmu,bin", defaultPath: null);
            if (result.IsOk)
            {
                _game.SaveVmuFileAs(vmu, result.Path);
            }
        }

        // TODO: list recently opened VMU files?
    }

    private static readonly string[] AllDreamcastSlotNames = ["A", "B", "C", "D"];
    private static readonly string[] AllExpansionSlotNames = ["Slot 1", "Slot 2", "Slot 1 and 2"];

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

            // Slot Configuration
            {
                ImGui.Text("Use Expansion Slots");
                ImGui.SameLine();

                var expansionSlots = configuration.ExpansionSlots;
                var selectedIndex = (int)expansionSlots;
                ImGui.SetNextItemWidth(CalcComboWidth(longestItem: AllExpansionSlotNames[2]));
                ImGui.PushID("ExpansionSlotCombo");
                ImGui.Combo(label: "", ref selectedIndex, items: AllExpansionSlotNames, items_count: AllExpansionSlotNames.Length);
                ImGui.PopID();
                if ((int)expansionSlots != selectedIndex)
                    _game.Configuration_ExpansionSlotsChanged((ExpansionSlots)selectedIndex);
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

        var title = _game.UseSecondaryVmu switch
        {
            false => "Keyboard Config",
            true when _mappingEditState.TargetVmu == _game.PrimaryVmu => "Slot 1 Keyboard Config",
            _ => "Slot 2 Keyboard Config"
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
            if (ImGui.BeginCombo(label: "", preview_value: Configuration.AllKeyPresets[_mappingEditState.PresetIndex].name))
            {
                for (int i = 0; i < Configuration.AllKeyPresets.Length; i++)
                {
                    if (ImGui.Selectable(Configuration.AllKeyPresets[i].name))
                        _mappingEditState.PresetIndex = i;

                    if (ImGui.BeginItemTooltip())
                    {
                        ImGui.Text(Configuration.AllKeyPresets[i].description);
                        ImGui.EndTooltip();
                    }
                }

                ImGui.EndCombo();
            }

            if (ImGui.BeginItemTooltip())
            {
                ImGui.Text(Configuration.AllKeyPresets[_mappingEditState.PresetIndex].description);
                ImGui.EndTooltip();
            }

            ImGui.PopID();
            ImGui.SameLine();

            if (ImGui.Button("Apply"))
            {
                var preset = Configuration.AllKeyPresets[_mappingEditState.PresetIndex].mappings;
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
                _game.Configuration_DoneEditingButtonMappings(_mappingEditState.ButtonMappings.ToImmutableArray(), forPrimary: _mappingEditState.TargetVmu == _game.PrimaryVmu);
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

            ImGui.Text($"Target button: {keyMappings[_mappingEditState.EditedIndex].TargetButton}");
            ImGui.Text("Press a key or press ESC to cancel.");

            var keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                ImGui.CloseCurrentPopup();
            }
            else if (keyboard.GetPressedKeyCount() != 0)
            {
                var key = keyboard.GetPressedKeys()[0];
                keyMappings[_mappingEditState.EditedIndex] = keyMappings[_mappingEditState.EditedIndex] with { SourceKey = key };
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private void LayoutEditButton()
    {
        if (ImGui.BeginPopup("Edit Button"))
        {
            if (_mappingEditState.ButtonMappings is not { } buttonMappings)
                throw new InvalidOperationException();

            ImGui.Text($"Target button: {buttonMappings[_mappingEditState.EditedIndex].TargetButton}");
            ImGui.Text("Press a button or press ESC to cancel.");

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
                return;
            }

            var gamepad = GamePad.GetState(PlayerIndex.One);
            var sourceButton = Enum.GetValues<Buttons>().FirstOrDefault(b => gamepad.IsButtonDown(b));
            if (sourceButton == default)
            {
                ImGui.EndPopup();
                return;
            }

            buttonMappings[_mappingEditState.EditedIndex] = buttonMappings[_mappingEditState.EditedIndex] with { SourceButton = sourceButton };
            ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
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
            PendingCommandKind.Reset when PendingCommand.Vmu == _game.PrimaryVmu => "Reset slot 1?",
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
            switch (PendingCommand.Kind)
            {
                case PendingCommandKind.Exit:
                    _game.Exit();
                    // Ideally, we always reset the PendingCommand state after 'performCommand()'.
                    // But, OnExiting is not called until after this tick.
                    // So, we specifically need to keep PendingCommand around for this command.
                    break;
                case PendingCommandKind.Reset:
                    Reset(PendingCommand.Vmu ?? throw new InvalidOperationException());
                    PendingCommand = default;
                    break;
                case PendingCommandKind.NewVmu:
                    NewVmu(PendingCommand.Vmu ?? throw new InvalidOperationException());
                    PendingCommand = default;
                    break;
                case PendingCommandKind.OpenVms:
                    OpenVmsDialog(PendingCommand.Vmu ?? throw new InvalidOperationException());
                    PendingCommand = default;
                    break;
                case PendingCommandKind.OpenVmu:
                    OpenVmuDialog(PendingCommand.Vmu ?? throw new InvalidOperationException());
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
