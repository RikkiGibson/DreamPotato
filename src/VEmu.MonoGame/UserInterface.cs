using System;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;

using Myra;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;

namespace VEmu.MonoGame;

class UserInterface
{
    public static Desktop Initialize(Game1 game, int menuBarHeight)
    {
        MyraEnvironment.Game = game;

        var root = new VerticalStackPanel();

        var fileMenuSection = new MenuItem
        {
            Id = "file",
            Text = "File",
        };
        var emulationSection = new MenuItem
        {
            Id = "emulation",
            Text = "Emulation"
        };

        root.Widgets.Add(new HorizontalMenu()
        {
            Items = { fileMenuSection, emulationSection },
            Height = menuBarHeight
        });

        var desktop = new Desktop();
        desktop.Root = root;

        // File menu actions
        {
            var openVMSFileItem = new MenuItem("openVMSFile", "Open VMS (Game)");
            var openVMUFileItem = new MenuItem("openVMUFile", "Open VMU (Memory Card)");
            var quitItem = new MenuItem("quit", "Quit");
            fileMenuSection.Items.Add(openVMSFileItem);
            fileMenuSection.Items.Add(openVMUFileItem);
            fileMenuSection.Items.Add(quitItem);

            const int fileDialogWidth = 600;
            const int fileDialogHeight = 400;
            openVMSFileItem.Selected += (s, a) =>
            {
                var fileDialog = new FileDialog(FileDialogMode.OpenFile)
                {
                    Title = "Open .vms File",
                    Filter = "*.vms"
                };

                var oldWindowSize = game.WindowSize;
                fileDialog.Closed += (s, a) =>
                {
                    game.WindowSize = oldWindowSize;
                    if (!fileDialog.Result)
                        return;

                    game.Vmu.LoadGameVms(fileDialog.FilePath);
                    game.Paused = false;
                };

                game.WindowSize = new Point(fileDialogWidth, fileDialogHeight);
                fileDialog.ShowModal(desktop);
            };

            openVMUFileItem.Selected += (s, a) =>
            {
                var fileDialog = new FileDialog(FileDialogMode.OpenFile)
                {
                    Title = "Open .vmu or .bin File"
                };
                var oldWindowSize = game.WindowSize;
                fileDialog.Closed += (s, a) =>
                {
                    game.WindowSize = oldWindowSize;
                    if (!fileDialog.Result)
                        return;

                    game.Vmu.LoadVmu(fileDialog.FilePath);
                    game.Paused = false;
                };

                game.WindowSize = new Point(fileDialogWidth, fileDialogHeight);
                fileDialog.ShowModal(desktop);
            };

            quitItem.Selected += (s, a) =>
            {
                game.Exit();
            };
        }

        // Emulation menu actions
        {
            var pauseItem = new MenuItem("pause", "Pause/Resume");
            pauseItem.Selected += (s, a) =>
            {
                game.Paused = !game.Paused;
            };

            var openConfigItem = new MenuItem("openConfig", "Open configuration.json");
            openConfigItem.Selected += (s, a) =>
            {
                // NB: this won't work when debugging with VS Code, if VS Code is the default app for json files
                new Process()
                {
                    StartInfo = new ProcessStartInfo(Configuration.ConfigFilePath)
                    {
                        UseShellExecute = true,
                    }
                }.Start();
            };

            emulationSection.Items.Add(pauseItem);
            emulationSection.Items.Add(openConfigItem);
        }

        return desktop;
    }
}