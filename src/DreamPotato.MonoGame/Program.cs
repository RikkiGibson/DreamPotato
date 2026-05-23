
using System;
using System.Runtime.InteropServices;

using DreamPotato.Core;
using DreamPotato.MonoGame;

Console.WriteLine($"DreamPotato VMU Emulator (PID: {Environment.ProcessId})");
string? gameFilePath = null;
bool integrated = false;
int? tcpPort = null;
DreamcastPort? port = null;
ExpansionSlots? slots = null;

bool showHelp = false;

// Console.WriteLine("Waiting for debugger to attach...");
// while(!System.Diagnostics.Debugger.IsAttached)
//     System.Threading.Thread.Sleep(100);

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--integrated":
            integrated = true;
            break;
        case "--tcp-port":
            i++;
            if (i >= args.Length)
            {
                Console.Error.WriteLine($"Missing '--tcp-port' argument");
                showHelp = true;
                break;
            }

            if (!int.TryParse(args[i], out var tcpPort1) || tcpPort1 <= 0)
            {
                Console.Error.WriteLine($"Bad '--tcp-port' argument: {args[i]}");
                showHelp = true;
                break;
            }

            tcpPort = tcpPort1;
            break;
        case "--port":
            i++;
            if (i >= args.Length)
            {
                Console.Error.WriteLine($"Missing '--port' argument");
                showHelp = true;
                break;
            }

            if (!char.TryParse(args[i], out var portChar) || portChar is not (>= 'A' and <= 'D'))
            {
                Console.Error.WriteLine($"Bad '--port' argument: {args[i]}");
                showHelp = true;
                break;
            }

            port = (DreamcastPort)(portChar - 'A');
            break;
        case "--slots":
            i++;
            if (i >= args.Length)
            {
                Console.Error.WriteLine($"Missing '--slots' argument");
                showHelp = true;
                break;
            }

            slots = args[i] switch { "1" => ExpansionSlots.Slot1, "2" => ExpansionSlots.Slot2, "both" => ExpansionSlots.Slot1And2, _ => null };
            if (slots == null)
            {
                Console.Error.WriteLine($"Bad '--slots' argument: {args[i]}");
                showHelp = true;
            }

            break;
        case "--help":
            showHelp = true;
            break;
        default:
            if (args[i].StartsWith('-'))
            {
                Console.Error.WriteLine($"Unknown option: {args[i]}");
                showHelp = true;
                break;
            }

            gameFilePath = args[i];
            break;
    }
}

if (!integrated)
{
    if (tcpPort != null)
    {
        Console.Error.WriteLine($"Cannot specify --tcp-port without --integrated flag.");
        showHelp = true;
    }

    if (port != null)
    {
        Console.Error.WriteLine($"Cannot specify --port without --integrated flag.");
        showHelp = true;
    }

    if (slots != null)
    {
        Console.Error.WriteLine($"Cannot specify --slots without --integrated flag.");
        showHelp = true;
    }
}
else
{
    if (tcpPort == null)
    {
        Console.Error.WriteLine($"Must specify --tcp-port with --integrated flag.");
        showHelp = true;
    }

    if (port == null)
    {
        Console.Error.WriteLine($"Must specify --port with --integrated flag.");
        showHelp = true;
    }

    if (slots == null)
    {
        Console.Error.WriteLine($"Must specify --port with --integrated flag.");
        showHelp = true;
    }
}

if (showHelp)
{
    Console.WriteLine("""
        Usage: 'DreamPotato [vmu-or-vms-path] [options]'

        options:
          --integrated          Run in "integrated mode". See Hollycast documentation for more info.
          --tcp-port [PORT]     TCP port that DreamPotato should connect to. Required in integrated mode and otherwise invalid.
          --port [PORT]         Dreamcast port letter. (A-D). Required in integrated mode and otherwise invalid.
          --slots [SLOTS]       Dreamcast slot number. (1, 2, or 'both'). Required in integrated mode and otherwise invalid.
        """);
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        Console.ReadKey();

    return 1;
}

using var game = new Game1(gameFilePath, integrated ? (tcpPort!.Value, port!.Value, slots!.Value) : null);
game.Run();
return 0;
