using System.Buffers.Binary;
using System.Text;

namespace DreamPotato.Core;

/// <summary>https://vmu.falcogirgis.net/formats.html#formats_vmi</summary>
public class VmiInfo
{
    public const int VmiSize = 0x6c; // 108 bytes

    public VmiInfo(byte[] data)
    {
        if (data.Length != VmiSize)
            throw new ArgumentException(null, nameof(data));

        this.data = data;

        Checksum = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0, length: 4));
        CreationTime = ReadDate(data.AsSpan(0x44, length: 8));
        Version = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(0x4c, length: 2));
        FileNumber = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(0x4e, length: 2));
        FileMode = (VmuFileMode)BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(0x64, length: 2));
        VmsFileSize = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0x68, length: 4));
    }

    private DateTimeOffset ReadDate(ReadOnlySpan<byte> source)
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

    private readonly byte[] data;

    public int Checksum { get; init; }
    public string Description => field ??= FileSystem.Encoding.GetString(data.AsSpan(4, length: 0x20));
    public string Copyright => field ??= FileSystem.Encoding.GetString(data.AsSpan(0x24, length: 0x20));
    public DateTimeOffset CreationTime { get; init; }
    public ushort Version { get; init; }
    public ushort FileNumber { get; init; }
    public string VmsResourceName => field ??= FileSystem.Encoding.GetString(data.AsSpan(0x50, length: 8));
    public ReadOnlySpan<byte> VmuFileNameBytes => data.AsSpan(0x58, 0xc);
    public string VmuFileName => field ??= FileSystem.Encoding.GetString(VmuFileNameBytes);
    public VmuFileMode FileMode { get; init; }
    public int VmsFileSize { get; init; }
}

public enum VmuFileMode : ushort
{
    CopyProtected = 1,
    Game = 2,
}
