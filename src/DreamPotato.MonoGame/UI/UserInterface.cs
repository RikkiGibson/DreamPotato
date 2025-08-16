
using System;
using System.Diagnostics;
using System.IO;

using DreamPotato.Core;

using ImGuiNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NativeFileDialogSharp;

using Numerics = System.Numerics;

namespace DreamPotato.MonoGame.UI;

class UserInterface
{
    private readonly Game1 _game;

    private ImGuiRenderer _imGuiRenderer = null!;
    private Texture2D _userInterfaceTexture = null!;
    private nint _rawIconConnectedTexture;

    public UserInterface(Game1 game)
    {
        _game = game;
    }

    internal void Initialize(Texture2D iconConnectedTexture)
    {
        _imGuiRenderer = new ImGuiRenderer(_game);
        _imGuiRenderer.RebuildFontAtlas();

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

    private void LayoutImpl()
    {
        LayoutMenuBar();
        LayoutSettings();
        LayoutResetModal();
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

        bool doOpenSettings = false;
        bool doOpenResetModal = false;
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
                    _game.Vmu.LoadStateById("0", saveOopsFile: true);
                }

                if (ImGui.MenuItem("Undo Load State"))
                {
                    _game.Vmu.LoadOopsFile();
                }
            }

            ImGui.Separator();

            if (ImGui.MenuItem("Settings"))
            {
                // Workaround to delay calling OpenPopup: https://github.com/ocornut/imgui/issues/331#issuecomment-751372071
                doOpenSettings = true;
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

        if (doOpenSettings)
            ImGui.OpenPopup("Settings");

        if (doOpenResetModal)
            ImGui.OpenPopup("Reset?");
    }

    private static readonly string[] AllDreamcastSlotNames = ["A1", "B1", "C1", "D1"];

    private void LayoutSettings()
    {
        var configuration = _game.Configuration;

        ImGui.SetNextWindowSize(Numerics.Vector2.Create(x: Game1.TotalScreenWidth * 8 / 10, y: Game1.TotalScreenHeight * 8 / 10));
        if (ImGui.BeginPopupModal("Settings"))
        {
            if (ImGui.Button("Done"))
            {
                _game.Configuration_DoneEditing();
                ImGui.CloseCurrentPopup();
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

    private void LayoutResetModal()
    {
        ImGui.SetNextWindowSize(Numerics.Vector2.Create(x: Game1.TotalScreenWidth * 3 / 4, y: Game1.TotalScreenHeight * 1 / 4));
        if (ImGui.BeginPopupModal("Reset?"))
        {
            ImGui.Text("Unsaved progress will be lost.");
            if (ImGui.Button("Reset"))
            {
                _game.Reset();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
                ImGui.CloseCurrentPopup();

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
