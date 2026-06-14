using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

using DreamPotato.Core.SFRs;
using DreamPotato.Core.Waterbear;

namespace DreamPotato.Core;

public class Vmu
{
    public readonly Cpu _cpu; // TODO: probably want to wrap everything a front-end would want to use thru here
    private FileSystem FileSystem => _cpu.FileSystem;
    public Audio Audio => _cpu.Audio;
    public Display Display => _cpu.Display;
    public string? LoadedPath => _cpu.FileSystem.LoadedPath;
    public (byte a, byte r, byte g, byte b) Color => FileSystem.VmuColor;

    public bool HasUnsavedChanges => _cpu.FileSystem.HasUnsavedChanges;
    public event Action UnsavedChangesDetected
    {
        add => _cpu.FileSystem.UnsavedChangesDetected += value;
        remove => _cpu.FileSystem.UnsavedChangesDetected -= value;
    }

    public event Action<string> OpenFileRequested
    {
        add => _cpu.FileSystem.OpenFileRequested += value;
        remove => _cpu.FileSystem.OpenFileRequested += value;
    }

    public Vmu(MapleMessageBroker? mapleMessageBroker = null)
    {
        _cpu = new Cpu(mapleMessageBroker);
        _cpu.Reset();
    }

    public void InitializeFlash(DateTimeOffset date)
    {
        FileSystem.InitializeFileSystem(date);
    }

    public void InitializeRTCDate(DateTimeOffset date)
    {
        if (_cpu.Pc != 0 || _cpu.CurrentInstructionBankId != InstructionBank.ROM)
            throw new InvalidOperationException("Date should only be initialized at startup");

        _cpu.Pc = BuiltInCodeSymbols.BIOSAfterDateIsSet;

        var ramBank0 = _cpu.Memory.Direct_AccessMainRam0();
        ramBank0[BuiltInRamSymbols.DateTime_Century_Bcd] = FileSystem.ToBinaryCodedDecimal(date.Year / 100 % 100);
        ramBank0[BuiltInRamSymbols.DateTime_Year_Bcd] = FileSystem.ToBinaryCodedDecimal(date.Year % 100);
        ramBank0[BuiltInRamSymbols.DateTime_Month_Bcd] = FileSystem.ToBinaryCodedDecimal(date.Month);
        ramBank0[BuiltInRamSymbols.DateTime_Day_Bcd] = FileSystem.ToBinaryCodedDecimal(date.Day);
        ramBank0[BuiltInRamSymbols.DateTime_Hour_Bcd] = FileSystem.ToBinaryCodedDecimal(date.Hour);
        ramBank0[BuiltInRamSymbols.DateTime_Minute_Bcd] = FileSystem.ToBinaryCodedDecimal(date.Minute);
        ramBank0[BuiltInRamSymbols.DateTime_Second_Bcd] = FileSystem.ToBinaryCodedDecimal(date.Second);
        ramBank0[BuiltInRamSymbols.DateTime_Year_Msb] = (byte)(date.Year >> 8 & 0xff);
        ramBank0[BuiltInRamSymbols.DateTime_Year_Lsb] = (byte)(date.Year & 0xff);
        ramBank0[BuiltInRamSymbols.DateTime_Month] = (byte)date.Month;
        ramBank0[BuiltInRamSymbols.DateTime_Day] = (byte)date.Day;
        ramBank0[BuiltInRamSymbols.DateTime_Hour] = (byte)date.Hour;
        ramBank0[BuiltInRamSymbols.DateTime_Minute] = (byte)date.Minute;
        ramBank0[BuiltInRamSymbols.DateTime_Second] = (byte)date.Second;
        ramBank0[BuiltInRamSymbols.DateTime_HalfSecond] = (byte)(date.Millisecond >= 500 ? 1 : 0);
        ramBank0[BuiltInRamSymbols.DateTime_LeapYear] = (byte)(DateTime.IsLeapYear(date.Year) ? 1 : 0);
        ramBank0[BuiltInRamSymbols.DateTime_DateSet] = 0xff;

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

    public void Reset(DateTimeOffset? rtcDate)
    {
        _cpu.Reset();
        if (rtcDate.HasValue)
            InitializeRTCDate(rtcDate.GetValueOrDefault());
    }

    public void LoadRom()
    {
        try
        {
            var filePath = RomFilePath;
            var bios = File.ReadAllBytes(filePath);
            if (bios.Length != Cpu.InstructionBankSize)
                throw new InvalidOperationException($"VMU ROM '{filePath}' needs to be exactly 64KB in size.");
            bios.AsSpan().CopyTo(_cpu.ROM);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException($"'{RomFileName}' must be included in '{UserDataFolder}'.", ex);
        }
    }

    public void LoadNewVmu(DateTimeOffset date, bool autoInitializeRTCDate)
    {
        Reset(autoInitializeRTCDate ? date : null);

        FileSystem.SetHostFileInfo(loadedPath: null, vmuFileWriteStream: null);
        FileSystem.InitializeFileSystem(date);
        _cpu.ResyncMapleOutbound();
        _cpu.LazyDebugInfo?.ClearFlash();
    }

    public void LoadGameVms(string filePath, DateTimeOffset date, bool autoInitializeRTCDate)
    {
        // TODO2: use info from a valid companion vmi file if present
        if (!filePath.EndsWith(".vms", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"VMS file '{filePath}' must have .vms extension.", nameof(filePath));

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > Cpu.InstructionBankSize)
            throw new ArgumentException($"VMS file '{filePath}' must be 64KB or smaller to be loaded.", nameof(filePath));

        Reset(autoInitializeRTCDate ? date : null);

        FileSystem.SetHostFileInfo(filePath, vmuFileWriteStream: null);
        FileSystem.InitializeFileSystem(date);

        var gameData = File.ReadAllBytes(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        fileName = fileName.Substring(0, Math.Min(DirectoryEntry.FileNameLength, fileName.Length));
        if (FileSystem.TryWriteGameFile(gameData, fileName, FileSystem.Encoding.GetBytes(fileName), date, FileCopyProtection.NotCopyProtected) is (false, var error))
            throw new InvalidOperationException(error);

        _cpu.LazyDebugInfo?.ClearFlash();
        _cpu.LazyDebugInfo?.GetBankInfo(InstructionBank.FlashBank0).WaterbearInfo = GetWaterbearInfo(filePath);

        _cpu.ResyncMapleOutbound();
    }

    public void LoadVmu(string filePath, DateTimeOffset? rtcDate)
    {
        // TODO: loading a wrong file type should just show a toast or something, not crash the emu.
        if (!filePath.EndsWith(".vmu", StringComparison.OrdinalIgnoreCase) && !filePath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"VMU file '{filePath}' must have .vmu or .bin extension.", nameof(filePath));

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length != Cpu.InstructionBankSize * 2)
            throw new ArgumentException($"VMU file '{filePath}' needs to be exactly 128KB in size.", nameof(filePath));

        // NB: lifetime of the VMU file stream is managed by _cpu.
        var fileStream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

        Reset(rtcDate);

        _cpu.FileSystem.SetHostFileInfo(filePath, fileStream);
        _cpu.LazyDebugInfo?.ClearFlash();
        fileStream.ReadExactly(_cpu.Flash);
        _cpu.ResyncMapleOutbound();
    }

    public (bool ok, string? error) LoadVmsFolder(string folderPath, DateTimeOffset date, bool autoInitializeRtcDate)
    {
        var folderInfo = new DirectoryInfo(folderPath);
        if (!folderInfo.Exists)
            throw new ArgumentException(null, nameof(folderPath));

        Reset(autoInitializeRtcDate ? date : null);
        _cpu.FileSystem.SetHostFileInfo(folderPath, vmuFileWriteStream: null);
        Array.Clear(_cpu.Flash);
        if (FileSystem.TryInitializeFolder(sourceDirectory: folderInfo, fallbackDate: date) is (false, var error))
            return (false, error);

        _cpu.LazyDebugInfo?.ClearFlash();
        _cpu.ResyncMapleOutbound();

        return (true, null);
    }

    public void SaveVmuAsFile(string filePath)
    {
        if (IsDockedToDreamcast)
            _cpu.ResyncMapleInbound();

        var fileStream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        _cpu.FileSystem.SetHostFileInfo(filePath, fileStream);
        fileStream.Write(_cpu.Flash);
        if (IsDockedToDreamcast)
            _cpu.ResyncMapleOutbound();
    }

    public (bool ok, string? error) SaveVmuAsFolder(string folderPath)
    {
        if (IsDockedToDreamcast)
            _cpu.ResyncMapleInbound();

        var info = new DirectoryInfo(folderPath);
        if (!info.Exists)
            return (false, $"The folder '{info.Name}' does not exist.");

        FileSystem.ReadAllFiles(destDirectory: info);

        _cpu.FileSystem.SetHostFileInfo(folderPath, vmuFileWriteStream: null);
        if (IsDockedToDreamcast)
            _cpu.ResyncMapleOutbound();

        return (true, null);
    }

    public bool IsServerConnected
    {
        get
        {
            return _cpu.MapleMessageBroker.IsConnected;
        }
    }

    /// <summary>Indicates whether the VMU is docked in the Dreamcast controller.</summary>
    public bool IsDockedToDreamcast => _cpu.SFRs.P7.DreamcastConnected;

    /// <summary>Indicates whether the VMU is connected to another VMU for serial I/O.</summary>
    public bool IsOtherVmuConnected => _cpu.SFRs.P7.VmuConnected;

    // Toggle the docked/ejected state.
    public void DockOrEjectToDreamcast()
        => DockOrEjectToDreamcast(dock: !IsDockedToDreamcast);

    // Dock or eject depending on a bool argument.
    public void DockOrEjectToDreamcast(bool dock)
    {
        if (IsOtherVmuConnected)
        {
            Debug.Assert(!IsDockedToDreamcast);
            _cpu.DisconnectVmu();
        }

        _cpu.ConnectDreamcast(dock);
    }

    public void ConnectOrDisconnectVmu(Vmu other)
    {
        if (IsDockedToDreamcast)
        {
            Debug.Assert(!IsOtherVmuConnected);
            _cpu.ConnectDreamcast(connect: false);
        }

        if (other.IsDockedToDreamcast)
        {
            Debug.Assert(!other.IsOtherVmuConnected);
            other._cpu.ConnectDreamcast(connect: false);
        }

        if (IsOtherVmuConnected)
            _cpu.DisconnectVmu();
        else
            _cpu.ConnectVmu(other._cpu);
    }

    public static string EmbeddedDataFolder => IsMacAppBundle
        ? Path.Combine(AppContext.BaseDirectory, "..", "Resources", "Data")
        : Path.Combine(AppContext.BaseDirectory, "Data");

    private static bool IsLinuxAppImage => OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("APPIMAGE") != null;
    private static bool IsMacAppBundle => OperatingSystem.IsMacOS() && AppContext.BaseDirectory.Contains(".app/Contents/", StringComparison.Ordinal);
    private static bool UseNonEmbeddedUserDataFolder => IsLinuxAppImage || IsMacAppBundle;

    public static string UserDataRootFolder => UseNonEmbeddedUserDataFolder
        ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        : AppContext.BaseDirectory;

    public static string UserDataFolder => UseNonEmbeddedUserDataFolder
        ? Path.Combine(UserDataRootFolder, "DreamPotato")
        : EmbeddedDataFolder;

    public DreamcastSlot DreamcastSlot { get => _cpu.DreamcastSlot; set => _cpu.DreamcastSlot = value; }
    public DebugInfo GetOrCreateDebugInfo()
    {
        if (_cpu.LazyDebugInfo is { } existingDebugInfo)
        {
            return existingDebugInfo;
        }

        var debugInfo = _cpu.InitializeDebugInfo();
        debugInfo.GetBankInfo(InstructionBank.ROM).WaterbearInfo = GetWaterbearInfo(RomFilePath);
        debugInfo.GetBankInfo(InstructionBank.FlashBank0).WaterbearInfo = GetWaterbearInfo(LoadedPath);
        return debugInfo;
    }

    private WB.DebugInfo? GetWaterbearInfo(string? filePath)
    {
        if (filePath is null)
            return null;

        var debugInfoPath = $"{filePath}.debug.json";
        if (!File.Exists(debugInfoPath))
            return null;

        try
        {
            using var fileStream = File.OpenRead(debugInfoPath);
            var waterbearInfo = JsonSerializer.Deserialize(fileStream, WaterbearJsonSerializerContext.Default.DebugInfo);
            if (waterbearInfo?.Version != "1")
                return null;

            return waterbearInfo;
        }
        catch (Exception ex)
        {
            _cpu.Logger.LogError(ex.Message);
            return null;
        }
    }

    public DebugInfo? LazyDebugInfo => _cpu.LazyDebugInfo;

    public const string RomFileName = "american_v1.05.bin";
    public const string SaveStateHeaderMessage = $"DreamPotatoSaveStateV{SaveStateVersion}";
    public const string SaveStateVersion = "6";
    public string RomFilePath => Path.Combine(EmbeddedDataFolder, RomFileName);

    public static string GetSaveStatePath(string loadedFilePath, string id)
    {
        var filePath = $"{Path.GetFileNameWithoutExtension(loadedFilePath)}_{id}.dpstate";
        return Path.Combine(UserDataFolder, "SaveStates", filePath);
    }

    public bool SaveState(string id)
    {
        if (LoadedPath is null || GetSaveStatePath(LoadedPath, id) is not string filePath)
            return false;

        if (IsOtherVmuConnected)
           return false;

        var directory = Path.GetDirectoryName(filePath);
        Debug.Assert(directory != null && directory.StartsWith(UserDataFolder, StringComparison.Ordinal));
        Directory.CreateDirectory(directory);
        using var fileStream = File.Create(filePath);
        using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create);
        zipArchive.Comment = SaveStateHeaderMessage;

        // Save thumbnail
        var thumbnailBytes = Display.GetBytes();
        using (var thumbnailStream = zipArchive.CreateEntry(SaveState_ThumbnailFile).Open())
        {
            thumbnailStream.Write(thumbnailBytes);
        }

        using (var cpuStateStream = zipArchive.CreateEntry(SaveState_CpuStateFile).Open())
        {
            _cpu.SaveState(cpuStateStream);
        }

        return true;
    }

    public bool SaveOopsFile() => SaveState("oops");
    public (bool success, string? error) LoadOopsFile() => LoadStateById("oops", saveOopsFile: false);

    public (bool success, string? error) LoadStateById(string id, bool saveOopsFile)
    {
        if (LoadedPath is null)
            return (success: false, error: "Cannot load state because no VMU/VMS file is currently open.");

        var filePath = GetSaveStatePath(LoadedPath, id);
        if (filePath is null)
            return (success: false, "");

        return LoadStateFromPath(filePath, saveOopsFile);
    }

    public const string SaveState_ThumbnailFile = "Thumbnail.bin";
    public const string SaveState_CpuStateFile = "CpuState.bin";

    public (bool success, string? error) LoadStateFromPath(string filePath, bool saveOopsFile)
    {
        if (IsOtherVmuConnected)
            return (false, "Cannot load state while connected to other VMU.");

        if (saveOopsFile)
        {
            if (!SaveOopsFile())
                return (false, "Cannot load state because oops file could not be saved.");
        }

        try
        {
            using var readStream = File.OpenRead(filePath);
            using var zipArchive = new ZipArchive(readStream);
            if (zipArchive.Comment != SaveStateHeaderMessage)
                return (success: false, $"Outdated '{zipArchive.Comment}' is not supported. Expected '{SaveStateHeaderMessage}'.");

            if (zipArchive.GetEntry(SaveState_CpuStateFile) is not { } cpuStateEntry)
                return (success: false, $"'{SaveState_CpuStateFile}' not found.");

            using var cpuStateStream = cpuStateEntry.Open();
            _cpu.LoadState(cpuStateStream);
            return (true, null);
        }
        catch (FileNotFoundException)
        {
            return (false, $"Could not load state because '{filePath}' was not found.");
        }
        catch (InvalidDataException)
        {
            return (false, $"Invalid or outdated save state: '{filePath}'");
        }
    }

    public bool PollFileSystem(DateTimeOffset now)
    {
        if (LoadedPath is null)
            return false;

        var vmsFolder = new DirectoryInfo(LoadedPath);
        if (!vmsFolder.Exists)
            // Either a folder is not currently open (i.e. a file is open instead), or the folder was deleted/renamed.
            return false;

        if (FileSystem.ShouldFlushToFolder(now))
        {
            if (IsDockedToDreamcast)
                _cpu.ResyncMapleInbound();

            FileSystem.FlushToFolder(vmsFolder);
            return true;
        }

        return false;
    }

    public void FlushFileSystem()
    {
        if (LoadedPath is null)
            return;

        var vmsFolder = new DirectoryInfo(LoadedPath);
        if (!vmsFolder.Exists)
            // Either a folder is not currently open (i.e. a file is open instead), or the folder was deleted/renamed.
            return;

        if (IsDockedToDreamcast)
            _cpu.ResyncMapleInbound();

        FileSystem.FlushToFolder(vmsFolder);
    }
}