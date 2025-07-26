
using System;
using System.IO;

using ImGuiNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
        ImGuiLayout();

        // Call AfterLayout now to finish up and draw all the things
        _imGuiRenderer.AfterLayout();
    }

    // Direct port of the example at https://github.com/ocornut/imgui/blob/master/examples/sdl_opengl2_example/main.cpp
    protected void ImGuiLayout()
    {
        bool openSaveFile = false;
        ImGui.BeginMainMenuBar();
        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("Open VMS (Game)"))
            {
                openSaveFile = true;
            }
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Emulation"))
        {
            ImGui.MenuItem("GOOD STUFF");
            ImGui.EndMenu();
        }

        ImGui.EndMainMenuBar();

        if (openSaveFile)
            ImGui.OpenPopup("save-file");
        LayoutFilePicker();
    }

    private void LayoutFilePicker()
    {
        var isOpen = true;
        if (ImGui.BeginPopupModal("save-file", ref isOpen, ImGuiWindowFlags.NoTitleBar))
        {
            var picker = FilePicker.GetFolderPicker(this, Path.Combine(Environment.CurrentDirectory));
            if (picker.Draw())
            {
                Console.WriteLine(picker.SelectedFile);
                FilePicker.RemoveFilePicker(this);
            }
            ImGui.EndPopup();
        }
    }
}