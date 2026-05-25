using System;
using DreamPotato.MonoGame;

Console.WriteLine($"DreamPotato VMU Emulator");
if (args.Length > 1)
{
    Console.Error.WriteLine($"Usage: 'DreamPotato [vmu-or-vms-path]'");
    return 1;
}

string? gameFilePath = args is [var path] ? path : null;

PlatformServices.SetCurrent(new DesktopPlatformServices());

using var game = new DreamPotato.MonoGame.Game1(gameFilePath);
game.Run();
return 0;
