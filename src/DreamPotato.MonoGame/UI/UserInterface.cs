
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

class UserInterface
{
    private readonly Game1 _game;

    private ImGuiRenderer _imGuiRenderer = null!;
    private Texture2D _userInterfaceTexture = null!;
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

        _userInterfaceTexture = new Texture2D(_game.GraphicsDevice, Game1.TotalScreenWidth, Game1.TotalScreenHeight);
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

    private void Pause()
    {
        // TODO: if user presses insert/eject while in menus we can end up unpausing when we shouldn't.
        _pauseWhenClosed = _game.Paused;
        _game.Paused = true;
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

        LayoutToast();
    }

    private void LayoutToast()
    {
        if (_toastDisplayFrames == 0)
            return;

        _toastDisplayFrames--;

        var doFadeout = _toastDisplayFrames < ToastBeginFadeoutFrames;
        if (doFadeout)
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ((float)_toastDisplayFrames) / ToastBeginFadeoutFrames);

        var textSize = ImGui.CalcTextSize(_toastMessage, wrapWidth: Game1.TotalScreenWidth);
        ImGui.SetNextWindowPos(new Numerics.Vector2(x: 2, y: Game1.TotalScreenHeight - textSize.Y - 20));
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
            if (ImGui.MenuItem("Open VMS (Game)"))
            {
                var result = Dialog.FileOpen("vms", defaultPath: null);
                if (result.IsOk)
                {
                    _game.LoadAndStartVmsOrVmuFile(result.Path);
                }
            }

            if (ImGui.MenuItem("Open VMU (Memory Card)"))
            {
                var result = Dialog.FileOpen("vmu,bin", defaultPath: null);
                if (result.IsOk)
                {
                    _game.LoadAndStartVmsOrVmuFile(result.Path);
                }
            }

            if (ImGui.MenuItem("Save As"))
            {
                var result = Dialog.FileSave(filterList: "vmu,bin", defaultPath: null);
                if (result.IsOk)
                {
                    _game.SaveVmuFileAs(result.Path);
                }
            }

            ImGui.Separator();

            if (ImGui.MenuItem("Quit"))
            {
                _game.Exit();
            }

            ImGui.EndMenu();
        }

        bool doOpenResetModal = false;
        bool doOpenSettings = false;
        if (ImGui.BeginMenu("Emulation"))
        {
            var isEjected = _game.Vmu.IsEjected;
            using (new DisabledScope(disabled: !isEjected))
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

            if (ImGui.MenuItem(isEjected ? "Dock VMU" : "Eject VMU"))
            {
                _game.Vmu.InsertOrEject();
            }

            ImGui.Separator();

            using (new DisabledScope(disabled: !isEjected || _game.Vmu.LoadedFilePath is null))
            {
                if (ImGui.MenuItem("Save State"))
                {
                    _game.Vmu.SaveState("0");
                }

                if (ImGui.MenuItem("Load State"))
                {
                    if (_game.Vmu.LoadStateById(id: "0", saveOopsFile: true) is (false, var error))
                    {
                        ShowToast(error ?? $"An unknown error occurred in '{nameof(Vmu.LoadStateById)}'.");
                    }
                }

                if (ImGui.MenuItem("Undo Load State"))
                {
                    _game.Vmu.LoadOopsFile();
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
                _editingKeyMappings = _game.Configuration.KeyMappings.ToList();
            }

            if (ImGui.MenuItem("Gamepad Config"))
            {
                // Workaround to delay calling OpenPopup: https://github.com/ocornut/imgui/issues/331#issuecomment-751372071
                Pause();
                _editingButtonMappings = _game.Configuration.ButtonMappings.ToList();
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

        if (_game.Vmu.IsServerConnected)
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

    private static readonly string[] AllDreamcastSlotNames = ["A1", "B1", "C1", "D1"];

    private void LayoutSettings()
    {
        var configuration = _game.Configuration;

        // TODO: should not set this every frame. do it when calling OpenPopup or something
        // ImGui.SetNextWindowSize(Numerics.Vector2.Create(x: Game1.TotalScreenWidth * 8 / 10, y: Game1.TotalScreenHeight * 8 / 10));
        if (ImGui.BeginPopupModal("Settings"))
        {
            if (ImGui.Button("Done"))
            {
                _game.Configuration_DoneEditing();
                ClosePopupAndUnpause();
            }

            var autoInitializeDate = configuration.AutoInitializeDate;
            if (ImGui.Checkbox("Auto-initialize date on startup", ref autoInitializeDate))
                _game.Configuration_AutoInitializeDateChanged(autoInitializeDate);


            var anyButtonWakesFromSleep = configuration.AnyButtonWakesFromSleep;
            if (ImGui.Checkbox("Any button wakes from sleep", ref anyButtonWakesFromSleep))
                _game.Configuration_AnyButtonWakesFromSleepChanged(anyButtonWakesFromSleep);

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
                ImGui.SetNextItemWidth(120);
                ImGui.PushID("ColorPaletteCombo");
                ImGui.Combo(label: "", ref selectedIndex, items: ColorPalette.AllPaletteNames, items_count: ColorPalette.AllPaletteNames.Length);
                ImGui.PopID();
                if (paletteIndex != selectedIndex)
                    _game.Configuration_PaletteChanged(ColorPalette.AllPalettes[selectedIndex]);
            }

            // Dreamcast Port
            {
                ImGui.Text("Dreamcast controller slot");
                ImGui.SameLine();

                var port = configuration.DreamcastPort;
                var selectedIndex = (int)port;
                ImGui.SetNextItemWidth(40);
                ImGui.PushID("DreamcastSlotCombo");
                ImGui.Combo(label: "", ref selectedIndex, items: AllDreamcastSlotNames, items_count: AllDreamcastSlotNames.Length);
                ImGui.PopID();
                if ((int)port != selectedIndex)
                    _game.Configuration_DreamcastPortChanged((DreamcastPort)selectedIndex);
            }

            ImGui.EndPopup();
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
        ImGui.SetNextWindowSize(Numerics.Vector2.Create(x: Game1.TotalScreenWidth * 3 / 4, y: Game1.TotalScreenHeight * 1 / 4));
        if (ImGui.BeginPopupModal("Reset?"))
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
