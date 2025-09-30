
using System;
using System.Runtime.InteropServices;

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    AttachConsole(-1);

Console.WriteLine($"DreamPotato VMU Emulator");
if (args.Length > 1)
{
    Console.Error.WriteLine($"Usage: 'DreamPotato [vmu-or-vms-path]'");
    return 1;
}

string? gameFilePath = args is [var path] ? path : null;

using var game = new DreamPotato.MonoGame.Game1(gameFilePath);
game.Run();
return 0;

partial class Program
{
    [LibraryImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(int dwProcessId);
}