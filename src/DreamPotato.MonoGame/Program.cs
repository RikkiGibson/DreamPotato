
using System;

Console.WriteLine($"DreamPotato VMU Emulator");
if (args.Length > 1)
{
    Console.Error.WriteLine($"Usage: '{args[0]} [vmu-or-vms-path]'");
}

string? gameFilePath = args is [var path] ? path : null;

using var game = new DreamPotato.MonoGame.Game1(gameFilePath);
game.Run();
