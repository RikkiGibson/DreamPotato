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
    public Display Display => _cpu.Display;
    public string? LoadedFilePath { get; private set; }
    public (byte a, byte r, byte g, byte b) Color => _fileSystem.VmuColor;

    public bool HasUnsavedChanges => _cpu.HasUnsavedChanges;
    public event Action UnsavedChangesDetected
    {
        add => _cpu.UnsavedChangesDetected += value;
        remove => _cpu.UnsavedChangesDetected -= value;
    }

    public Vmu(MapleMessageBroker? mapleMessageBroker = null)
    {
        _cpu = new Cpu(mapleMessageBroker);
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
            throw new InvalidOperationException("Date should only be initialized at startup");

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

    public void Reset(DateTimeOffset? date)
    {
        _cpu.Reset();
        if (date.HasValue)
            InitializeDate(date.GetValueOrDefault());
    }

    public void LoadRom()
    {
        try
        {
            var filePath = Path.Combine(DataFolder, RomFileName);
            var bios = File.ReadAllBytes(filePath);
            if (bios.Length != Cpu.InstructionBankSize)
                throw new ArgumentException($"VMU ROM '{filePath}' needs to be exactly 64KB in size.", nameof(filePath));
            bios.AsSpan().CopyTo(_cpu.ROM);
        }
        catch (FileNotFoundException ex)
        {
            throw new InvalidOperationException($"'{RomFileName}' must be included in '{DataFolder}'.", ex);
        }
    }

    public void LoadNewVmu(DateTimeOffset date, bool autoInitializeRTCDate)
    {
        LoadedFilePath = null;
        Reset(autoInitializeRTCDate ? date : null);
        InitializeFlash(date);

        LoadedFilePath = null;
        _cpu.HasUnsavedChanges = false;
        _cpu.VmuFileWriteStream = null;
        _cpu.ResyncMapleOutbound();
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
        _cpu.HasUnsavedChanges = false;
        _cpu.VmuFileWriteStream = null;

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
        _cpu.HasUnsavedChanges = false;
        _cpu.VmuFileWriteStream = fileStream;
        _cpu.ResyncMapleOutbound();
    }

    public void SaveVmuAs(string filePath)
    {
        if (IsDocked)
            _cpu.ResyncMapleInbound();

        var fileStream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        fileStream.Write(_cpu.Flash);
        LoadedFilePath = filePath;
        _cpu.HasUnsavedChanges = false;
        _cpu.VmuFileWriteStream = fileStream;
        if (IsDocked)
            _cpu.ResyncMapleOutbound();
    }

    public bool IsServerConnected
    {
        get
        {
            return _cpu.MapleMessageBroker.IsConnected;
        }
    }

    /// <summary>Indicates whether the VMU is docked in the Dreamcast controller.</summary>
    public bool IsDocked => _cpu.SFRs.P7.DreamcastConnected;

    // Toggle the docked/ejected state.
    public void DockOrEject()
        => _cpu.ConnectDreamcast(connect: !IsDocked);

    // Dock or eject depending on a bool argument.
    public void DockOrEject(bool connect)
        => _cpu.ConnectDreamcast(connect);

    public static string DataFolder => Path.Combine(AppContext.BaseDirectory, "Data");

    public DreamcastSlot DreamcastSlot { get => _cpu.DreamcastSlot; set => _cpu.DreamcastSlot = value; }

    public const string RomFileName = "american_v1.05.bin";
    public const string SaveStateHeaderMessage = "DreamPotatoSaveState";
    public static readonly ReadOnlyMemory<byte> SaveStateHeaderBytes = Encoding.UTF8.GetBytes(SaveStateHeaderMessage);
    public const int SaveStateVersion = 3;

    private static string GetSaveStatePath(string loadedFilePath, string id)
    {
        var filePath = $"{Path.GetFileNameWithoutExtension(loadedFilePath)}_{id}.dpstate";
        return Path.Combine(DataFolder, filePath);
    }

    public bool SaveState(string id)
    {
        if (LoadedFilePath is null ||  GetSaveStatePath(LoadedFilePath, id) is not string filePath)
            return false;

        // TODO: it feels like it would be reasonable to zip/unzip the state implicitly.
        // But, 194k is also not that hefty.
        Debug.Assert(filePath.StartsWith(DataFolder, StringComparison.Ordinal));
        Directory.CreateDirectory(DataFolder);
        using var writeStream = File.Create(filePath);
        writeStream.Write(SaveStateHeaderBytes.Span);

        Span<byte> bytes = [0, 0, 0, 0];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, SaveStateVersion);
        writeStream.Write(bytes);
        _cpu.SaveState(writeStream);
        return true;
    }

    public bool SaveOopsFile() => SaveState("oops");
    public (bool success, string? error) LoadOopsFile() => LoadStateById("oops", saveOopsFile: false);

    public (bool success, string? error) LoadStateById(string id, bool saveOopsFile)
    {
        if (LoadedFilePath is null)
            return (success: false, error: "Cannot load state because no VMU/VMS file is currently open.");

        var filePath = GetSaveStatePath(LoadedFilePath, id);
        if (filePath is null)
            return (success: false, "");

        return LoadStateFromPath(filePath, saveOopsFile);
    }

    public (bool success, string? error) LoadStateFromPath(string filePath, bool saveOopsFile)
    {
        if (saveOopsFile)
        {
            if (!SaveOopsFile())
                return (false, "Cannot load state because oops file could not be saved.");
        }

        try
        {
            using var readStream = File.OpenRead(filePath);

            byte[] buffer = new byte[SaveStateHeaderBytes.Length];
            readStream.ReadExactly(buffer);
            if (!buffer.SequenceEqual(SaveStateHeaderBytes.Span))
                return (success: false, $"Unsupported save state. Bad header data: '{Encoding.UTF8.GetString(buffer)}'");

            byte[] versionBytes = new byte[4];
            readStream.ReadExactly(versionBytes);
            int version = BinaryPrimitives.ReadInt32LittleEndian(versionBytes);
            if (version != SaveStateVersion)
                return (success: false, $"Unsupported save state version '{version}'. Version '{SaveStateVersion}' needed.");

            _cpu.LoadState(readStream);
            return (true, null);
        }
        catch (FileNotFoundException)
        {
            return (false, $"Could not load state because '{filePath}' was not found.");
        }
    }
}