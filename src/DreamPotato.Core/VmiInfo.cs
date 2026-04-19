using System.Buffers.Binary;
using System.Text;

namespace DreamPotato.Core;

/// <summary>https://vmu.falcogirgis.net/formats.html#formats_vmi</summary>
public struct VmiInfo
{
    public const int Size = 0x6c; // 108 bytes

    public VmiInfo(Memory<byte> data)
    {
        if (data.Length != Size)
            throw new ArgumentException(null, nameof(data));

        this.data = data;

        Checksum = BinaryPrimitives.ReadInt32LittleEndian(data.Span.Slice(0, length: 4));
        CreationTime = FileSystem.ReadDate(data.Span.Slice(0x44, length: 8));
        Version = BinaryPrimitives.ReadUInt16LittleEndian(data.Span.Slice(0x4c, length: 2));
        FileNumber = BinaryPrimitives.ReadUInt16LittleEndian(data.Span.Slice(0x4e, length: 2));
        FileMode = (VmuFileMode)BinaryPrimitives.ReadUInt16LittleEndian(data.Span.Slice(0x64, length: 2));
        VmsFileSize = BinaryPrimitives.ReadInt32LittleEndian(data.Span.Slice(0x68, length: 4));
    }

    private readonly Memory<byte> data;

    public int Checksum { get; init; }
    public string Description => field ??= FileSystem.Encoding.GetString(data.Span.Slice(4, length: 0x20));
    public string Copyright => field ??= FileSystem.Encoding.GetString(data.Span.Slice(0x24, length: 0x20));
    public DateTimeOffset CreationTime { get; init; }
    public ushort Version { get; init; }
    public ushort FileNumber { get; init; }
    public string VmsResourceName => field ??= FileSystem.Encoding.GetString(data.Span.Slice(0x50, length: 8));
    public ReadOnlySpan<byte> VmuFileNameBytes => data.Span.Slice(0x58, 0xc);
    public string VmuFileName => field ??= FileSystem.Encoding.GetString(VmuFileNameBytes);
    public VmuFileMode FileMode { get; init; }
    public int VmsFileSize { get; init; }
}

public enum VmuFileMode : ushort
{
    CopyProtected = 1,
    Game = 2,
}
