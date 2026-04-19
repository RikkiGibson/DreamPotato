using System.Buffers.Binary;

namespace DreamPotato.Core;

/// <summary>https://vmu.falcogirgis.net/formats.html#formats_vmi</summary>
public class VmiInfo
{
    public const int Size = 0x6c; // 108 bytes

    public VmiInfo(Memory<byte> data)
    {
        if (data.Length != Size)
            throw new ArgumentException(null, nameof(data));

        RawData = data;
    }

    public Memory<byte> RawData { get; }

    public Memory<byte> Checksum => RawData.Slice(0, length: 4);
    public Memory<byte> Description => RawData.Slice(4, length: 0x20);
    public Memory<byte> Copyright => RawData.Slice(0x24, length: 0x20);
    public Memory<byte> CreationTime => RawData.Slice(0x44, length: 8);

    public DateTimeOffset CreationDateTimeOffset
    {
        get => FileSystem.ReadDate(CreationTime.Span);
        set => FileSystem.WriteDate(CreationTime.Span, value);
    }

    public ushort Version
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(RawData.Span.Slice(0x4c, length: 2));
        set => BinaryPrimitives.WriteUInt16LittleEndian(RawData.Span.Slice(0x4c, length: 2), value);
    }

    public ushort FileNumber
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(RawData.Span.Slice(0x4e, length: 2));
        set => BinaryPrimitives.WriteUInt16LittleEndian(RawData.Span.Slice(0x4e, length: 2), value);
    }

    public Memory<byte> VmsResourceName => RawData.Slice(0x50, length: 8);

    public Memory<byte> VmuFileName => RawData.Slice(0x58, 0xc);

    public VmuFileMode FileMode
    {
        get => (VmuFileMode)BinaryPrimitives.ReadUInt16LittleEndian(RawData.Span.Slice(0x64, length: 2));
        set => BinaryPrimitives.WriteUInt16LittleEndian(RawData.Span.Slice(0x64, length: 2), (ushort)value);
    }

    public int VmsFileSize
    {
        get => BinaryPrimitives.ReadInt32LittleEndian(RawData.Span.Slice(0x68, length: 4));
        set => BinaryPrimitives.WriteInt32LittleEndian(RawData.Span.Slice(0x68, length: 4), value);
    }
}

public enum VmuFileMode : ushort
{
    CopyProtected = 1,
    Game = 2,
}
