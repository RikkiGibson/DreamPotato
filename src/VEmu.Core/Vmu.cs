namespace VEmu.Core;

public class Vmu
{
    public readonly Cpu _cpu; // TODO: probably want to wrap everything a front-end would want to use thru here
    private readonly FileSystem _fileSystem;

    public Vmu()
    {
        _cpu = new Cpu();
        _fileSystem = new FileSystem(_cpu.FlashBank0, _cpu.FlashBank1);
    }

    public void LoadGameVms(string filePath)
    {
        var date = DateTimeOffset.Now;

        _cpu.Reset();
        _fileSystem.InitializeFileSystem(date);

        var gameData = File.ReadAllBytes(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        fileName = fileName.Substring(0, Math.Min(FileSystem.DirectoryEntryFileNameLength, fileName.Length));
        _fileSystem.WriteGameFile(gameData, fileName, date);
    }
}