using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

using DreamPotato.Core.SFRs;

namespace DreamPotato.Core;

public class Vmu
{
    public readonly Cpu _cpu; // TODO: probably want to wrap everything a front-end would want to use thru here
    private readonly FileSystem _fileSystem;
    public Audio Audio => _cpu.Audio;
    public string? LoadedFilePath { get; private set; }

    public Vmu()
    {
        _cpu = new Cpu();
        _cpu.Reset();
        _fileSystem = new FileSystem(_cpu.Flash);
    }

    public void InitializeFlash(DateTimeOffset date)
    {
        _fileSystem.InitializeFileSystem(date);
    }

    public void InitializeDate(DateTimeOffset date)
    {
        if (_cpu.Pc != 0 || _cpu.InstructionBank != InstructionBank.ROM)
            throw new Exception("Date should only be initialized at startup");

        _cpu.Pc = BuiltInCodeSymbols.BIOSAfterDateIsSet;

        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_Century_Bcd, FileSystem.ToBinaryCodedDecimal(date.Year / 100 % 100));
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_Year_Bcd, FileSystem.ToBinaryCodedDecimal(date.Year % 100));
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_Month_Bcd, FileSystem.ToBinaryCodedDecimal(date.Month));
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_Day_Bcd, FileSystem.ToBinaryCodedDecimal(date.Day));
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_Hour_Bcd, FileSystem.ToBinaryCodedDecimal(date.Hour));
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_Minute_Bcd, FileSystem.ToBinaryCodedDecimal(date.Minute));
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_Second_Bcd, FileSystem.ToBinaryCodedDecimal(date.Second));
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_Year_Msb, (byte)(date.Year >> 8 & 0xff));
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_Year_Lsb, (byte)(date.Year & 0xff));
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_Month, (byte)date.Month);
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_Day, (byte)date.Day);
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_Hour, (byte)date.Hour);
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_Minute, (byte)date.Minute);
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_Second, (byte)date.Second);
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_HalfSecond, (byte)(date.Millisecond >= 500 ? 1 : 0));
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_LeapYear, (byte)(DateTime.IsLeapYear(date.Year) ? 1 : 0));
        _cpu.Memory.Write(BuiltInRamSymbols.DateTime_DateSet, 0xff);

        // Following SFR writes are based on examining memory state from manual date initialization.
        _cpu.SFRs.Ie = new Ie { PriorityControl0 = true, PriorityControl1 = true, MasterInterruptEnable = true };
        _cpu.SFRs.Ip = new Ip { Int3_BaseTimer = true };
        _cpu.SFRs.Ocr = new Ocr(0xA3);
        _cpu.SFRs.T1Lc = 0xff;
        _cpu.SFRs.T1L = 0xff;
        _cpu.SFRs.Mcr = 0x9;
        _cpu.SFRs.Cnr = 0x5;
        _cpu.SFRs.Tdr = 0x20;
        _cpu.SFRs.P1 = new P1(0xC0);
        _cpu.SFRs.P1Ddr = 0xC0;
        _cpu.SFRs.P3Int = new P3Int { Continuous = true, Enable = true };
        _cpu.SFRs.Write(0x51, 0x20);
        _cpu.SFRs.FPR = new FPR();
        _cpu.SFRs.Write(0x55, 0xFF);
        _cpu.SFRs.Vsel = new Vsel { Ince = true };
        _cpu.SFRs.Btcr = new Btcr(0x79);
    }

    public void LoadGameVms(string filePath, DateTimeOffset? date)
    {
        if (!filePath.EndsWith(".vms", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"VMS file '{filePath}' must have .vms extension.", nameof(filePath));

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > Cpu.InstructionBankSize)
            throw new ArgumentException($"VMS file '{filePath}' must be 64KB or smaller to be loaded.", nameof(filePath));

        _cpu.Reset();
        if (date.HasValue)
            InitializeDate(date.GetValueOrDefault());

        var fileSystemDate = date ?? DateTime.Now;
        _fileSystem.InitializeFileSystem(fileSystemDate);

        var gameData = File.ReadAllBytes(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        fileName = fileName.Substring(0, Math.Min(FileSystem.DirectoryEntryFileNameLength, fileName.Length));
        _fileSystem.WriteGameFile(gameData, fileName, fileSystemDate);
        LoadedFilePath = filePath;

        _cpu.ResyncMapleOutbound();
    }

    public void LoadVmu(string filePath, DateTimeOffset? date)
    {
        // TODO: loading a wrong file type should just show a toast or something, not crash the emu.
        if (!filePath.EndsWith(".vmu", StringComparison.OrdinalIgnoreCase) && !filePath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"VMU file '{filePath}' must have .vmu or .bin extension.", nameof(filePath));

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length != Cpu.InstructionBankSize * 2)
            throw new ArgumentException($"VMU file '{filePath}' needs to be exactly 128KB in size.", nameof(filePath));

        // NB: lifetime of the VMU file stream is managed by _cpu.
        var fileStream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

        _cpu.Reset();
        if (date.HasValue)
            InitializeDate(date.GetValueOrDefault());

        fileStream.ReadExactly(_cpu.Flash);
        LoadedFilePath = filePath;
        _cpu.VmuFileWriteStream = fileStream;
        _cpu.ResyncMapleOutbound();
    }

    public void SaveVmuAs(string filePath)
    {
        var fileStream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        fileStream.Write(_cpu.Flash);
        LoadedFilePath = filePath;
        _cpu.VmuFileWriteStream = fileStream;
    }

    public void StartMapleServer()
    {
        _cpu.MapleMessageBroker.StartServer();
    }

    public bool IsServerConnected
    {
        get
        {
            return _cpu.MapleMessageBroker.IsConnected;
        }
    }

    public bool IsEjected => !_cpu.SFRs.P7.DreamcastConnected;

    // Toggle the inserted/ejected state.
    public void InsertOrEject()
    {
        _cpu.ConnectDreamcast(connect: IsEjected);
    }

    public static string DataFolder => Path.Combine(AppContext.BaseDirectory, "Data");
    public const string SaveStateHeaderMessage = "DreamPotatoSaveState";
    public static readonly ReadOnlyMemory<byte> SaveStateHeaderBytes = Encoding.UTF8.GetBytes(SaveStateHeaderMessage);
    public const int SaveStateVersion = 2;

    private string GetSaveStatePath(string id)
    {
        if (string.IsNullOrEmpty(LoadedFilePath))
            throw new InvalidOperationException();

        var filePath = $"{Path.GetFileNameWithoutExtension(LoadedFilePath)}_{id}.dpstate";
        return Path.Combine(DataFolder, filePath);
    }

    public void SaveState(string id)
    {
        // TODO: it feels like it would be reasonable to zip/unzip the state implicitly.
        // But, 194k is also not that hefty.
        var filePath = GetSaveStatePath(id);
        Debug.Assert(filePath.StartsWith(DataFolder, StringComparison.Ordinal));
        Directory.CreateDirectory(DataFolder);
        using var writeStream = File.Create(filePath);
        writeStream.Write(SaveStateHeaderBytes.Span);

        Span<byte> bytes = [0, 0, 0, 0];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, SaveStateVersion);
        writeStream.Write(bytes);
        _cpu.SaveState(writeStream);
    }

    public void LoadStateById(string id)
    {
        var filePath = GetSaveStatePath(id);
        LoadStateFromPath(filePath);
    }

    public void LoadStateFromPath(string filePath)
    {
        // TODO: before overwriting current state, save it to an oops file
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