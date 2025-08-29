using System.Diagnostics;
using System.Text;

namespace DreamPotato.Core;

/// <summary>
/// Performs file system operations on flash memory.
/// NOTE: does not own its state. The same flash memory buffers used by <see cref="Cpu"/> are shared here.
/// </summary>
internal class FileSystem(byte[] flash)
{
    private const int VolumeSizeBytes = 128 * 1024; // 128kb

    // user region: where user visible files are stored.
    private const int UserRegionSizeBlocks = 200;
    private const int UserRegionSizeBytes = UserRegionSizeBlocks * BlockSize; // 100kb

    // hidden region: extra storage which is used on-demand to store user files when blocks in the user region being to malfunction.
    private const int HiddenRegionSizeBlocks = 31;
    private const int HiddenRegionSizeBytes = HiddenRegionSizeBlocks * BlockSize;

    internal const int BlockSize = 0x200; // 512b
    private const int BlocksCount = VolumeSizeBytes / BlockSize; // 256

    internal const byte RootBlockId = BlocksCount - 1;
    internal const byte FATBlockId = BlocksCount - 2;
    internal const byte DirectoryLastBlockId = BlocksCount - 3;

    internal const int DirectoryEntrySize = 0x20; // 32

    private const int DirectoryTableSizeBlocks = 13;

    private const byte DirectoryFileTypeNone = 0;
    private const byte DirectoryFileTypeData = 0x33;
    private const byte DirectoryFileTypeGame = 0xcc;

    private const byte DirectoryEntryCopyProtected = 0xff;
    private const byte DirectoryEntryNotCopyProtected = 0;

    internal const int DirectoryEntryFileNameLength = 12;

    private const int Magic = 0x55;

    // 0xfffc: Unallocated space
    const int FAT_UnallocatedLsb = 0xfc;
    const int FAT_UnallocatedMsb = 0xff;

    // 0xfffa: Last in File
    const int FAT_LastInFileLsb = 0xfa;
    const int FAT_LastInFileMsb = 0xff;

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
            rootBlock[0x10] = 0x01; // Using custom volume color
            rootBlock[0x11] = 0xff; // blue
            rootBlock[0x12] = 0xff; // green
            rootBlock[0x13] = 0xff; // red
            rootBlock[0x14] = 0x64; // alpha
            rootBlock[0x15..0x30].Clear();

            // 0x30-0x38: Timestamp
            WriteDate(date, rootBlock.Slice(0x30, length: 8));

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

            rootBlock[0x4a] = DirectoryLastBlockId; // DIR last. Last block of the Directory table, which holds file metadata
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
            var directoryStartBlockId = DirectoryLastBlockId - DirectoryTableSizeBlocks + 1;
            for (int i = DirectoryLastBlockId; i > directoryStartBlockId; i--)
            {
                // Point to the previous block.
                fatBlock[i * 2] = (byte)(i - 1);
                fatBlock[i * 2 + 1] = 0;
            }

            fatBlock[directoryStartBlockId * 2] = FAT_LastInFileLsb;
            fatBlock[directoryStartBlockId * 2 + 1] = FAT_LastInFileMsb;
        }
    }

    private static void WriteDate(DateTimeOffset date, Span<byte> dest)
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


    // Note: this only works on a newly initialized file system.
    // Possibly in future will support more generalized file I/O operations.
    // At that point though why not just expose it as a .vms/.vmi folder a la Dolphin's .gci folder.
    public void WriteGameFile(ReadOnlySpan<byte> gameFileData, string filename, DateTimeOffset date)
    {
        if (gameFileData.Length == 0)
            throw new ArgumentException(nameof(gameFileData));

        if (gameFileData.Length > Cpu.InstructionBankSize)
            throw new ArgumentException(nameof(gameFileData));

        if (filename.Length > DirectoryEntryFileNameLength)
            throw new ArgumentException(nameof(filename));

        // Game data itself must be written to start of bank 0
        gameFileData.CopyTo(flash);

        // Update FAT table indicating the game data starts at block 0 and grows toward the end
        var fatBlock = GetBlock(FATBlockId);
        var lastBlockId = (gameFileData.Length + BlockSize - 1) / BlockSize - 1;
        for (int i = 0; i < lastBlockId; i++)
        {
            fatBlock[i * 2] = (byte)(i + 1);
            fatBlock[i * 2 + 1] = 0;
        }

        fatBlock[lastBlockId * 2] = FAT_LastInFileLsb;
        fatBlock[lastBlockId * 2 + 1] = FAT_LastInFileMsb;

        // Create a directory entry
        var directoryLastBlock = GetBlock(DirectoryLastBlockId);
        directoryLastBlock[0x00] = DirectoryFileTypeGame;
        directoryLastBlock[0x01] = DirectoryEntryNotCopyProtected;

        directoryLastBlock[0x02] = 0; // Start FAT: first block containing file data. Should be same as "Game First" in the root block.
        directoryLastBlock[0x03] = 0;

        var directoryEntryFilename = directoryLastBlock.Slice(start: 0x04, length: 12);
        directoryEntryFilename.Clear();

        const int shiftJisCodePage = 932;
        var encoding = CodePagesEncodingProvider.Instance.GetEncoding(shiftJisCodePage) ?? Encoding.ASCII;
        var encodedBytes = encoding.GetBytes(filename, directoryLastBlock.Slice(start: 0x04, length: 12));
        if (encodedBytes != filename.Length)
            throw new InvalidOperationException();

        WriteDate(date, directoryLastBlock.Slice(0x10, length: 8));

        var sizeInBlocks = (gameFileData.Length + BlockSize - 1) / BlockSize; // integer division rounding up
        directoryLastBlock[0x18] = (byte)sizeInBlocks; // Size in blocks
        directoryLastBlock[0x19] = 0;

        directoryLastBlock[0x1a] = 1; // Location of the VMS header within the file. (Ignored for game files?)
        directoryLastBlock[0x1b] = 0;

        directoryLastBlock.Slice(0x1c, length: 4).Clear();
    }

    internal Span<byte> GetBlock(int blockId)
    {
        var rangeStart = blockId * BlockSize;
        var rangeEnd = (blockId + 1) * BlockSize;
        return flash.AsSpan(rangeStart..rangeEnd);
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
}