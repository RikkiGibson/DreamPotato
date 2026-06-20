using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

using Microsoft.Win32.SafeHandles;

namespace DreamPotato.Core;

/// <summary>
/// Performs file system operations on flash memory.
/// NOTE: does not own its state. The same flash memory buffers used by <see cref="Cpu"/> are shared here.
/// </summary>
internal class FileSystem
{
    private readonly byte[] flash;

    /// <summary>
    /// Records the IDs of FAT blocks whose contents were modified.
    /// Later, these are used to decide which files to flush to a VMS folder.
    /// </summary>
    private readonly HashSet<ushort> _changedBlockIds = [];

    /// <summary>
    /// Copy of the directory table. Each time we open a file, or after we flush I/O, we update this copy.
    /// When we flush, we compare <see cref="_directoryMirror"/> and <see cref="flash"/>, to see which files were added/removed/modified.
    /// </summary>
    private readonly byte[] _directoryMirror = new byte[DirectoryTableSizeBlocks * BlockSize];

    /// <summary>Records the time at which VMS folder I/O should be flushed.</summary>
    private DateTimeOffset _flushDeadline = DateTimeOffset.MaxValue;

    internal event Action? UnsavedChangesDetected;
    internal bool HasUnsavedChanges
    {
        get;
        private set
        {
            var isNewUnsavedChanges = !field && value;
            field = value;
            if (isNewUnsavedChanges)
                UnsavedChangesDetected?.Invoke();
        }
    }

    public event Action<string>? OpenFileRequested;

    public void RequestOpenFile(string path) => OpenFileRequested?.Invoke(path);

    internal string? LoadedPath { get; private set; }

    private FileStream? VmuFileWriteStream;

    internal SafeFileHandle? VmuFileHandle => VmuFileWriteStream?.SafeFileHandle;

    internal void SetHostFileInfo(string? loadedPath, FileStream? vmuFileWriteStream)
    {
        if (LoadedPath is not null && new DirectoryInfo(LoadedPath) is { Exists: true } directoryInfo)
            FlushToFolder(directoryInfo);

        ResetFlushData();
        LoadedPath = loadedPath;
        VmuFileWriteStream?.Dispose();
        VmuFileWriteStream = vmuFileWriteStream;
        HasUnsavedChanges = false;
    }

    public FileSystem(byte[] flash)
    {
        this.flash = flash;
        ResetFlushData();
    }

    private const int ShiftJisCodePage = 932;
    public static readonly Encoding Encoding = CodePagesEncodingProvider.Instance.GetEncoding(ShiftJisCodePage) ?? Encoding.ASCII;

    private const int VolumeSizeBytes = 128 * 1024; // 128kb

    // user region: where user visible files are stored.
    private const int UserRegionSizeBlocks = 200;
    internal const ushort UserRegionLastBlockId = UserRegionSizeBlocks - 1;

    // hidden region: extra storage which is used on-demand to store user files when blocks in the user region being to malfunction.
    private const int HiddenRegionSizeBlocks = BlocksCount - UserRegionSizeBlocks - DirectoryTableSizeBlocks - 1 /*root block*/ - 1 /*FAT size*/; // 41

    internal const int BlockSize = 0x200; // 512b
    internal const int BlockIdSize = 2;
    private const int BlocksCount = VolumeSizeBytes / BlockSize; // 256

    /// <summary>Used to store the root block contents in a vms folder</summary>
    internal const string RootBlockFilename = "fs_root.bin";
    internal const byte RootBlockId = BlocksCount - 1;
    internal const byte FATBlockId = BlocksCount - 2;
    internal const byte DirectoryTableLastBlockId = BlocksCount - 3;
    private const int DirectoryTableSizeBlocks = 13;
    internal const byte DirectoryTableFirstBlockId = DirectoryTableLastBlockId - DirectoryTableSizeBlocks + 1;

    private const int Magic = 0x55;

    const ushort FAT_Unallocated = 0xfffc;

    const ushort FAT_LastInFile = 0xfffa;

    public (byte a, byte r, byte g, byte b)? VmuColor => GetRootBlock().VmuColor;

    /// <summary>Reset data used for tracking folder changes to default state, i.e. no changes detected, no deadline, mirror up-to-date.</summary>
    private void ResetFlushData()
    {
        _changedBlockIds.Clear();
        _flushDeadline = DateTimeOffset.MaxValue;

        var newTable = flash.AsSpan(DirectoryTableFirstBlockId * BlockSize, length: DirectoryTableSizeBlocks * BlockSize);
        Debug.Assert(newTable.Length == _directoryMirror.Length);
        newTable.CopyTo(_directoryMirror);
    }

    public void InitializeFileSystem(DateTimeOffset date)
    {
        // Note that multi-byte values are little-endian encoded!
        Debug.Assert(flash.Length == Cpu.FlashSize);
        Array.Clear(flash);

        initializeRootBlock();
        initializeFAT();
        ResetFlushData();

        void initializeRootBlock()
        {
            var rootBlock = GetRootBlock();

            // 0x00-0x10: Magic
            rootBlock.RawData.Span[0..0x10].Fill(Magic);

            // 0x10-0x30: Volume label (VMU stores the color here).
            rootBlock.RawData.Span[RootBlock.UsingCustomColor] = 0x01;
            rootBlock.VmuColor = (a: 0x64, r: 0xff, g: 0xff, b: 0xff);
            rootBlock.RawData.Span[0x15..0x30].Clear();

            // 0x30-0x38: Timestamp
            rootBlock.TimestampDateTimeOffset = date;

            // 0x38-0x40: Reserved
            rootBlock.RawData.Span[0x38..0x40].Clear();

            // 0x40: Important FAT stuff
            rootBlock.VolumeLast = 0xff;
            rootBlock.Partition = 0;
            rootBlock.Root = RootBlockId; // Root block ID. This is the last block in the storage device.
            rootBlock.FATFirst = FATBlockId; // FAT First. First block containing the FAT data. i.e. indicating the sequences of blocks that comprise the contents of files.
            rootBlock.FATSize = 1; // FAT size. How many blocks does the FAT take up.
            rootBlock.DirLast = DirectoryTableLastBlockId; // DIR last. Last block of the Directory table, which holds file metadata
            Debug.Assert(DirectoryTableSizeBlocks == Math.Ceiling((double)UserRegionSizeBlocks * DirectoryEntry.Size / BlockSize));
            rootBlock.DirSize = DirectoryTableSizeBlocks; // DIR size. How many blocks does the Directory table take up.
            rootBlock.Icon = 0; // Icon (0-123). Used in the DC BIOS.
            rootBlock.Sort = 0; // Sort. Unused.
            rootBlock.HiddenFirst = UserRegionSizeBlocks; // Hidden First: Block ID of the first hidden region block.
            rootBlock.HiddenSize = HiddenRegionSizeBlocks; // Hidden Size.
            rootBlock.GameFirst = 0; // Game First: Block ID where a game file must start.
            rootBlock.GameSize = Cpu.InstructionBankSize / BlockSize; // Game Size: Largest size in blocks of a game file.

            // The rest of the root block is reserved
            rootBlock.RawData.Span[0x58..].Clear();
        }

        void initializeFAT()
        {
            var fatBlock = GetFATBlock();

            // mark all blocks unused to start
            for (int i = 0; i < FATBlock.Length; i++)
            {
                fatBlock[i] = FAT_Unallocated;
            }

            // mark root block as last in its file
            fatBlock[RootBlockId] = FAT_LastInFile;

            // mark FAT itself as last in its file
            fatBlock[FATBlockId] = FAT_LastInFile;

            // directory: mark as growing from end toward start of the volume.
            var directoryStartBlockId = DirectoryTableLastBlockId - DirectoryTableSizeBlocks + 1;
            for (int i = DirectoryTableLastBlockId; i > directoryStartBlockId; i--)
            {
                // Point to the previous block.
                fatBlock[i] = (ushort)(i - 1);
            }

            fatBlock[directoryStartBlockId] = FAT_LastInFile;
        }
    }

    internal static DateTimeOffset ReadBcdDate(ReadOnlySpan<byte> source)
    {
        if (source.Length < 8)
            throw new ArgumentException(null, nameof(source));

        var year = FromBinaryCodedDecimal(source[0]) * 100 + FromBinaryCodedDecimal(source[1]);
        var month = FromBinaryCodedDecimal(source[2]);
        var day = FromBinaryCodedDecimal(source[3]);
        var hour = FromBinaryCodedDecimal(source[4]);
        var minute = FromBinaryCodedDecimal(source[5]);
        var second = FromBinaryCodedDecimal(source[6]);
        // note: source[7] (day of week) is ignored as the other info will let us reconstitute that
        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
    }

    internal static DateTimeOffset ReadBinaryDate(ReadOnlySpan<byte> source)
    {
        if (source.Length < 8)
            throw new ArgumentException(null, nameof(source));

        var year = BinaryPrimitives.ReadUInt16LittleEndian(source[0..2]);
        var month = source[2];
        var day = source[3];
        var hour = source[4];
        var minute = source[5];
        var second = source[6];
        // note: source[7] (day of week) is ignored as the other info will let us reconstitute that
        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
    }

    internal static void WriteBcdDate(Span<byte> dest, DateTimeOffset date)
    {
        Debug.Assert(dest.Length >= 8);

        var year = date.Year;

        // Really? We're accounting for running this software in the year 10,000? Yep..
        dest[0x0] = ToBinaryCodedDecimal(year / 100 % 100); // century: first two digits of year.
        dest[0x1] = ToBinaryCodedDecimal(year % 100); // year: last two digits of year.
        dest[0x2] = ToBinaryCodedDecimal(date.Month); // month: 1(Jan)-12(Dec)
        dest[0x3] = ToBinaryCodedDecimal(date.Day); // day: 1-31
        dest[0x4] = ToBinaryCodedDecimal(date.Hour); // hour: 0-23
        dest[0x5] = ToBinaryCodedDecimal(date.Minute); // minute: 0-59
        dest[0x6] = ToBinaryCodedDecimal(date.Second); // second: 0-59
        dest[0x7] = (byte)date.DayOfWeek; // week_day: 0(Mon)-6(Sun)
    }

    internal static void WriteBinaryDate(Span<byte> dest, DateTimeOffset date)
    {
        Debug.Assert(dest.Length >= 8);

        BinaryPrimitives.WriteUInt16LittleEndian(dest[0..2], (ushort)date.Year);
        dest[0x2] = (byte)date.Month; // month: 1(Jan)-12(Dec)
        dest[0x3] = (byte)date.Day; // day: 1-31
        dest[0x4] = (byte)date.Hour; // hour: 0-23
        dest[0x5] = (byte)date.Minute; // minute: 0-59
        dest[0x6] = (byte)date.Second; // second: 0-59
        dest[0x7] = (byte)date.DayOfWeek; // week_day: 0(Mon)-6(Sun)
    }

    public (bool ok, string? errorMessage) TryWriteGameFileWithVmi(Stream vmsFile, string onDiskFileName, VmiInfo vmiInfo)
    {
        var copyProtection = vmiInfo.FileMode.HasFlag(VmuFileMode.CopyProtected) ? FileCopyProtection.CopyProtected : FileCopyProtection.NotCopyProtected;
        return TryWriteGameFile(vmsFile, onDiskFileName, onVmuFileName: vmiInfo.VmuFileName, vmiInfo.CreationDateTimeOffset, copyProtection);
    }

    public (bool ok, string? errorMessage) TryWriteGameFile(Stream vmsFile, string onDiskFileName, ReadOnlyMemory<byte> onVmuFileName, DateTimeOffset date, FileCopyProtection copyProtection)
    {
        var vmsLength = checked((int)(vmsFile.Length - vmsFile.Position));
        ArgumentOutOfRangeException.ThrowIfNegative(vmsLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vmsLength, Cpu.InstructionBankSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(onVmuFileName.Length, DirectoryEntry.FileNameLength);

        // Verify sufficient free space
        var fatBlock = GetFATBlock();
        var lastBlockId = (vmsLength + BlockSize - 1) / BlockSize - 1;
        for (int i = 0; i <= lastBlockId; i++)
        {
            if (fatBlock[i] != FAT_Unallocated)
            {
                return (false, $"{onDiskFileName}: Insufficient space");
            }
        }

        // Update FAT table indicating the game data starts at block 0 and grows toward the end
        for (int i = 0; i < lastBlockId; i++)
        {
            fatBlock[i] = (ushort)(i + 1);
        }

        fatBlock[lastBlockId] = FAT_LastInFile;

        // Game data itself must be written to start of bank 0
        vmsFile.ReadExactly(flash.AsSpan(0, vmsLength));

        // Create a directory entry
        var directoryEntry = EnumerateDirectoryTable().FirstOrDefault(entry => entry.Type == FileType.None);
        if (!directoryEntry.HasValue)
            return (false, $"{onDiskFileName}: The file system directory is full.");

        directoryEntry.Type = FileType.Game;
        directoryEntry.CopyProtection = copyProtection;
        directoryEntry.StartFAT = 0;
        onVmuFileName.CopyTo(directoryEntry.Name);
        directoryEntry.DateTimeOffset = date;
        directoryEntry.SizeInBlocks = (ushort)((vmsLength + BlockSize - 1) / BlockSize);
        directoryEntry.VmsHeaderBlockOffset = 1;

        return (true, null);
    }

    /// <summary>Read all the files from this file system to destDirectory.</summary>
    public (bool ok, string? error) TryReadAllFiles(DirectoryInfo destDirectory)
    {
        HashSet<string> allFilePaths = [];
        foreach (var directoryEntry in EnumerateDirectoryTable())
        {
            if (directoryEntry.Type == FileType.None)
                continue;

            // Note: we don't mind overwriting existing files on disk as part of this.
            // We just don't want the VMU's own file system containing duplicates.
            if (!allFilePaths.Add(directoryEntry.NameString))
                return (false, $"VMU contains duplicate file: '{directoryEntry.NameString}'");

            readFile(directoryEntry);
        }

        var rootBlock = GetBlock(RootBlockId);
        File.WriteAllBytes(Path.Combine(destDirectory.FullName, RootBlockFilename), rootBlock);
        return (true, null);

        void readFile(DirectoryEntry directoryEntry)
        {
            var outFileName = directoryEntry.NameString;
            var fatBlock = GetFATBlock();

            using var vmsFile = File.Open(Path.Combine(destDirectory.FullName, $"{outFileName}.vms"), FileMode.CreateNew);

            for (var blockId = directoryEntry.StartFAT;
                blockId != FAT_LastInFile;
                blockId = fatBlock[blockId])
            {
                var block = this.GetBlock(blockId);
                vmsFile.Write(block);
            }

            using var vmiFile = File.Open(Path.Combine(destDirectory.FullName, $"{outFileName}.vmi"), FileMode.CreateNew);
            var vmiInfo = CreateVmiInfo(directoryEntry);
            vmiFile.Write(vmiInfo.RawData.Span);
        }
    }

    private VmiInfo CreateVmiInfo(DirectoryEntry directoryEntry)
    {
        var vmsHeaderBlock = GetBlockMemory(directoryEntry.StartFAT + directoryEntry.VmsHeaderBlockOffset);
        var vmsHeaderInfo = new VmsHeaderInfo(vmsHeaderBlock[..VmsHeaderInfo.Size]);

        var vmiInfo = new VmiInfo(new byte[VmiInfo.Size]);
        vmsHeaderInfo.DreamcastDescription.CopyTo(vmiInfo.Description);
        "Generated by DreamPotato"u8.CopyTo(vmiInfo.Copyright.Span);
        vmiInfo.CreationDateTimeOffset = directoryEntry.DateTimeOffset;

        vmiInfo.Version = 0;
        vmiInfo.FileNumber = 1;

        // Directory entry name is truncated when copying
        directoryEntry.Name[..vmiInfo.VmsResourceName.Length].CopyTo(vmiInfo.VmsResourceName);
        directoryEntry.Name.CopyTo(vmiInfo.VmuFileName);

        vmiInfo.FileMode =
            (directoryEntry.CopyProtection == FileCopyProtection.CopyProtected ? VmuFileMode.CopyProtected : 0)
            | (directoryEntry.Type == FileType.Game ? VmuFileMode.Game : 0);

        vmiInfo.VmsFileSize = directoryEntry.SizeInBlocks * BlockSize;

        var resourceNameSpan = vmiInfo.VmsResourceName.Span;
        vmiInfo.Checksum.Span[0] = (byte)(resourceNameSpan[0] & 'S');
        vmiInfo.Checksum.Span[1] = (byte)(resourceNameSpan[1] & 'E');
        vmiInfo.Checksum.Span[2] = (byte)(resourceNameSpan[2] & 'G');
        vmiInfo.Checksum.Span[3] = (byte)(resourceNameSpan[3] & 'A');

        return vmiInfo;
    }

    public (bool ok, string? errorMessage) TryInitializeFromFolder(DirectoryInfo sourceDirectory, DateTimeOffset fallbackDate)
    {
        InitializeFileSystem(fallbackDate);

        // Replace the root block contents if present.
        // This will allow user VMU color, icon etc to be preserved
        var rootBlockPath = new FileInfo(Path.Combine(sourceDirectory.FullName, RootBlockFilename));
        if (rootBlockPath.Exists && rootBlockPath.Length == BlockSize)
        {
            using var rootBlockFile = rootBlockPath.OpenRead();
            rootBlockFile.ReadExactly(GetBlock(RootBlockId));
        }

        var (ok, error) = TryWriteAllFiles(sourceDirectory);
        ResetFlushData();
        return (ok, error);
    }

    private readonly struct VmiFileNameInfo(string vmuFileName, string onDiskFileName) : IEquatable<VmiFileNameInfo>
    {
        public readonly string VmuFileName = vmuFileName;
        public readonly string OnDiskFileName = onDiskFileName;

        public bool Equals(VmiFileNameInfo other) => VmuFileName == other.VmuFileName;
        public override bool Equals(object? obj) => obj is VmiFileNameInfo info && Equals(info);
        public override int GetHashCode() => VmuFileName.GetHashCode();
    }

    public (bool ok, string? errorMessage) TryWriteAllFiles(DirectoryInfo sourceDirectory)
    {
        if (TryWriteAllFilesCheckPreconditions(sourceDirectory) is (false, var error))
            return (false, error);

        // It's unlikely but still possible that the below call can fail, e.g. if we are racing with other stuff happening on the file system.
    }

    private (bool ok, string? errorMessage) TryWriteAllFilesCheckPreconditions(DirectoryInfo sourceDirectory)
    {
        var fatBlock = GetFATBlock();

        int freeBlocks = 0;
        for (var i = 0; i <= UserRegionLastBlockId; i++)
        {
            if (fatBlock[i] == FAT_Unallocated)
                freeBlocks++;
        }

        // A game can only be stored contiguously at the start of the volume
        int gameBlocks = 0;
        const int maxGameBlockSize = Cpu.InstructionBankSize / BlockSize; // 128
        for (var i = 0; i < maxGameBlockSize && fatBlock[i] == FAT_Unallocated; i++)
            gameBlocks++;

        int neededBlocks = 0;
        string? foundGameName = null;
        HashSet<VmiFileNameInfo> allVmuFileNames = [];
        foreach (var vmsFileInfo in sourceDirectory.EnumerateFiles("*.vms").OrderBy(f => f.Name))
        {
            if (vmsFileInfo.Length == 0)
                return (false, $"{vmsFileInfo.Name}: Cannot write an empty file");

            var vmiFileInfo = new FileInfo(Path.ChangeExtension(vmsFileInfo.FullName, ".vmi"));
            if (!vmiFileInfo.Exists)
                return (false, $"{vmsFileInfo.Name}: No matching .vmi found");

            if (vmiFileInfo.Length != VmiInfo.Size)
                return (false, $"{vmiFileInfo.Name}: Bad format");

            var vmiInfo = new VmiInfo(File.ReadAllBytes(vmiFileInfo.FullName));
            var vmiFileNameInfo = new VmiFileNameInfo(vmiInfo.VmuFileNameString, vmiFileInfo.Name);
            if (allVmuFileNames.TryGetValue(vmiFileNameInfo, out var existingNameInfo))
                return (false, $"VMU filename '{vmiFileNameInfo.VmuFileName}' is duplicated by '{existingNameInfo.OnDiskFileName}' and '{vmiFileNameInfo.OnDiskFileName}'.");

            allVmuFileNames.Add(vmiFileNameInfo);

            var sizeInBlocks = checked((int)vmsFileInfo.Length + BlockSize - 1) / BlockSize;
            neededBlocks += sizeInBlocks;

            if (vmiInfo.FileMode.HasFlag(VmuFileMode.Game))
            {
                if (foundGameName != null)
                    return (false, $"Cannot use multiple games in VMS folder: '{foundGameName}', '{vmiFileInfo.Name}'");

                foundGameName = vmiFileInfo.Name;
                var neededGameBlocks = (vmsFileInfo.Length + BlockSize - 1) / BlockSize;
                if (neededGameBlocks > gameBlocks)
                    return (false, $"Not enough space to store game {foundGameName}. {neededGameBlocks} are required but only {gameBlocks} blocks are available.");
            }
        }

        if (neededBlocks > freeBlocks)
            return (false, $"Not enough space to open {sourceDirectory.Name}. {neededBlocks} blocks are required but only {freeBlocks} blocks are available.");

        return (true, null);
    }

    /// <summary>Write the files from sourceDirectory to this file system.</summary>
    private (bool ok, string? errorMessage) TryWriteAllFilesCore(DirectoryInfo sourceDirectory)
    {
        string? foundGameName = null;
        HashSet<VmiFileNameInfo> allVmuFileNames = [];

        // Data (non-game) files are written from the end of the user region toward the start
        ushort currentDataBlockId = UserRegionLastBlockId;
        foreach (var vmsFileInfo in sourceDirectory.EnumerateFiles("*.vms").OrderBy(f => f.Name))
        {
            if (vmsFileInfo.Length == 0)
                return (false, $"{vmsFileInfo.Name}: Cannot write an empty file");

            var vmiFileInfo = new FileInfo(Path.ChangeExtension(vmsFileInfo.FullName, ".vmi"));
            if (!vmiFileInfo.Exists)
                return (false, $"{vmsFileInfo.Name}: No matching .vmi found");

            if (vmiFileInfo.Length != VmiInfo.Size)
                return (false, $"{vmiFileInfo.Name}: Bad format");

            var vmiInfo = new VmiInfo(File.ReadAllBytes(vmiFileInfo.FullName));
            var vmiFileNameInfo = new VmiFileNameInfo(vmiInfo.VmuFileNameString, vmiFileInfo.Name);
            if (allVmuFileNames.TryGetValue(vmiFileNameInfo, out var existingNameInfo))
                return (false, $"VMU filename '{vmiFileNameInfo.VmuFileName}' is duplicated by '{existingNameInfo.OnDiskFileName}' and '{vmiFileNameInfo.OnDiskFileName}'.");

            allVmuFileNames.Add(vmiFileNameInfo);
            using var vmsFile = vmsFileInfo.OpenRead();
            if (vmiInfo.FileMode.HasFlag(VmuFileMode.Game))
            {
                if (foundGameName != null)
                    return (false, $"Cannot use multiple games in VMS folder: '{foundGameName}', '{vmiFileInfo.Name}'");

                foundGameName = vmiFileInfo.Name;
                var copyProtection = vmiInfo.FileMode.HasFlag(VmuFileMode.CopyProtected) ? FileCopyProtection.CopyProtected : FileCopyProtection.NotCopyProtected;
                if (TryWriteGameFile(vmsFile, onDiskFileName: vmsFileInfo.Name, onVmuFileName: vmiInfo.VmuFileName, vmiInfo.CreationDateTimeOffset, copyProtection) is (false, var error))
                    return (false, error);

                continue;
            }

            if (TryWriteDataFile(ref currentDataBlockId, vmsFile, vmiInfo) is (false, var error1))
                return (false, error1);
        }

        return (true, null);
    }

    public (bool ok, string? error) TryWriteDataFile(ref ushort currentDataBlockId, Stream vmsFile, VmiInfo vmiInfo)
    {
        Debug.Assert(!vmiInfo.FileMode.HasFlag(VmuFileMode.Game));

        if (vmsFile.Length != vmiInfo.VmsFileSize)
            return (false, $"{vmiInfo.VmuFileName}: VMI expected the VMS file size to be {vmiInfo.VmsFileSize} but was actually {vmsFile.Length}");

        var directoryEntry = EnumerateDirectoryTable().FirstOrDefault(entry => entry.Type == FileType.None);
        if (!directoryEntry.HasValue)
            return (false, $"{vmiInfo.VmuFileName}: The file system directory is full.");

        var sizeInBlocks = (ushort)((vmsFile.Length + (BlockSize - 1)) / BlockSize);
        var fatBlock = GetFATBlock();

        // Scan to next free data block
        while (fatBlock[currentDataBlockId] != FAT_Unallocated)
        {
            if (currentDataBlockId == 0)
                return (false, $"{vmiInfo.VmuFileName}: Insufficient space on VMU");

            currentDataBlockId--;
        }

        ushort startFAT = currentDataBlockId;
        for (var i = 0; i < sizeInBlocks; i++)
        {
            var dataBlock = GetBlock(currentDataBlockId);
            vmsFile.ReadExactly(dataBlock);

            if (i == sizeInBlocks - 1)
            {
                fatBlock[currentDataBlockId] = FAT_LastInFile;
                break;
            }

            var prevDataBlockId = currentDataBlockId;

            // Scan to next free data block
            do
            {
                if (currentDataBlockId == 0)
                    return (false, $"{vmiInfo.VmuFileName}: Insufficient space on VMU");

                currentDataBlockId--;
            } while (fatBlock[currentDataBlockId] != FAT_Unallocated);

            fatBlock[prevDataBlockId] = currentDataBlockId;
        }

        directoryEntry.Type = FileType.Data;
        directoryEntry.CopyProtection = vmiInfo.FileMode.HasFlag(VmuFileMode.CopyProtected) ? FileCopyProtection.CopyProtected : FileCopyProtection.NotCopyProtected;
        directoryEntry.StartFAT = startFAT;

        Debug.Assert(vmiInfo.VmuFileName.Length == directoryEntry.Name.Length);
        vmiInfo.VmuFileName.CopyTo(directoryEntry.Name);

        directoryEntry.DateTimeOffset = vmiInfo.CreationDateTimeOffset;
        directoryEntry.SizeInBlocks = sizeInBlocks;

        // Note: if the vms doesn't match this convention, then we have no way of really knowing where its header is.
        // We might want to verify the header is in the expected location in 'ReadAllFiles'.
        directoryEntry.VmsHeaderBlockOffset = 0;
        return (true, null);
    }

    internal Span<byte> GetBlock(int blockId)
    {
        return flash.AsSpan(blockId * BlockSize, length: BlockSize);
    }

    internal Memory<byte> GetBlockMemory(int blockId)
    {
        return flash.AsMemory(blockId * BlockSize, length: BlockSize);
    }

    private FATBlock GetFATBlock() => new FATBlock(GetBlockMemory(FATBlockId));

    private RootBlock GetRootBlock() => new RootBlock(GetBlockMemory(RootBlockId));

    private readonly struct FATBlock
    {
        public const int Length = BlockSize / BlockIdSize;
        private readonly Memory<byte> _fatBlock;

        public FATBlock(Memory<byte> fatBlock)
        {
            Debug.Assert(fatBlock.Length == BlockSize);
            _fatBlock = fatBlock;
        }

        /// <summary>Gets or sets the successor block ID for a given <paramref name="blockId"/>.</summary>
        public ushort this[int blockId]
        {
            get
            {
                return BinaryPrimitives.ReadUInt16LittleEndian(_fatBlock.Span.Slice(blockId * BlockIdSize, length: BlockIdSize));
            }
            set
            {
                BinaryPrimitives.WriteUInt16LittleEndian(_fatBlock.Span.Slice(blockId * BlockIdSize, length: BlockIdSize), value);
            }
        }
    }

    /// <summary>https://vmu.falcogirgis.net/filesystem.html#fs_root</summary>
    private readonly struct RootBlock
    {
        public Memory<byte> RawData { get; }

        public RootBlock(Memory<byte> rootBlock)
        {
            Debug.Assert(rootBlock.Length == BlockSize);
            RawData = rootBlock;
        }

        public Memory<byte> Magic => RawData[..0x10];

        public Memory<byte> VolumeLabel => RawData.Slice(0x10, length: 0x20);

        public const int UsingCustomColor = 0x10;
        public const int ColorBlue = 0x11;
        public const int ColorGreen = 0x12;
        public const int ColorRed = 0x13;
        public const int ColorAlpha = 0x14;
     
        public (byte a, byte r, byte g, byte b)? VmuColor
        {
            get
            {
                var rootBlock = RawData.Span;
                if (rootBlock[UsingCustomColor] == 0)
                    return null;

                return (rootBlock[ColorAlpha], rootBlock[ColorRed], rootBlock[ColorGreen], rootBlock[ColorBlue]);
            }
            set
            {
                var rootBlock = RawData.Span;
                if (value is not { } color)
                {
                    rootBlock[UsingCustomColor..(ColorAlpha+1)].Clear();
                }
                else
                {
                    rootBlock[UsingCustomColor] = 1;
                    (rootBlock[ColorAlpha], rootBlock[ColorRed], rootBlock[ColorGreen], rootBlock[ColorBlue]) = color;
                }
            }
        }

        public Memory<byte> TimestampBinary => RawData.Slice(0x30, length: 8);

        public DateTimeOffset TimestampDateTimeOffset
        {
            get => ReadBcdDate(TimestampBinary.Span);
            set => WriteBcdDate(TimestampBinary.Span, value);
        }

        public ushort VolumeLast
        {
            get => BinaryPrimitives.ReadUInt16LittleEndian(RawData.Span.Slice(0x40, length: 2));
            set => BinaryPrimitives.WriteUInt16LittleEndian(RawData.Span.Slice(0x40, length: 2), value);
        }

        public ushort Partition
        {
            get => BinaryPrimitives.ReadUInt16LittleEndian(RawData.Span.Slice(0x42, length: 2));
            set => BinaryPrimitives.WriteUInt16LittleEndian(RawData.Span.Slice(0x42, length: 2), value);
        }

        public ushort Root
        {
            get => BinaryPrimitives.ReadUInt16LittleEndian(RawData.Span.Slice(0x44, length: 2));
            set => BinaryPrimitives.WriteUInt16LittleEndian(RawData.Span.Slice(0x44, length: 2), value);
        }

        public ushort FATFirst
        {
            get => BinaryPrimitives.ReadUInt16LittleEndian(RawData.Span.Slice(0x46, length: 2));
            set => BinaryPrimitives.WriteUInt16LittleEndian(RawData.Span.Slice(0x46, length: 2), value);
        }

        public ushort FATSize
        {
            get => BinaryPrimitives.ReadUInt16LittleEndian(RawData.Span.Slice(0x48, length: 2));
            set => BinaryPrimitives.WriteUInt16LittleEndian(RawData.Span.Slice(0x48, length: 2), value);
        }

        public ushort DirLast
        {
            get => BinaryPrimitives.ReadUInt16LittleEndian(RawData.Span.Slice(0x4a, length: 2));
            set => BinaryPrimitives.WriteUInt16LittleEndian(RawData.Span.Slice(0x4a, length: 2), value);
        }

        public ushort DirSize
        {
            get => BinaryPrimitives.ReadUInt16LittleEndian(RawData.Span.Slice(0x4c, length: 2));
            set => BinaryPrimitives.WriteUInt16LittleEndian(RawData.Span.Slice(0x4c, length: 2), value);
        }

        public byte Icon
        {
            get => RawData.Span[0x4e];
            set => RawData.Span[0x4e] = value;
        }

        public byte Sort
        {
            get => RawData.Span[0x4f];
            set => RawData.Span[0x4f] = value;
        }

        public ushort HiddenFirst
        {
            get => BinaryPrimitives.ReadUInt16LittleEndian(RawData.Span.Slice(0x50, length: 2));
            set => BinaryPrimitives.WriteUInt16LittleEndian(RawData.Span.Slice(0x50, length: 2), value);
        }

        public ushort HiddenSize
        {
            get => BinaryPrimitives.ReadUInt16LittleEndian(RawData.Span.Slice(0x52, length: 2));
            set => BinaryPrimitives.WriteUInt16LittleEndian(RawData.Span.Slice(0x52, length: 2), value);
        }

        public ushort GameFirst
        {
            get => BinaryPrimitives.ReadUInt16LittleEndian(RawData.Span.Slice(0x54, length: 2));
            set => BinaryPrimitives.WriteUInt16LittleEndian(RawData.Span.Slice(0x54, length: 2), value);
        }

        public ushort GameSize
        {
            get => BinaryPrimitives.ReadUInt16LittleEndian(RawData.Span.Slice(0x56, length: 2));
            set => BinaryPrimitives.WriteUInt16LittleEndian(RawData.Span.Slice(0x56, length: 2), value);
        }
    }

    internal static byte ToBinaryCodedDecimal(int value)
    {
        Debug.Assert(value is >= 0 and <= 99);
        var digit1 = value / 10;
        var digit0 = value % 10;
        var digits = (digit1 << 4) | digit0;
        Debug.Assert((byte)digits == digits);
        return (byte)digits;
    }

    internal static byte FromBinaryCodedDecimal(byte value)
    {
        // Note: this will handle invalid BCDs (nybbles > 9) by just permitting each nybble to represent up to 15 of its place-value.
        // e.g. a value `0xff` would be interpreted as (15 + 15 * 10) == 165.
        var lower = value & 0xf;
        var upper = ((value & 0xf0) >> 0x4) * 10;
        var sum = lower + upper;
        Debug.Assert((byte)sum == sum);
        return (byte)sum;
    }

    internal void OnFlashBlockModified(int blockId, DateTimeOffset now)
    {
        Debug.Assert((ushort)blockId == blockId);
        var usingFolder = Directory.Exists(LoadedPath);
        if (usingFolder)
        {
            // No point updating this info unless we are using a folder.
            _changedBlockIds.Add((ushort)blockId);
            _flushDeadline = now.AddMilliseconds(100);
        }

        HasUnsavedChanges = VmuFileHandle is null && !usingFolder;
    }

    internal bool ShouldFlushToFolder(DateTimeOffset now) => now >= _flushDeadline && Directory.Exists(LoadedPath);

    internal void FlushToFolder(DirectoryInfo vmsFolder)
    {
        if (!vmsFolder.Exists)
            throw new InvalidOperationException();

        // Step 1: detect deletions
        var newFiles = new HashSet<string>();
        foreach (var newEntry in EnumerateDirectoryTable())
        {
            if (newEntry.Type != FileType.None)
                newFiles.Add(newEntry.NameString);
        }

        var toDelete = new List<string>();
        foreach (var oldEntry in EnumerateDirectoryTableMirror())
        {
            if (oldEntry is { Type: not FileType.None, NameString: var nameString }
                && !newFiles.Contains(nameString))
            {
                // Old table contains this entry but new table doesn't.
                // The file was deleted.
                toDelete.Add(nameString);
            }
        }

        // Step 2: detect new/changed files
        var toWrite = new List<DirectoryEntry>();
        foreach (var newEntry in EnumerateDirectoryTable())
        {
            if (newEntry.Type == FileType.None)
                continue;

            var fatBlock = GetFATBlock();
            for (var blockId = newEntry.StartFAT; blockId != FAT_LastInFile; blockId = fatBlock[blockId])
            {
                if (_changedBlockIds.Contains(blockId))
                {
                    // This file's content was changed since the last flush.
                    // Write it to disk again.
                    toWrite.Add(newEntry);
                    break;
                }
            }
        }

        // Look at all the .vmi files in the host folder, to find which one has a matching VMU filesystem name.
        //
        // Note: the filenames in the host file system, are not matched against the vms names.
        // The user can rename a .vmi+.vms pair to anything they want (as long as the names without extension match)
        // and we will continue to pick the right file up, update it when it changes, etc.
        //
        // Note: VMU filenames can be duplicated (simply by copy+paste+renaming a vmi+vms pair, for example).
        // If this happens, we will operate on the first one we found and ignore the others.
        // This is thought to be preferable to canceling the flush (not saving user's changes to disk),
        // or potentially overwriting all duplicates (unnecessary complexity for an error scenario).
        // If user needs to figure out which of the duplicate files got updated, they can check timestamps.
        var vmiFilesByVmuFileName = vmsFolder
            .EnumerateFiles("*.vmi")
            .GroupBy(info => new VmiInfo(File.ReadAllBytes(info.FullName)).VmuFileNameString)
            .ToDictionary(
                group => group.Key,
                group => group.First());

        // Step 3: make host file system changes
        foreach (var vmuFileToDelete in toDelete)
        {
            if (!vmiFilesByVmuFileName.TryGetValue(vmuFileToDelete, out var vmiInfo))
                // Didn't find the corresponding .vmi file in the host system. Nothing to delete.
                continue;

            vmiInfo.Delete();
            File.Delete(Path.ChangeExtension(vmiInfo.FullName, ".vms"));
        }

        foreach (var entryToWrite in toWrite)
        {
            var onVmuFileName = entryToWrite.NameString;
            var vmiFileInfo = vmiFilesByVmuFileName.TryGetValue(entryToWrite.NameString, out var existingInfo)
                ? existingInfo
                : new FileInfo(Path.Combine(vmsFolder.FullName, $"{onVmuFileName}.vmi")); // new file

            var vmiInfo = CreateVmiInfo(entryToWrite);
            using var vmiFileStream = vmiFileInfo.Create();
            vmiFileStream.Write(vmiInfo.RawData.Span);

            using var vmsFile = File.Create(Path.ChangeExtension(vmiFileInfo.FullName, ".vms"));
            var fatBlock = GetFATBlock();
            for (var blockId = entryToWrite.StartFAT; blockId != FAT_LastInFile; blockId = fatBlock[blockId])
            {
                vmsFile.Write(GetBlock(blockId));
            }
        }

        // Update the root block if it actually changed
        var newRootBlock = GetBlock(RootBlockId);
        var rootBlockPath = Path.Combine(vmsFolder.FullName, RootBlockFilename);
        if (!File.Exists(rootBlockPath) || !File.ReadAllBytes(rootBlockPath).SequenceEqual(newRootBlock))
        {
            File.WriteAllBytes(rootBlockPath, newRootBlock);
        }

        ResetFlushData();
    }

    private IEnumerable<DirectoryEntry> EnumerateDirectoryTable()
    {
        // Note: we probably should not hardcode the last/size of the directory table here,
        // and should use the root block as a source of truth.
        // However, dealing with the fallout of this, such as by dynamically sizing the directory mirror, doesn't feel worth it currently.
        for (var blockId = DirectoryTableLastBlockId; blockId >= DirectoryTableFirstBlockId; blockId--)
        {
            var directoryBlock = GetBlockMemory(blockId);
            // Scan from start to end within a block.
            for (var offset = 0; offset < BlockSize; offset += DirectoryEntry.Size)
            {
                var directoryEntry = new DirectoryEntry(directoryBlock.Slice(start: offset, length: DirectoryEntry.Size));
                yield return directoryEntry;
            }
        }
    }

    private IEnumerable<DirectoryEntry> EnumerateDirectoryTableMirror()
    {
        for (var blockId = DirectoryTableSizeBlocks - 1; blockId >= 0; blockId--)
        {
            var directoryBlock = _directoryMirror.AsMemory(blockId * BlockSize, length: BlockSize);
            // Scan from start to end within a block.
            for (var offset = 0; offset < BlockSize; offset += DirectoryEntry.Size)
            {
                var directoryEntry = new DirectoryEntry(directoryBlock.Slice(start: offset, length: DirectoryEntry.Size));
                yield return directoryEntry;
            }
        }
    }
}

/// <summary>https://vmu.falcogirgis.net/filesystem.html#fs_dir</summary>
internal readonly struct DirectoryEntry
{
    public DirectoryEntry(Memory<byte> data)
    {
        if (data.Length != Size)
            throw new ArgumentException(null, nameof(data));

        _data = data;
    }

    private readonly Memory<byte> _data;

    internal bool HasValue => _data.Length == Size;

    internal FileType Type
    {
        get => (FileType)_data.Span[Offset_FileType];
        set => _data.Span[Offset_FileType] = (byte)value;
    }

    internal FileCopyProtection CopyProtection
    {
        get => (FileCopyProtection)_data.Span[Offset_CopyProtection];
        set => _data.Span[Offset_CopyProtection] = (byte)value;
    }

    internal ushort StartFAT
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(_data.Span.Slice(Offset_StartFAT));
        set => BinaryPrimitives.WriteUInt16LittleEndian(_data.Span.Slice(Offset_StartFAT), value);
    }

    internal Memory<byte> Name => _data.Slice(Offset_Filename, length: FileNameLength);
    internal string NameString => FileSystem.Encoding.GetString(Name.Span).Trim();

    internal Memory<byte> DateBcd => _data.Slice(Offset_Date, DateLength);

    internal DateTimeOffset DateTimeOffset
    {
        get => FileSystem.ReadBcdDate(DateBcd.Span);
        set => FileSystem.WriteBcdDate(DateBcd.Span, value);
    }

    internal ushort SizeInBlocks
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(_data.Span.Slice(Offset_SizeInBlocks));
        set => BinaryPrimitives.WriteUInt16LittleEndian(_data.Span.Slice(Offset_SizeInBlocks), value);
    }

    internal ushort VmsHeaderBlockOffset
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(_data.Span.Slice(Offset_VmsHeaderBlockOffset));
        set => BinaryPrimitives.WriteUInt16LittleEndian(_data.Span.Slice(Offset_VmsHeaderBlockOffset), value);
    }

    internal const int Size = 0x20; // 32
    internal const int Offset_FileType = 0;
    internal const int Offset_CopyProtection = 1;
    internal const int Offset_StartFAT = 2;

    internal const int Offset_Filename = 4;
    internal const int FileNameLength = 12;

    internal const int Offset_Date = 0x10;
    internal const int DateLength = 8;

    internal const int Offset_SizeInBlocks = 0x18;
    internal const int Offset_VmsHeaderBlockOffset = 0x1a;
}

internal enum FileCopyProtection : byte
{
    NotCopyProtected = 0x80,
    CopyProtected = 0xff,
}

internal enum FileType : byte
{
    None = 0,
    Data = 0x33,
    Game = 0xcc,
}

public static class MyCrazyExtensions
{
    extension(int value)
    {
        public int KB => value * 1024;
    }
}