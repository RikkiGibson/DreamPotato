
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
    private IntPtr _rawImguiTexture;

    public UserInterface(Game1 game)
    {
        _game = game;
    }

    internal void Initialize()
    {
        _imGuiRenderer = new ImGuiRenderer(_game);
        _imGuiRenderer.RebuildFontAtlas();

        _userInterfaceTexture = new Texture2D(_game.GraphicsDevice, Game1.TotalScreenWidth, Game1.TotalScreenHeight);
        _rawImguiTexture = _imGuiRenderer.BindTexture(_userInterfaceTexture);
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

            if (ImGui.MenuItem("Quit"))
            {
                _game.Exit();
            }

            ImGui.EndMenu();
        }

        bool doOpenSettings = false;
        if (ImGui.BeginMenu("Emulation"))
        {
            var pauseResumeLabel = _game.Paused ? "Resume" : "Pause";
            if (ImGui.MenuItem(pauseResumeLabel))
            {
                _game.Paused = !_game.Paused;
            }

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

        ImGui.EndMainMenuBar();

        if (doOpenSettings)
            ImGui.OpenPopup("Settings");
    }

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

            ImGui.Text("Volume");
            ImGui.SameLine();

            var sliderVolume = configuration.Volume;
            ImGui.PushID("VolumeSlider");
            ImGui.SliderInt(label: "", ref sliderVolume, v_min: 0, v_max: 100);
            ImGui.PopID();

            if (configuration.Volume != sliderVolume)
                _game.Configuration_VolumeChanged(sliderVolume);

            ImGui.EndPopup();
        }
    }
}