
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
    Exit
}

readonly struct PendingCommand
{
    private PendingCommand(ConfirmationState confirmationState, PendingCommandKind kind)
    {
        // If we are showing dialog or confirming something, there needs to be an actual command
        if (confirmationState is not ConfirmationState.None && kind is PendingCommandKind.None)
            throw new ArgumentException(null, nameof(kind));

        State = confirmationState;
        Kind = kind;
    }

    public ConfirmationState State { get; }
    public PendingCommandKind Kind { get; }

    public static PendingCommand ShowDialog(PendingCommandKind kind) => new(ConfirmationState.ShowDialog, kind);
    public PendingCommand Confirmed() => new(ConfirmationState.Confirmed, Kind);
}

class UserInterface
{
    private readonly Game1 _game;

    private ImGuiRenderer _imGuiRenderer = null!;
    private nint _rawIconConnectedTexture;
    private GCHandle _iniFilenameHandle;

    private bool _pauseWhenClosed;

    private int _editedMappingIndex = -1;
    private List<KeyMapping>? _editingKeyMappings;
    private List<ButtonMapping>? _editingButtonMappings;

    private int _buttonPresetIndex = 0;
    private int _keyPresetIndex = 0;

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
    internal void ShowConfirmCommandDialog(PendingCommandKind commandKind)
    {
        Pause();
        PendingCommand = PendingCommand.ShowDialog(commandKind);
    }

    private void Pause()
    {
        if (_game.Paused)
            return;

        // TODO: if user presses insert/eject while in menus we can end up unpausing when we shouldn't.
        _pauseWhenClosed = _game.Paused;
        _game.Paused = true;
    }

    internal void NewVmu()
    {
        if (_game.PrimaryVmu.HasUnsavedChanges && PendingCommand is not { Kind: PendingCommandKind.NewVmu, State: ConfirmationState.Confirmed })
        {
            ShowConfirmCommandDialog(PendingCommandKind.NewVmu);
            return;
        }

        _game.LoadNewVmu();
    }

    internal void OpenVmsDialog()
    {
        if (_game.PrimaryVmu.HasUnsavedChanges && PendingCommand is not { Kind: PendingCommandKind.OpenVms, State: ConfirmationState.Confirmed })
        {
            ShowConfirmCommandDialog(PendingCommandKind.OpenVms);
            return;
        }

        var result = Dialog.FileOpen("vms", defaultPath: null);
        if (result.IsOk)
        {
            _game.LoadAndStartVmsOrVmuFile(_game.PrimaryVmu, result.Path);
        }
    }

    internal void OpenVmuDialog()
    {
        if (_game.PrimaryVmu.HasUnsavedChanges && PendingCommand is not { Kind: PendingCommandKind.OpenVmu, State: ConfirmationState.Confirmed })
        {
            ShowConfirmCommandDialog(PendingCommandKind.OpenVmu);
            return;
        }

        var result = Dialog.FileOpen("vmu,bin", defaultPath: null);
        if (result.IsOk)
        {
            _game.LoadAndStartVmsOrVmuFile(_game.PrimaryVmu, result.Path);
        }
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
        LayoutMenuBar();
        LayoutSettings();

        LayoutKeyMapping();
        LayoutEditKey();

        LayoutButtonMapping();
        LayoutEditButton();

        LayoutResetModal();
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

    private void LayoutMenuBar()
    {
        ImGui.BeginMainMenuBar();
        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("New VMU"))
                NewVmu();

            if (ImGui.MenuItem("Open VMS (Game)"))
                OpenVmsDialog();

            if (ImGui.MenuItem("Open VMU (Memory Card)"))
                OpenVmuDialog();

            if (ImGui.MenuItem("Save As"))
            {
                var result = Dialog.FileSave(filterList: "vmu,bin", defaultPath: null);
                if (result.IsOk)
                {
                    _game.SaveVmuFileAs(result.Path);
                }
            }

            ImGui.Separator();

            // TODO: list recently opened VMU files?

            if (ImGui.MenuItem("Quit"))
                _game.Exit();

            ImGui.EndMenu();
        }

        bool doOpenResetModal = false;
        bool doOpenSettings = false;
        if (ImGui.BeginMenu("Emulation"))
        {
            var isDocked = _game.PrimaryVmu.IsDocked;
            using (new DisabledScope(disabled: isDocked))
            {
                if (ImGui.MenuItem(_game.Paused ? "Resume" : "Pause"))
                {
                    _game.Paused = !_game.Paused;
                }

                if (ImGui.MenuItem("Reset"))
                {
                    // Workaround to delay calling OpenPopup: https://github.com/ocornut/imgui/issues/331#issuecomment-751372071
                    doOpenResetModal = true;
                }
            }

            if (ImGui.MenuItem(isDocked ? "Eject VMU" : "Dock VMU"))
            {
                _game.PrimaryVmu.DockOrEject();
            }

            ImGui.Separator();

            using (new DisabledScope(_game.PrimaryVmu.LoadedFilePath is null))
            {
                if (ImGui.MenuItem("Save State"))
                {
                    _game.PrimaryVmu.SaveState("0");
                }

                if (ImGui.MenuItem("Load State"))
                {
                    if (_game.PrimaryVmu.LoadStateById(id: "0", saveOopsFile: true) is (false, var error))
                    {
                        ShowToast(error ?? $"An unknown error occurred in '{nameof(Vmu.LoadStateById)}'.");
                    }
                }

                if (ImGui.MenuItem("Undo Load State"))
                {
                    _game.PrimaryVmu.LoadOopsFile();
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
                // Workaround to delay calling OpenPopup: https://github.com/ocornut/imgui/issues/331#issuecomment-751372071
                Pause();
                _editingKeyMappings = _game.Configuration.PrimaryInput.KeyMappings.ToList();
            }

            if (ImGui.MenuItem("Gamepad Config"))
            {
                // Workaround to delay calling OpenPopup: https://github.com/ocornut/imgui/issues/331#issuecomment-751372071
                Pause();
                _editingButtonMappings = _game.Configuration.PrimaryInput.ButtonMappings.ToList();
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

        if (doOpenResetModal)
            OpenPopupAndPause("Reset?");

        if (doOpenSettings)
            OpenPopupAndPause("Settings");
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
            if (ImGui.Checkbox("Auto-initialize date on startup", ref autoInitializeDate))
                _game.Configuration_AutoInitializeDateChanged(autoInitializeDate);


            var anyButtonWakesFromSleep = configuration.AnyButtonWakesFromSleep;
            if (ImGui.Checkbox("Any button wakes from sleep", ref anyButtonWakesFromSleep))
                _game.Configuration_AnyButtonWakesFromSleepChanged(anyButtonWakesFromSleep);


            var preserveAspectRatio = configuration.PreserveAspectRatio;
            if (ImGui.Checkbox("Preserve aspect ratio", ref preserveAspectRatio))
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
        if (_editingKeyMappings is null)
            return;

        bool doOpenEditKey = false;

        ImGui.Begin("Keyboard Config", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.Modal);
        {
            if (ImGui.Button("Save"))
            {
                _game.Configuration_DoneEditingKeyMappings(_editingKeyMappings.ToImmutableArray());
                _editingKeyMappings = null;
                Unpause();
                ImGui.End();
                return;
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                _editingKeyMappings = null;
                Unpause();
                ImGui.End();
                return;
            }

            ImGui.SeparatorText("Presets");
            ImGui.PushID("KeyPresetCombo");
            ImGui.SetNextItemWidth(80);
            if (ImGui.BeginCombo(label: "", preview_value: Configuration.AllKeyPresets[_keyPresetIndex].name))
            {
                for (int i = 0; i < Configuration.AllKeyPresets.Length; i++)
                {
                    if (ImGui.Selectable(Configuration.AllKeyPresets[i].name))
                        _keyPresetIndex = i;

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
                ImGui.Text(Configuration.AllKeyPresets[_keyPresetIndex].description);
                ImGui.EndTooltip();
            }

            ImGui.PopID();
            ImGui.SameLine();

            if (ImGui.Button("Apply"))
            {
                var preset = Configuration.AllKeyPresets[_keyPresetIndex].mappings;
                _editingKeyMappings = [.. preset];
            }

            ImGui.SeparatorText("Mappings");

            if (ImGui.BeginTable("Key Mappings", columns: 2, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn(label: "", flags: ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn(label: "", flags: ImGuiTableColumnFlags.WidthStretch);

                for (int i = 0; i < _editingKeyMappings.Count; i++)
                {
                    ImGui.PushID(i);

                    var mapping = _editingKeyMappings[i];
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(mapping.TargetButton.ToString());

                    ImGui.TableSetColumnIndex(1);
                    if (ImGui.Button(mapping.SourceKey.ToString()))
                    {
                        _editedMappingIndex = i;
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
        if (_editingButtonMappings is null)
            return;

        bool doOpenEditKey = false;

        ImGui.Begin("Gamepad Config", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.Modal);
        {
            if (ImGui.Button("Save"))
            {
                _game.Configuration_DoneEditingButtonMappings(_editingButtonMappings.ToImmutableArray());
                _editingButtonMappings = null;
                Unpause();
                ImGui.End();
                return;
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                _editingButtonMappings = null;
                Unpause();
                ImGui.End();
                return;
            }

            ImGui.SeparatorText("Presets");
            ImGui.PushID("ButtonPresetCombo");
            ImGui.SetNextItemWidth(80);
            if (ImGui.BeginCombo(label: "", preview_value: Configuration.AllButtonPresets[_buttonPresetIndex].name))
            {
                for (int i = 0; i < Configuration.AllButtonPresets.Length; i++)
                {
                    if (ImGui.Selectable(Configuration.AllButtonPresets[i].name))
                        _buttonPresetIndex = i;

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
                ImGui.Text(Configuration.AllButtonPresets[_buttonPresetIndex].description);
                ImGui.EndTooltip();
            }

            ImGui.PopID();
            ImGui.SameLine();

            if (ImGui.Button("Apply"))
            {
                var preset = Configuration.AllButtonPresets[_buttonPresetIndex].mappings;
                _editingButtonMappings = [.. preset];
            }

            ImGui.SeparatorText("Mappings");

            if (ImGui.BeginTable("Button Mappings", columns: 2, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn(label: "", flags: ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn(label: "", flags: ImGuiTableColumnFlags.WidthStretch);

                for (int i = 0; i < _editingButtonMappings.Count; i++)
                {
                    ImGui.PushID(i);

                    var mapping = _editingButtonMappings[i];
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(mapping.TargetButton.ToString());

                    ImGui.TableSetColumnIndex(1);
                    if (ImGui.Button(mapping.SourceButton.ToString()))
                    {
                        _editedMappingIndex = i;
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
            if (_editingKeyMappings is null)
                throw new InvalidOperationException();

            ImGui.Text($"Target button: {_editingKeyMappings[_editedMappingIndex].TargetButton}");
            ImGui.Text("Press a key or press ESC to cancel.");

            var keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                ImGui.CloseCurrentPopup();
            }
            else if (keyboard.GetPressedKeyCount() != 0)
            {
                var key = keyboard.GetPressedKeys()[0];
                _editingKeyMappings[_editedMappingIndex] = _editingKeyMappings[_editedMappingIndex] with { SourceKey = key };
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private void LayoutEditButton()
    {
        if (ImGui.BeginPopup("Edit Button"))
        {
            if (_editingButtonMappings is null)
                throw new InvalidOperationException();

            ImGui.Text($"Target button: {_editingButtonMappings[_editedMappingIndex].TargetButton}");
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

            _editingButtonMappings[_editedMappingIndex] = _editingButtonMappings[_editedMappingIndex] with { SourceButton = sourceButton };
            ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    private void LayoutResetModal()
    {
        if (ImGui.BeginPopupModal("Reset?", ImGuiWindowFlags.NoResize))
        {
            ImGui.Text("Unsaved progress will be lost.");
            if (ImGui.Button("Reset"))
            {
                _game.Reset();
                ClosePopupAndUnpause();
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
                ClosePopupAndUnpause();

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
                case PendingCommandKind.NewVmu:
                    NewVmu();
                    PendingCommand = default;
                    break;
                case PendingCommandKind.OpenVms:
                    OpenVmsDialog();
                    PendingCommand = default;
                    break;
                case PendingCommandKind.OpenVmu:
                    OpenVmuDialog();
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
