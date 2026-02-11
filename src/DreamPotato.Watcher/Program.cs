using System.Diagnostics;
using System.Text.RegularExpressions;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: DreamPotato.Watcher <input.s> <output.vms> [--once]");
    return 1;
}

var inputFile = Path.GetFullPath(args[0]);
var outputFile = Path.GetFullPath(args[1]);
var runOnce = args.Length > 2 && args[2] == "--once";

if (!File.Exists(inputFile))
{
    Console.Error.WriteLine($"Input file not found: {inputFile}");
    return 1;
}

// Run build
var exitCode = RunWaterbear(inputFile, outputFile);

if (runOnce)
{
    return exitCode;
}

var directory = Path.GetDirectoryName(inputFile)!;
var fileName = Path.GetFileName(inputFile);

Console.WriteLine($"Watching {inputFile}...");

using var watcher = new FileSystemWatcher(directory)
{
    Filter = fileName,
    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
    EnableRaisingEvents = true
};

var lastRun = DateTime.MinValue;
var debounceMs = 200;

watcher.Changed += (sender, e) =>
{
    // Debounce rapid changes
    var now = DateTime.Now;
    if ((now - lastRun).TotalMilliseconds < debounceMs)
        return;
    lastRun = now;

    Console.WriteLine($"[{now:HH:mm:ss}] File changed, assembling...");
    RunWaterbear(inputFile, outputFile);
};

Console.WriteLine("Press Ctrl+C to stop watching.");

// Keep running until cancelled
var exitEvent = new ManualResetEvent(false);
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    exitEvent.Set();
};
exitEvent.WaitOne();

return 0;

static int RunWaterbear(string inputFile, string outputFile)
{
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = "waterbear",
            Arguments = $"assemble \"{inputFile}\" --output \"{outputFile}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            Console.Error.WriteLine("Failed to start waterbear");
            return 1;
        }

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // Parse and reformat waterbear errors for VS Code problem matcher
        // Format: file(line,col): error: message
        var combined = stdout + stderr;
        var lines = combined.Split('\n', StringSplitOptions.None);

        string? currentMessage = null;
        var locationPattern = new Regex(@"^(.+):(\d+):(\d+)(?:-(\d+))?$");
        var hasErrors = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            // Capture error message (lines starting with ✘ or similar)
            if (line.StartsWith("✘") || line.StartsWith("×") || line.StartsWith("[ERROR]"))
            {
                currentMessage = line.TrimStart('✘', '×', ' ').Trim();
                if (currentMessage.StartsWith("[ERROR]"))
                    currentMessage = currentMessage.Substring(7).Trim();
            }
            // Match location line and emit formatted error
            else if (locationPattern.Match(line) is { Success: true } match)
            {
                var file = match.Groups[1].Value;
                var lineNum = int.Parse(match.Groups[2].Value);
                var col = int.Parse(match.Groups[3].Value) + 1;
                var endCol = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) + 1 : col;
                var msg = currentMessage ?? "Assembly error";

                // Output in VS Code friendly format: file(line,col,line,endCol): error: message
                Console.WriteLine($"{file}({lineNum},{col},{lineNum},{endCol}): error: {msg}");
                currentMessage = null;
                hasErrors = true;
            }
        }

        if (hasErrors)
        {
            Console.WriteLine("Build failed.");
            return 1;
        }
        else
        {
            Console.WriteLine("Build succeeded.");
            return 0;
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error running waterbear: {ex.Message}");
        return 1;
    }
}
