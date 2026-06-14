using DreamPotato.Core;

namespace DreamPotato.Tests;

public class FileSystemTests : IDisposable
{
    private DirectoryInfo? _tempRoot;

    public void Dispose()
    {
        if (_tempRoot?.Exists == true)
            _tempRoot.Delete(recursive: true);
    }

    [Fact]
    public void InitializeFileSystem()
    {
        var date = DateTime.Parse("08/18/2018 07:22:16");
        var flash = new byte[Cpu.FlashSize];
        var fileSystem = new FileSystem(flash);
        fileSystem.InitializeFileSystem(date);

        verifyRootBlock();
        verifyFATBlock();
        using var gameFile = File.OpenRead("TestSource/helloworld.vms");
        var (success, _) = fileSystem.TryWriteGameFile(gameFile, "HelloWorld", FileSystem.Encoding.GetBytes("HelloWorld"), date, FileCopyProtection.NotCopyProtected);
        Assert.True(success);

        void verifyRootBlock()
        {
            byte[] expected = new byte[FileSystem.BlockSize];
            ReadOnlySpan<byte> content = [
                0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, // 0000
                0x01, 0xFF, 0xFF, 0xFF, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 0100
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 0200
                0x20, 0x18, 0x08, 0x18, 0x07, 0x22, 0x16, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 0300
                0xFF, 0x00, 0x00, 0x00, 0xFF, 0x00, 0xFE, 0x00, 0x01, 0x00, 0xFD, 0x00, 0x0D, 0x00, 0x00, 0x00, // 0400
                0xC8, 0x00, 0x29, 0x00, 0x00, 0x00, 0x80,                                                       // 0500
            ];
            content.CopyTo(expected);

            var rootBlock = fileSystem.GetBlock(FileSystem.RootBlockId);
            Assert.Equal(expected.Length, rootBlock.Length);
            Assert.Equal(expected, rootBlock);
        }

        void verifyFATBlock()
        {
            ReadOnlySpan<byte> expected = [
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0000
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0100
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0200
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0300
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0400
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0500
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0600
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0700
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0800
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0900
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0a00
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0b00
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0c00
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0d00
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0e00
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 0f00
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 1000
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 1100
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 1200
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 1300
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 1400
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 1500
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 1600
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 1700
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 1800
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 1900
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 1a00
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 1b00
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 1c00
                0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, 0xFC, 0xFF, // 1d00
                0xFC, 0xFF, 0xFA, 0xFF, 0xF1, 0x00, 0xF2, 0x00, 0xF3, 0x00, 0xF4, 0x00, 0xF5, 0x00, 0xF6, 0x00, // 1e00
                0xF7, 0x00, 0xF8, 0x00, 0xF9, 0x00, 0xFA, 0x00, 0xFB, 0x00, 0xFC, 0x00, 0xFA, 0xFF, 0xFA, 0xFF, // 1f00
            ];

            var fatBlock = fileSystem.GetBlock(FileSystem.FATBlockId);
            Assert.Equal(expected.Length, fatBlock.Length);
            Assert.Equal(expected, fatBlock);
        }
    }

    [Fact]
    public void WriteGameFile()
    {
        var date = DateTime.Parse("08/18/2018 07:22:16");
        var flash = new byte[Cpu.FlashSize];
        var fileSystem = new FileSystem(flash);
        fileSystem.InitializeFileSystem(date);
        using var gameFile = File.OpenRead("TestSource/helloworld.vms");
        var (success, _) = fileSystem.TryWriteGameFile(gameFile, "HelloWorld", FileSystem.Encoding.GetBytes("HelloWorld"), date, FileCopyProtection.NotCopyProtected);
        Assert.True(success);

        var fatEntry = fileSystem.GetBlock(FileSystem.FATBlockId);
        ReadOnlySpan<byte> expectedFATStart = [
            0x01, 0x00, 0x02, 0x00,
            0x03, 0x00, 0xfa, 0xff,
            0xfc, 0xff, 0xfc, 0xff,
            0xfc, 0xff, 0xfc, 0xff,
        ];
        Assert.Equal(expectedFATStart, fatEntry[0..0x10]);

        var directoryEntry = fileSystem.GetBlock(FileSystem.DirectoryTableLastBlockId);
        Assert.Equal<object>("""
               | 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 
            00 | CC 80 00 00 48 65 6C 6C 6F 57 6F 72 6C 64 00 00 
            01 | 20 18 08 18 07 22 16 06 04 00 01 00 00 00 00 00 
            """, ((ReadOnlySpan<byte>)directoryEntry[0..DirectoryEntry.Size]).AsHexBlock());
    }

    [Fact]
    public void ReadFiles_01()
    {
        // Test the functionality used to implement 'Save as Folder' using a pre-existing VMU
        // I have a gut feeling that while vms/vmi needs to just work,
        // dci is going to be a way better format to work with..
        _tempRoot = Directory.CreateTempSubdirectory("DreamPotato.Tests");
        var flash = File.ReadAllBytes("TestSource/ReadFiles_01.vmu");
        var fileSystem = new FileSystem(flash);

        var outDir = _tempRoot.CreateSubdirectory("ReadFiles_01");
        var (ok, error) = fileSystem.TryReadAllFiles(outDir);
        Assert.True(ok, error);

        var fileNames = string.Join(Environment.NewLine,
            Directory.EnumerateFiles(outDir.FullName)
                .Select(f => Path.GetFileName(f))
                .OrderBy(f => f));

        Assert.Equal<object>("""
            fs_root.bin
            ICONDATA_VMS.vmi
            ICONDATA_VMS.vms
            S.ARCADIA_VM.vmi
            S.ARCADIA_VM.vms
            S.ARCADIA001.vmi
            S.ARCADIA001.vms
            S.ARCADIA002.vmi
            S.ARCADIA002.vms
            S.ARCADIA003.vmi
            S.ARCADIA003.vms
            VMUTOOL__OPT.vmi
            VMUTOOL__OPT.vms
            ZOMBIE_U_SYS.vmi
            ZOMBIE_U_SYS.vms
            """,
            fileNames);

        var vmiBytes = File.ReadAllBytes(Path.Combine(outDir.FullName, "S.ARCADIA001.vmi"));
        Assert.Equal<object>("""
               | 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 
            00 | 53 04 41 40 41 72 63 61 64 69 61 20 20 20 20 20 
            01 | 20 53 61 69 6C 6F 72 73 27 20 49 73 6C 61 6E 64 
            02 | 20 49 6E 6E 47 65 6E 65 72 61 74 65 64 20 62 79 
            03 | 20 44 72 65 61 6D 50 6F 74 61 74 6F 00 00 00 00 
            04 | 00 00 00 00 E7 07 07 13 16 2F 1E 03 00 00 01 00 
            05 | 53 2E 41 52 43 41 44 49 53 2E 41 52 43 41 44 49 
            06 | 41 30 30 31 00 00 00 00 00 36 00 00 
            """,
            ((ReadOnlySpan<byte>)vmiBytes).AsHexBlock());
    }

    [Fact]
    public void WriteFiles_01()
    {
        // Test the functionality used to implement 'Open Folder'
        _tempRoot = Directory.CreateTempSubdirectory("DreamPotato.Tests");

        var flash1 = File.ReadAllBytes("TestSource/ReadFiles_01.vmu");
        var fileSystem1 = new FileSystem(flash1);

        var vmsFolder = _tempRoot.CreateSubdirectory("ReadFiles_01");
        var (ok, error) = fileSystem1.TryReadAllFiles(vmsFolder);
        Assert.True(ok, error);

        var flash2 = new byte[Cpu.FlashSize];
        var fileSystem2 = new FileSystem(flash2);
        (ok, error) = fileSystem2.TryInitializeFromFolder(vmsFolder, fallbackDate: DateTimeOffset.MaxValue);
        Assert.True(ok, error);

        var expectedFatBlock = ((ReadOnlySpan<byte>)fileSystem2.GetBlock(FileSystem.FATBlockId)).AsHexBlock();
        Assert.Equal<object>("""
               | 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 
            00 | 01 00 02 00 03 00 04 00 05 00 06 00 07 00 08 00 
            01 | 09 00 0A 00 0B 00 0C 00 0D 00 0E 00 0F 00 10 00 
            02 | 11 00 12 00 13 00 14 00 15 00 16 00 17 00 18 00 
            03 | 19 00 1A 00 1B 00 1C 00 1D 00 1E 00 1F 00 20 00 
            04 | 21 00 22 00 23 00 24 00 25 00 26 00 27 00 28 00 
            05 | 29 00 2A 00 2B 00 2C 00 2D 00 2E 00 2F 00 30 00 
            06 | 31 00 32 00 33 00 34 00 35 00 36 00 37 00 38 00 
            07 | 39 00 3A 00 3B 00 3C 00 3D 00 3E 00 3F 00 40 00 
            08 | 41 00 42 00 43 00 44 00 45 00 46 00 47 00 48 00 
            09 | 49 00 4A 00 4B 00 4C 00 4D 00 4E 00 4F 00 50 00 
            0A | 51 00 52 00 FA FF FC FF FC FF FC FF FC FF FC FF 
            0B | FC FF FC FF FC FF FC FF FC FF FC FF FC FF FC FF 
            0C | FC FF FC FF FC FF FC FF FC FF FC FF FC FF FC FF 
            0D | FC FF FA FF 69 00 6A 00 6B 00 FA FF 6D 00 6E 00 
            0E | 6F 00 70 00 71 00 72 00 73 00 FA FF 75 00 76 00 
            0F | 77 00 78 00 79 00 7A 00 7B 00 7C 00 7D 00 7E 00 
            10 | 7F 00 80 00 81 00 82 00 83 00 84 00 85 00 86 00 
            11 | 87 00 88 00 89 00 8A 00 8B 00 8C 00 8D 00 8E 00 
            12 | FA FF 90 00 91 00 92 00 93 00 94 00 95 00 96 00 
            13 | 97 00 98 00 99 00 9A 00 9B 00 9C 00 9D 00 9E 00 
            14 | 9F 00 A0 00 A1 00 A2 00 A3 00 A4 00 A5 00 A6 00 
            15 | A7 00 A8 00 A9 00 FA FF AB 00 AC 00 AD 00 AE 00 
            16 | AF 00 B0 00 B1 00 B2 00 B3 00 B4 00 B5 00 B6 00 
            17 | B7 00 B8 00 B9 00 BA 00 BB 00 BC 00 BD 00 BE 00 
            18 | BF 00 C0 00 C1 00 C2 00 C3 00 C4 00 FA FF C6 00 
            19 | FC FF FC FF FC FF FC FF FC FF FC FF FC FF FC FF 
            1A | FC FF FC FF FC FF FC FF FC FF FC FF FC FF FC FF 
            1B | FC FF FC FF FC FF FC FF FC FF FC FF FC FF FC FF 
            1C | FC FF FC FF FC FF FC FF FC FF FC FF FC FF FC FF 
            1D | FC FF FC FF FC FF FC FF FC FF FC FF FC FF FC FF 
            1E | FC FF FA FF F1 00 F2 00 F3 00 F4 00 F5 00 F6 00 
            1F | F7 00 F8 00 F9 00 FA 00 FB 00 FC 00 FA FF FA FF 
            """, expectedFatBlock);

        var expectedDirectoryBlock = ((ReadOnlySpan<byte>)fileSystem2.GetBlock(FileSystem.DirectoryTableLastBlockId)).AsHexBlock();
        Assert.Equal<object>("""
               | 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 
            00 | 33 80 C7 00 49 43 4F 4E 44 41 54 41 5F 56 4D 53 
            01 | 20 23 06 10 21 25 03 06 02 00 00 00 00 00 00 00 
            02 | CC FF 00 00 53 2E 41 52 43 41 44 49 41 5F 56 4D 
            03 | 20 25 09 29 18 32 18 01 53 00 01 00 00 00 00 00 
            04 | 33 80 C5 00 53 2E 41 52 43 41 44 49 41 30 30 31 
            05 | 20 23 07 19 22 47 30 03 1B 00 00 00 00 00 00 00 
            06 | 33 80 AA 00 53 2E 41 52 43 41 44 49 41 30 30 32 
            07 | 20 25 08 29 20 42 44 05 1B 00 00 00 00 00 00 00 
            08 | 33 80 8F 00 53 2E 41 52 43 41 44 49 41 30 30 33 
            09 | 20 25 09 13 10 32 30 06 1B 00 00 00 00 00 00 00 
            0A | 33 FF 74 00 56 4D 55 54 4F 4F 4C 5F 5F 4F 50 54 
            0B | 20 23 06 11 15 22 09 00 08 00 00 00 00 00 00 00 
            0C | 33 FF 6C 00 5A 4F 4D 42 49 45 5F 55 5F 53 59 53 
            0D | 20 25 09 14 12 45 44 00 04 00 00 00 00 00 00 00 
            0E | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            0F | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            10 | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            11 | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            12 | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            13 | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            14 | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            15 | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            16 | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            17 | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            18 | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            19 | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            1A | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            1B | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            1C | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            1D | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            1E | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            1F | 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            """,
            expectedDirectoryBlock);

        // Round trip from flash back to VMS folder
        var vmsFolder2 = _tempRoot.CreateSubdirectory("ReadFiles_02");
        (ok, error) = fileSystem2.TryReadAllFiles(vmsFolder2);
        Assert.True(ok, error);

        // All files in 'vmsFolder' are present in 'vmsFolder2'
        foreach (var info in vmsFolder.EnumerateFileSystemInfos())
        {
            Assert.True(File.Exists(Path.Combine(vmsFolder2.FullName, info.Name)));
        }

        // All files in 'vmsFolder2' have exactly the same content as corresponding file in 'vmsFolder'
        foreach (var info in vmsFolder2.EnumerateFileSystemInfos())
        {
            var expected = File.ReadAllBytes(Path.Combine(vmsFolder.FullName, info.Name));
            var actual = File.ReadAllBytes(info.FullName);
            Assert.Equal(expected, actual);
        }
    }

    // Additional WriteFiles test cases:
    // - No game file
    // - Multiple game files
    // - Insufficient VMU space (too many files)
    // - vmi file missing
    // - vmi+vms renamed together
    // - only vmi renamed
    // - only vms renamed
    // - duplicate vmu filenames in vmi's
    //
    // Additional Flush test cases:
    // - File deleted
    // - File added
    // - File modified
}
