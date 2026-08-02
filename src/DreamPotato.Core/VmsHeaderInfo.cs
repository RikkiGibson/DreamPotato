using System.Buffers.Binary;

namespace DreamPotato.Core;

/// <summary>https://vmu.falcogirgis.net/formats.html#formats_vms_header</summary>
public class VmsHeaderInfo
{
    public const int Size = 0x80; // 128 bytes

    public VmsHeaderInfo(Memory<byte> data)
    {
        if (data.Length != Size)
            throw new ArgumentException(null, nameof(data));

        this.data = data;
    }

    private readonly Memory<byte> data;

    /// <summary>Displayed in the VMU BIOS.</summary>
    public Memory<byte> VmuDescription => data.Slice(0, length: 0x10);

    /// <summary>Displayed in the Dreamcast BIOS.</summary>
    public Memory<byte> DreamcastDescription => data.Slice(0x10, length: 0x20);

    public Memory<byte> ApplicationId => data.Slice(0x30, length: 0x10);

    public ushort IconCount
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(data.Span.Slice(0x40, length: 2));
        set => BinaryPrimitives.WriteUInt16LittleEndian(data.Span.Slice(0x40, length: 2), value);
    }

    public ushort AnimationSpeed
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(data.Span.Slice(0x42, length: 2));
        set => BinaryPrimitives.WriteUInt16LittleEndian(data.Span.Slice(0x42, length: 2), value);
    }

    public ushort EyecatchType
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(data.Span.Slice(0x44, length: 2));
        set => BinaryPrimitives.WriteUInt16LittleEndian(data.Span.Slice(0x44, length: 2), value);
    }

    public ushort CrcChecksum
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(data.Span.Slice(0x46, length: 2));
        set => BinaryPrimitives.WriteUInt16LittleEndian(data.Span.Slice(0x46, length: 2), value);
    }

    public int DataBytes
    {
        get => BinaryPrimitives.ReadInt32LittleEndian(data.Span.Slice(0x48, length: 4));
        set => BinaryPrimitives.WriteInt32LittleEndian(data.Span.Slice(0x48, length: 4), value);
    }

    public Memory<byte> Palette => data.Slice(0x60, length: 0x20);
}
