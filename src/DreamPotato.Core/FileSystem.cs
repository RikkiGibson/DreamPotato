using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace DreamPotato.Core;

/// <summary>
/// Performs file system operations on flash memory.
/// </summary>
internal class FileSystem
{
    /// <summary>Same instance as <see cref="Cpu.Flash"/>.</summary>
    private readonly byte[] flash;

    /// <summary>
    /// Records the IDs of FAT blocks whose contents were modified.
    /// Later, these are used to decide which files to flush to a VMS folder.
    /// </summary>
    private readonly HashSet<ushort> _changedBlockIds = [];

    /// <summary>
    /// Contents of the directory table preceding the most recent flush or file load.
    /// Mainly used to detect file deletion.
    /// </summary>
    private readonly byte[] _directoryMirror = new byte[DirectoryTableSizeBlocks * BlockSize];

    /// <summary>Records the time at which VMS folder I/O should be flushed.</summary>
    private DateTimeOffset _flushDeadline = DateTimeOffset.MaxValue;

    public FileSystem(byte[] flash)
    {
        this.flash = flash;
        UpdateDirectoryMirror();
    }

    public void SetUnbacked(string filePath)
    {
        // TODO2: for each of these changes, flush the current state, then update to the new state.
    }

    public void SetBackingVmuFile(string filePath, FileStream vmuFileStream)
    {
    }

    public void SetBackingFolder(string folderPath)
    {
    }

    private const int ShiftJisCodePage = 932;
    public static readonly Encoding Encoding = CodePagesEncodingProvider.Instance.GetEncoding(ShiftJisCodePage) ?? Encoding.ASCII;

    private const int VolumeSizeBytes = 128 * 1024; // 128kb

    // user region: where user visible files are stored.
    private const int UserRegionSizeBlocks = 200;
    private const int UserRegionLastBlockId = UserRegionSizeBlocks - 1;

    // hidden region: extra storage which is used on-demand to store user files when blocks in the user region being to malfunction.
    private const int HiddenRegionSizeBlocks = 31; // TODO2: this seems likely wrong

    internal const int BlockSize = 0x200; // 512b
    internal const int BlockIdSize = 2;
    private const int BlocksCount = VolumeSizeBytes / BlockSize; // 256

    internal const byte RootBlockId = BlocksCount - 1;
    internal const byte FATBlockId = BlocksCount - 2;
    internal const byte DirectoryTableLastBlockId = BlocksCount - 3;
    private const int DirectoryTableSizeBlocks = 13;
    internal const byte DirectoryTableFirstBlockId = DirectoryTableLastBlockId - DirectoryTableSizeBlocks + 1;

    private const int Magic = 0x55;

    const ushort FAT_Unallocated = 0xfffc;

    const ushort FAT_LastInFile = 0xfffa;

    // offsets for color data within the root block
    const int UsingCustomColor = 0x10;
    const int ColorBlue = 0x11;
    const int ColorGreen = 0x12;
    const int ColorRed = 0x13;
    const int ColorAlpha = 0x14;

    public (byte a, byte r, byte g, byte b) VmuColor
    {
        get
        {
            var rootBlock = GetBlock(RootBlockId);
            // TODO: what if UsingCustomColor != 1?
            return (rootBlock[ColorAlpha], rootBlock[ColorRed], rootBlock[ColorGreen], rootBlock[ColorBlue]);
        }
    }

    private void UpdateDirectoryMirror()
    {
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
        UpdateDirectoryMirror();

        void initializeRootBlock()
        {
            var rootBlock = GetBlock(RootBlockId);

            // 0x00-0x10: Magic
            for (int i = 0; i < 0x10; i++)
                rootBlock[i] = Magic;

            // 0x10-0x30: Volume label (VMU stores the color here).
            rootBlock[UsingCustomColor] = 0x01;
            rootBlock[ColorBlue] = 0xff;
            rootBlock[ColorGreen] = 0xff;
            rootBlock[ColorRed] = 0xff;
            rootBlock[ColorAlpha] = 0x64;
            rootBlock[0x15..0x30].Clear();

            // 0x30-0x38: Timestamp
            WriteBcdDate(rootBlock.Slice(0x30, length: 8), date);

            // 0x38-0x40: Reserved
            rootBlock[0x38..0x40].Clear();

            // 0x40: Important FAT stuff
            rootBlock[0x40] = 0xff; // Volume Last
            rootBlock[0x41] = 0;

            rootBlock[0x42] = 0; // Partition
            rootBlock[0x43] = 0;

            rootBlock[0x44] = RootBlockId; // Root block ID. This is the last block in the storage device.
            rootBlock[0x45] = 0;

            rootBlock[0x46] = FATBlockId; // FAT First. First block containing the FAT data. i.e. indicating the sequences of blocks that comprise the contents of files.
            rootBlock[0x47] = 0;

            rootBlock[0x48] = 1; // FAT size. How many blocks does the FAT take up.
            rootBlock[0x49] = 0;

            rootBlock[0x4a] = DirectoryTableLastBlockId; // DIR last. Last block of the Directory table, which holds file metadata
            rootBlock[0x4b] = 0;

            Debug.Assert(DirectoryTableSizeBlocks == Math.Ceiling((double)UserRegionSizeBlocks * DirectoryEntry.Size / BlockSize));
            rootBlock[0x4c] = DirectoryTableSizeBlocks; // DIR size. How many blocks does the Directory table take up.
            rootBlock[0x4d] = 0;

            rootBlock[0x4e] = 0; // Icon (0-123). Used in the DC BIOS.
            rootBlock[0x4f] = 0; // Sort. Unused.

            rootBlock[0x50] = UserRegionSizeBlocks; // Hidden First: Block ID of the first hidden region block.
            rootBlock[0x51] = 0;

            rootBlock[0x52] = HiddenRegionSizeBlocks; // Hidden Size.
            rootBlock[0x53] = 0;

            rootBlock[0x54] = 0; // Game First: Block ID where a game file must start.
            rootBlock[0x55] = 0;

            rootBlock[0x56] = Cpu.InstructionBankSize / BlockSize; // Game Size: Largest size in blocks of a game file.
            rootBlock[0x57] = 0;

            // The rest of the root block is reserved
            rootBlock[0x58..^1].Clear();
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

    public (bool ok, string? errorMessage) TryWriteGameFile(ReadOnlySpan<byte> gameFileData, string onDiskFileName, Memory<byte> onVmuFileName, DateTimeOffset date, FileCopyProtection copyProtection)
    {
        if (gameFileData.Length == 0)
            throw new ArgumentException(null, paramName: nameof(gameFileData));

        if (gameFileData.Length > Cpu.InstructionBankSize)
            throw new ArgumentException(null, paramName: nameof(gameFileData));

        if (onVmuFileName.Length > DirectoryEntry.FileNameLength)
            throw new ArgumentException(null, paramName: nameof(onVmuFileName));

        // Verify sufficient free space
        var fatBlock = GetFATBlock();
        var lastBlockId = (gameFileData.Length + BlockSize - 1) / BlockSize - 1;
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
        gameFileData.CopyTo(flash);

        // Create a directory entry
        var directoryEntry = EnumerateDirectoryTable().FirstOrDefault(entry => entry.Type == FileType.None);
        if (!directoryEntry.HasValue)
            return (false, $"{onDiskFileName}: The file system directory is full.");

        directoryEntry.Type = FileType.Game;
        directoryEntry.CopyProtection = copyProtection;
        directoryEntry.StartFAT = 0;
        onVmuFileName.CopyTo(directoryEntry.Name);
        directoryEntry.DateTimeOffset = date;
        directoryEntry.SizeInBlocks = (ushort)((gameFileData.Length + BlockSize - 1) / BlockSize);
        directoryEntry.VmsHeaderBlockOffset = 1;

        return (true, null);
    }

    public void ReadAllFiles(DirectoryInfo destDirectory)
    {
        foreach (var directoryEntry in EnumerateDirectoryTable())
        {
            if (directoryEntry.Type != FileType.None)
            {
                readFile(directoryEntry);
            }
        }

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

    public (bool ok, string? errorMessage) TryWriteAllFiles(DirectoryInfo sourceDirectory)
    {
        string? foundGameName = null;

        // Note: We could consider writing in the hidden region to fit more files, but, it doesn't seem consistent with ordinary VMUs.
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
            var vmsFileBytes = File.ReadAllBytes(vmsFileInfo.FullName);
            if (vmiInfo.FileMode.HasFlag(VmuFileMode.Game))
            {
                if (foundGameName != null)
                    return (false, $"Cannot use multiple games in VMS folder: '{foundGameName}', '{vmiFileInfo.Name}'");

                foundGameName = vmiFileInfo.Name;
                var copyProtection = vmiInfo.FileMode.HasFlag(VmuFileMode.CopyProtected) ? FileCopyProtection.CopyProtected : FileCopyProtection.NotCopyProtected;
                if (TryWriteGameFile(vmsFileBytes, onDiskFileName: vmsFileInfo.Name, onVmuFileName: vmiInfo.VmuFileName, vmiInfo.CreationDateTimeOffset, copyProtection) is (false, var error))
                    return (false, error);

                continue;
            }

            var directoryEntry = EnumerateDirectoryTable().FirstOrDefault(entry => entry.Type == FileType.None);
            if (!directoryEntry.HasValue)
                return (false, $"{vmsFileInfo.Name}: The file system directory is full.");

            if (tryWriteDataFile(directoryEntry, vmsFileInfo.Name, vmsFileBytes, vmiInfo) is (false, var error1))
                return (false, error1);
        }

        return (true, null);

        (bool ok, string? error) tryWriteDataFile(DirectoryEntry directoryEntry, string onDiskFileName, ReadOnlySpan<byte> vmsFileBytes, VmiInfo vmiInfo)
        {
            Debug.Assert(!vmiInfo.FileMode.HasFlag(VmuFileMode.Game));

            var sizeInBlocks = (ushort)((vmsFileBytes.Length + (BlockSize - 1)) / BlockSize);
            var fatBlock = GetFATBlock();

            // Scan to next free data block
            while (fatBlock[currentDataBlockId] != FAT_Unallocated)
            {
                if (currentDataBlockId == 0)
                    return (false, $"{onDiskFileName}: Insufficient space on VMU");

                currentDataBlockId--;
            }

            ushort startFAT = currentDataBlockId;
            for (var i = 0; i < sizeInBlocks; i++)
            {
                var dataBlock = GetBlock(currentDataBlockId);
                vmsFileBytes.Slice(i * BlockSize, length: BlockSize).CopyTo(dataBlock);

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
                        return (false, $"{onDiskFileName}: Insufficient space on VMU");

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

            // Divide size in bytes by block size, round up
            directoryEntry.SizeInBlocks = (ushort)((vmiInfo.VmsFileSize + (BlockSize - 1)) / BlockSize);

            // Note: if the vms doesn't match this convention, then we have no way of really knowing where its header is.
            // We might want to verify the header is in the expected location in 'ReadAllFiles'.
            directoryEntry.VmsHeaderBlockOffset = 0;
            return (true, null);
        }
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

    internal void OnFlashModified(int offset, DateTimeOffset now)
    {
        var blockId = offset / BlockSize;
        Debug.Assert((ushort)blockId == blockId);
        _changedBlockIds.Add((ushort)blockId);
        _flushDeadline = now.AddSeconds(1);
    }

    internal bool Poll(DirectoryInfo vmsFolder, DateTimeOffset now)
    {
        if (now >= _flushDeadline)
        {
            Flush(vmsFolder);
            return true;
        }

        return false;
    }

    internal void Flush(DirectoryInfo vmsFolder)
    {
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

        var vmiFilesByVmuFileName = vmsFolder.EnumerateFiles("*.vmi").ToDictionary(
            info => new VmiInfo(File.ReadAllBytes(info.FullName)).VmuFileNameString);

        // Step 3: make host file system changes
        foreach (var vmuFileToDelete in toDelete)
        {
            if (!vmiFilesByVmuFileName.TryGetValue(vmuFileToDelete, out var vmiInfo))
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

        _changedBlockIds.Clear();
        _flushDeadline = DateTimeOffset.MaxValue;
        UpdateDirectoryMirror();
    }

    private IEnumerable<DirectoryEntry> EnumerateDirectoryTable()
    {
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

    private enum BackingMode
    {
        /// <summary>Changes are not saved anywhere. Used for a '.vms' file or no file.</summary>
        None,

        /// <summary>Changes are saved to a backing '.vmu'/'.bin' file.</summary>
        BackingFile,

        /// <summary>Changes are saved to a backing folder which mirrors the VMU's file system.</summary>
        BackingFolder
    }
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
