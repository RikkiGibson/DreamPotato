using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace DreamPotato.Core;

/// <summary>
/// Performs file system operations on flash memory.
/// NOTE: does not own its state. The same flash memory buffers used by <see cref="Cpu"/> are shared here.
/// </summary>
internal class FileSystem(byte[] flash)
{
    private const int ShiftJisCodePage = 932;
    public static readonly Encoding Encoding = CodePagesEncodingProvider.Instance.GetEncoding(ShiftJisCodePage) ?? Encoding.ASCII;

    private const int VolumeSizeBytes = 128 * 1024; // 128kb

    // user region: where user visible files are stored.
    private const int UserRegionSizeBlocks = 200;
    private const int UserRegionSizeBytes = UserRegionSizeBlocks * BlockSize; // 100kb
    private const int UserRegionLastBlockId = UserRegionSizeBlocks - 1;

    // hidden region: extra storage which is used on-demand to store user files when blocks in the user region being to malfunction.
    private const int HiddenRegionSizeBlocks = 31;
    private const int HiddenRegionSizeBytes = HiddenRegionSizeBlocks * BlockSize;
    internal const int HiddenRegionLastBlockId = DirectoryTableFirstBlockId - 1;

    internal const int BlockSize = 0x200; // 512b
    internal const int BlockIdSize = 2;
    private const int BlocksCount = VolumeSizeBytes / BlockSize; // 256

    internal const byte RootBlockId = BlocksCount - 1;
    internal const byte FATBlockId = BlocksCount - 2;
    internal const byte DirectoryTableLastBlockId = BlocksCount - 3;
    private const int DirectoryTableSizeBlocks = 13;
    internal const byte DirectoryTableFirstBlockId = DirectoryTableLastBlockId - DirectoryTableSizeBlocks + 1;

    internal const int DirectoryEntrySize = 0x20; // 32
    internal const int DirectoryFileTypeOffset = 0;
    internal const int DirectoryCopyProtectionOffset = 1;
    internal const int DirectoryStartFATOffset = 2;
    internal const int DirectoryFilenameOffset = 4;
    internal const int DirectoryDateOffset = 0x10;
    internal const int DirectoryDateLength = 8;

    internal const int DirectorySizeInBlocksOffset = 0x18;
    internal const int DirectoryVmsHeaderBlockOffset = 0x1a;

    private const byte DirectoryFileTypeNone = 0;
    private const byte DirectoryFileTypeData = 0x33;
    private const byte DirectoryFileTypeGame = 0xcc;

    private const byte DirectoryEntryCopyProtected = 0xff;
    private const byte DirectoryEntryNotCopyProtected = 0;

    internal const int DirectoryEntryFileNameLength = 12;

    private const int Magic = 0x55;

    const int FAT_Unallocated = 0xfffc;
    const int FAT_UnallocatedLsb = 0xfc;
    const int FAT_UnallocatedMsb = 0xff;

    const int FAT_LastInFile = 0xfffa;
    const int FAT_LastInFileLsb = 0xfa;
    const int FAT_LastInFileMsb = 0xff;

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

    public void InitializeFileSystem(DateTimeOffset date)
    {
        // Note that multi-byte values are little-endian encoded!
        Debug.Assert(flash.Length == Cpu.FlashSize);
        Array.Clear(flash);

        initializeRootBlock();
        initializeFAT();

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

            Debug.Assert(DirectoryTableSizeBlocks == Math.Ceiling((double)UserRegionSizeBlocks * DirectoryEntrySize / BlockSize));
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
            var fatBlock = GetBlock(FATBlockId);

            // mark all blocks unused to start
            Debug.Assert(fatBlock.Length == BlockSize);
            for (int i = 0; i < BlockSize;)
            {
                fatBlock[i++] = FAT_UnallocatedLsb;
                fatBlock[i++] = FAT_UnallocatedMsb;
            }

            // mark root block as last in its file
            fatBlock[RootBlockId * 2] = FAT_LastInFileLsb;
            fatBlock[RootBlockId * 2 + 1] = FAT_LastInFileMsb;

            // mark FAT itself as last in its file
            fatBlock[FATBlockId * 2] = FAT_LastInFileLsb;
            fatBlock[FATBlockId * 2 + 1] = FAT_LastInFileMsb;

            // directory: mark as growing from end toward start of the volume.
            var directoryStartBlockId = DirectoryTableLastBlockId - DirectoryTableSizeBlocks + 1;
            for (int i = DirectoryTableLastBlockId; i > directoryStartBlockId; i--)
            {
                // Point to the previous block.
                fatBlock[i * 2] = (byte)(i - 1);
                fatBlock[i * 2 + 1] = 0;
            }

            fatBlock[directoryStartBlockId * 2] = FAT_LastInFileLsb;
            fatBlock[directoryStartBlockId * 2 + 1] = FAT_LastInFileMsb;
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


    // Note: this only works on a newly initialized file system.
    // Possibly in future will support more generalized file I/O operations.
    // At that point though why not just expose it as a .vms/.vmi folder a la Dolphin's .gci folder.
    public (bool ok, string? errorMessage) TryWriteGameFile(ReadOnlySpan<byte> gameFileData, string onDiskFileName, Memory<byte> onVmuFileName, DateTimeOffset date, FileCopyProtection copyProtection)
    {
        if (gameFileData.Length == 0)
            throw new ArgumentException(null, paramName: nameof(gameFileData));

        if (gameFileData.Length > Cpu.InstructionBankSize)
            throw new ArgumentException(null, paramName: nameof(gameFileData));

        if (onVmuFileName.Length > DirectoryEntryFileNameLength)
            throw new ArgumentException(null, paramName: nameof(onVmuFileName));

        // Verify sufficient free space
        var fatBlock = GetBlock(FATBlockId);
        var lastBlockId = (gameFileData.Length + BlockSize - 1) / BlockSize - 1;
        for (int i = 0; i <= lastBlockId; i++)
        {
            if (fatBlock[i * 2] != FAT_UnallocatedLsb
                || fatBlock[i * 2 + 1] != FAT_UnallocatedMsb)
            {
                return (false, $"{onDiskFileName}: Insufficient space");
            }
        }

        // Update FAT table indicating the game data starts at block 0 and grows toward the end
        for (int i = 0; i < lastBlockId; i++)
        {
            fatBlock[i * 2] = (byte)(i + 1);
            fatBlock[i * 2 + 1] = 0;
        }

        fatBlock[lastBlockId * 2] = FAT_LastInFileLsb;
        fatBlock[lastBlockId * 2 + 1] = FAT_LastInFileMsb;

        // Game data itself must be written to start of bank 0
        gameFileData.CopyTo(flash);

        // Create a directory entry
        if (!tryGetFreeDirectoryEntry(out var directoryEntry))
            return (false, $"{onDiskFileName}: The file system directory is full.");

        directoryEntry[DirectoryFileTypeOffset] = DirectoryFileTypeGame;
        directoryEntry[DirectoryCopyProtectionOffset] = (byte)copyProtection;

        directoryEntry[DirectoryStartFATOffset] = 0; // Start FAT: first block containing file data. Should be same as "Game First" in the root block.
        directoryEntry[DirectoryStartFATOffset + 1] = 0;

        var directoryEntryFilename = directoryEntry.Slice(start: DirectoryFilenameOffset, length: DirectoryEntryFileNameLength);
        directoryEntryFilename.Clear();

        onVmuFileName.Span.CopyTo(directoryEntry.Slice(start: DirectoryFilenameOffset, length: DirectoryEntryFileNameLength));
        WriteBcdDate(directoryEntry.Slice(DirectoryDateOffset, length: DirectoryDateLength), date);

        var sizeInBlocks = (gameFileData.Length + BlockSize - 1) / BlockSize; // integer division rounding up
        directoryEntry[DirectorySizeInBlocksOffset] = (byte)sizeInBlocks; // Size in blocks
        directoryEntry[DirectorySizeInBlocksOffset + 1] = 0;

        directoryEntry[DirectoryVmsHeaderBlockOffset] = 1; // Location of the VMS header within the file. (Ignored for game files?)
        directoryEntry[DirectoryVmsHeaderBlockOffset + 1] = 0;

        // Unused
        directoryEntry.Slice(0x1c, length: 4).Clear();

        return (true, null);

        bool tryGetFreeDirectoryEntry(out Span<byte> foundEntry)
        {
            // Scan directory blocks starting from the end, toward the start.
            for (var blockId = DirectoryTableLastBlockId; blockId >= DirectoryTableFirstBlockId; blockId--)
            {
                var directoryBlock = GetBlock(blockId);
                // Scan from start to end within a block.
                for (var offset = 0; offset < BlockSize; offset += DirectoryEntrySize)
                {
                    var directoryEntry = directoryBlock.Slice(start: offset, length: DirectoryEntrySize);
                    if (directoryEntry[0] == DirectoryFileTypeNone)
                    {
                        foundEntry = directoryEntry;
                        return true;
                    }
                }
            }

            foundEntry = default;
            return false;
        }
    }

    public void ReadAllFiles(DirectoryInfo outDir)
    {
        // Scan directory blocks starting from the end, toward the start.
        for (var blockId = DirectoryTableLastBlockId; blockId >= DirectoryTableFirstBlockId; blockId--)
        {
            var directoryBlock = GetBlockMemory(blockId);
            // Scan from start to end within a block.
            for (var offset = 0; offset < BlockSize; offset += DirectoryEntrySize)
            {
                var directoryEntry = new DirectoryEntry(directoryBlock.Slice(start: offset, length: DirectoryEntrySize));
                if (directoryEntry.Type != FileType.None)
                {
                    readFile(directoryEntry);
                }
            }
        }

        void readFile(DirectoryEntry directoryEntry)
        {
            var outFileName = directoryEntry.NameString;
            var fatBlock = GetBlock(FATBlockId);

            using var vmsFile = File.Open(Path.Combine(outDir.FullName, $"{outFileName}.vms"), FileMode.CreateNew);

            for (var blockId = directoryEntry.StartFAT;
                blockId != FAT_LastInFile;
                blockId = BinaryPrimitives.ReadUInt16LittleEndian(fatBlock.Slice(blockId * 2, length: 2)))
            {
                var block = this.GetBlock(blockId);
                vmsFile.Write(block);
            }

            using var vmiFile = File.Open(Path.Combine(outDir.FullName, $"{outFileName}.vmi"), FileMode.CreateNew);
            var vmiInfo = toVmiInfo(directoryEntry);
            vmiFile.Write(vmiInfo.RawData.Span);
        }

        VmiInfo toVmiInfo(DirectoryEntry directoryEntry)
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
    }

    public (bool ok, string? errorMessage) TryWriteAllFiles(DirectoryInfo sourceDirectory)
    {
        string? foundGameName = null;

        // Note: We could consider writing in the hidden region to fit more files, but, it doesn't seem consistent with ordinary VMUs.
        ushort currentDataBlockId = UserRegionLastBlockId;
        foreach (var vmsFileInfo in sourceDirectory.EnumerateFiles("*.vms"))
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

            if (getNewDirectoryEntry() is not { } directoryEntry)
                return (false, $"{vmsFileInfo.Name}: The file system directory is full.");

            if (tryWriteDataFile(directoryEntry, vmsFileInfo.Name, vmsFileBytes, vmiInfo) is (false, var error1))
                return (false, error1);
        }

        return (true, null);

        DirectoryEntry? getNewDirectoryEntry()
        {
            for (var blockId = DirectoryTableLastBlockId; blockId >= DirectoryTableFirstBlockId; blockId--)
            {
                var directoryBlock = GetBlockMemory(blockId);
                // Scan from start to end within a block.
                for (var offset = 0; offset < BlockSize; offset += DirectoryEntrySize)
                {
                    var directoryEntry = new DirectoryEntry(directoryBlock.Slice(start: offset, length: DirectoryEntrySize));
                    if (directoryEntry.Type == FileType.None)
                        return directoryEntry;
                }
            }

            return null;
        }

        (bool ok, string? error) tryWriteDataFile(DirectoryEntry directoryEntry, string onDiskFileName, ReadOnlySpan<byte> vmsFileBytes, VmiInfo vmiInfo)
        {
            Debug.Assert(!vmiInfo.FileMode.HasFlag(VmuFileMode.Game));

            var sizeInBlocks = (ushort)((vmsFileBytes.Length + (BlockSize - 1)) / BlockSize);
            var fatBlock = GetBlock(FATBlockId);

            // Scan to next free data block
            while (BinaryPrimitives.ReadUInt16LittleEndian(fatBlock.Slice(currentDataBlockId * 2, length: 2)) != FAT_Unallocated)
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
                    BinaryPrimitives.WriteUInt16LittleEndian(fatBlock.Slice(currentDataBlockId * 2, length: 2), value: FAT_LastInFile);
                    break;
                }

                // Each FAT entry (indexed by block ID) points to the block ID for the next block
                // think 'FAT[prevDataBlockId] = currentDataBlockId;'
                var prevDataBlockId = currentDataBlockId;

                // Scan to next free data block
                do
                {
                    if (currentDataBlockId == 0)
                        return (false, $"{onDiskFileName}: Insufficient space on VMU");

                    currentDataBlockId--;
                } while (BinaryPrimitives.ReadUInt16LittleEndian(fatBlock.Slice(currentDataBlockId * 2, length: 2)) != FAT_Unallocated);

                BinaryPrimitives.WriteUInt16LittleEndian(fatBlock.Slice(prevDataBlockId * 2, length: 2), value: currentDataBlockId);
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
        var rangeStart = blockId * BlockSize;
        var rangeEnd = (blockId + 1) * BlockSize;
        return flash.AsSpan(rangeStart..rangeEnd);
    }

    internal Memory<byte> GetBlockMemory(int blockId)
    {
        var rangeStart = blockId * BlockSize;
        var rangeEnd = (blockId + 1) * BlockSize;
        return flash.AsMemory(rangeStart..rangeEnd);
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
}

/// <summary>https://vmu.falcogirgis.net/filesystem.html#fs_dir</summary>
internal readonly struct DirectoryEntry
{
    public DirectoryEntry(Memory<byte> data)
    {
        if (data.Length != DirectoryEntrySize)
            throw new ArgumentException(null, nameof(data));

        _data = data;
    }

    private readonly Memory<byte> _data;

    internal FileType Type
    {
        get => (FileType)_data.Span[DirectoryFileTypeOffset];
        set => _data.Span[DirectoryFileTypeOffset] = (byte)value;
    }

    internal FileCopyProtection CopyProtection
    {
        get => (FileCopyProtection)_data.Span[DirectoryCopyProtectionOffset];
        set => _data.Span[DirectoryCopyProtectionOffset] = (byte)value;
    }

    internal ushort StartFAT
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(_data.Span.Slice(DirectoryStartFATOffset));
        set => BinaryPrimitives.WriteUInt16LittleEndian(_data.Span.Slice(DirectoryStartFATOffset), value);
    }

    internal Memory<byte> Name => _data.Slice(DirectoryFilenameOffset, length: DirectoryEntryFileNameLength);
    internal string NameString => FileSystem.Encoding.GetString(Name.Span).Trim();

    internal Memory<byte> DateBcd => _data.Slice(DirectoryDateOffset, DirectoryDateLength);

    internal DateTimeOffset DateTimeOffset
    {
        get => FileSystem.ReadBcdDate(DateBcd.Span);
        set => FileSystem.WriteBcdDate(DateBcd.Span, value);
    }

    internal ushort SizeInBlocks
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(_data.Span.Slice(DirectorySizeInBlocksOffset));
        set => BinaryPrimitives.WriteUInt16LittleEndian(_data.Span.Slice(DirectorySizeInBlocksOffset), value);
    }

    internal ushort VmsHeaderBlockOffset
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(_data.Span.Slice(DirectoryVmsHeaderBlockOffset));
        set => BinaryPrimitives.WriteUInt16LittleEndian(_data.Span.Slice(DirectoryVmsHeaderBlockOffset), value);
    }

    internal const int DirectoryEntrySize = 0x20; // 32
    internal const int DirectoryFileTypeOffset = 0;
    internal const int DirectoryCopyProtectionOffset = 1;
    internal const int DirectoryStartFATOffset = 2;
    internal const int DirectoryFilenameOffset = 4;
    internal const int DirectoryDateOffset = 0x10;
    internal const int DirectoryDateLength = 8;

    internal const int DirectorySizeInBlocksOffset = 0x18;
    internal const int DirectoryVmsHeaderBlockOffset = 0x1a;

    internal const int DirectoryEntryFileNameLength = 12;
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
