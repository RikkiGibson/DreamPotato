using System.Buffers.Binary;
using System.Text;

namespace VEmu.Core;

public class Vmu
{
    public readonly Cpu _cpu; // TODO: probably want to wrap everything a front-end would want to use thru here
    private readonly FileSystem _fileSystem;
    public Audio Audio => _cpu.Audio;
    public string? LoadedFilePath { get; private set; }

    public Vmu()
    {
        _cpu = new Cpu();
        _fileSystem = new FileSystem(_cpu.FlashBank0, _cpu.FlashBank1);
    }

    public void LoadGameVms(string filePath)
    {
        if (!filePath.EndsWith(".vms", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"VMS file '{filePath}' must have .vms extension.", nameof(filePath));

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > Cpu.InstructionBankSize)
            throw new ArgumentException($"VMS file '{filePath}' must be 64KB or smaller to be loaded.", nameof(filePath));

        var date = DateTimeOffset.Now;

        _cpu.Reset();
        _fileSystem.InitializeFileSystem(date);

        var gameData = File.ReadAllBytes(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        fileName = fileName.Substring(0, Math.Min(FileSystem.DirectoryEntryFileNameLength, fileName.Length));
        _fileSystem.WriteGameFile(gameData, fileName, date);
        LoadedFilePath = filePath;
    }

    public void LoadVmu(string filePath)
    {
        if (!filePath.EndsWith(".vmu", StringComparison.OrdinalIgnoreCase) && !filePath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"VMU file '{filePath}' must have .vmu or .bin extension.", nameof(filePath));

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length != Cpu.InstructionBankSize * 2)
            throw new ArgumentException($"VMU file '{filePath}' needs to be exactly 128KB in size.", nameof(filePath));

        // NB: lifetime of the VMU file stream is managed by _cpu.
        var fileStream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

        _cpu.Reset();
        fileStream.ReadExactly(_cpu.FlashBank0);
        fileStream.ReadExactly(_cpu.FlashBank1);
        LoadedFilePath = filePath;
        _cpu.VmuFileWriteStream = fileStream;

        // TODO: perhaps detect if no game is present on the VMU, and default to BIOS in that case.
        // also detect a bad/missing filesystem.
    }

    public const string SaveStateHeaderMessage = "DreamPotatoSaveState";
    public static readonly ReadOnlyMemory<byte> SaveStateHeaderBytes = Encoding.UTF8.GetBytes(SaveStateHeaderMessage);
    public const int SaveStateVersion = 1;

    private string GetSaveStatePath(string id)
    {
        if (string.IsNullOrEmpty(LoadedFilePath))
            throw new InvalidOperationException();

        var filePath = $"{Path.GetFileNameWithoutExtension(LoadedFilePath)}{id}.dpstate";
        return filePath;
    }

    public void SaveState(string id)
    {
        // TODO: it feels like it would be reasonable to zip/unzip the state implicitly.
        // But, 194k is also not that hefty.
        var filePath = GetSaveStatePath(id);
        using var writeStream = File.Create(filePath);
        writeStream.Write(SaveStateHeaderBytes.Span);

        Span<byte> bytes = [0, 0, 0, 0];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, SaveStateVersion);
        writeStream.Write(bytes);
        _cpu.SaveState(writeStream);
    }

    public void LoadState(string id)
    {
        // TODO: before overwriting current state, save it to an oops file
        var filePath = GetSaveStatePath(id);
        try
        {
            using var readStream = File.OpenRead(filePath);

            byte[] buffer = new byte[SaveStateHeaderBytes.Length];
            readStream.ReadExactly(buffer);
            if (!buffer.SequenceEqual(SaveStateHeaderBytes.Span))
                throw new InvalidOperationException($"Unsupported save state. Bad header data: '{Encoding.UTF8.GetString(buffer)}'");

            byte[] versionBytes = new byte[4];
            readStream.ReadExactly(versionBytes);
            int version = BinaryPrimitives.ReadInt32LittleEndian(versionBytes);
            if (version != SaveStateVersion)
                throw new InvalidOperationException($"Unsupported save state version '{version}'");

            _cpu.LoadState(readStream);

            // TODO: it seems like loading the state, when the associated file is a '.vmu'/.bin', should also overwrite the vmu file on disk.
            // The emu itself only writes the bytes indicated by the STF instructions. So if we don't do that, we could end up in inconsistent state.
        }
        catch (FileNotFoundException)
        {
            _cpu.Logger.LogError($"Could not load state at '{filePath}' because it was not found.");
        }
    }
}