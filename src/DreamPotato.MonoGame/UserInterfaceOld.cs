using System;
using System.Diagnostics;
using System.IO;

using DreamPotato.Core;

using Microsoft.Xna.Framework;

using Myra;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;

namespace DreamPotato.MonoGame;

class UserInterfaceOld
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
            var saveAsItem = new MenuItem("saveAs", "Save As");
            var quitItem = new MenuItem("quit", "Quit");
            fileMenuSection.Items.Add(openVMSFileItem);
            fileMenuSection.Items.Add(openVMUFileItem);
            fileMenuSection.Items.Add(saveAsItem);
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

                    game.Vmu.LoadGameVms(fileDialog.FilePath,
                        date: game._configuration.AutoInitializeDate ? DateTime.Now : null);
                    game.Paused = false;
                    game.UpdateWindowTitle(fileDialog.FilePath);
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

                    game.Vmu.LoadVmu(fileDialog.FilePath,
                        date: game._configuration.AutoInitializeDate ? DateTime.Now : null);
                    game.Paused = false;
                    game.UpdateWindowTitle(fileDialog.FilePath);
                };

                game.WindowSize = new Point(fileDialogWidth, fileDialogHeight);
                fileDialog.ShowModal(desktop);
            };

            saveAsItem.Selected += (s, a) =>
            {
                var fileDialog = new FileDialog(FileDialogMode.SaveFile)
                {
                    Title = "Save as .vmu File"
                };
                var oldWindowSize = game.WindowSize;
                fileDialog.Closed += (s, a) =>
                {
                    game.WindowSize = oldWindowSize;
                    if (!fileDialog.Result)
                        return;

                    var path = fileDialog.FilePath.EndsWith(".vmu", StringComparison.OrdinalIgnoreCase)
                        ? fileDialog.FilePath
                        : Path.ChangeExtension(fileDialog.FilePath, ".vmu");

                    game.Vmu.SaveVmuAs(path);
                    game.UpdateWindowTitle(path);
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

            var openConfigItem = new MenuItem("openConfig", "Open Data Folder");
            openConfigItem.Selected += (s, a) =>
            {
                new Process()
                {
                    StartInfo = new ProcessStartInfo(Vmu.DataFolder)
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