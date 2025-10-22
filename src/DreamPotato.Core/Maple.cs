using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace DreamPotato.Core;

internal record struct MapleMessage()
{
    public bool HasValue { get; } = true;

    public bool IsResetMessage => (byte)Type == 0xff
        && (byte)Sender == 0xff
        && (byte)Recipient == 0xff
        && Length == 0xff
        && AdditionalWords.Length == 0;

    public MapleMessageType Type { get; init; }
    public MapleAddress Recipient { get; init; }
    public MapleAddress Sender { get; init; }

    public MapleFunction Function => (MapleFunction)AdditionalWords[0];

    /// <summary>
    /// The number of additional 32-bit words being sent in the packet.
    /// </summary>
    public byte Length { get; init; }

    public byte EffectiveLength => IsResetMessage ? (byte)0 : Length;

    public byte Checksum { get; init; }

    public required int[] AdditionalWords { get; init; }

    public byte[] WriteTo(byte[] buffer, out int bytesWritten)
    {
        bytesWritten = 4 * (EffectiveLength + 1);

        buffer[0] = (byte)Type;
        buffer[1] = (byte)Recipient;
        buffer[2] = (byte)Sender;
        buffer[3] = Length;
        var byteSpan = buffer.AsSpan();
        for (int i = 0; i < AdditionalWords.Length; i++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(byteSpan[(4 * (i + 1))..(4 * (i + 2))], AdditionalWords[i]);
        }

        return buffer;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter<DreamcastPort>))]
public enum DreamcastPort
{
    A = 0,
    B = 1,
    C = 2,
    D = 3,
}

public enum DreamcastSlot
{
    // i.e. the Dreamcast itself is being addressed.
    Dreamcast = 0,

    Slot1 = 1 << 0,
    Slot2 = 1 << 1,
}

struct MapleAddress
{
    private byte _value;

    public MapleAddress(byte value) => _value = value;
    public static explicit operator byte(MapleAddress value) => value._value;

    public DreamcastPort Port
    {
        get => (DreamcastPort)(_value >> 6);
        set => _value = (byte)(((byte)value << 6) | (_value & ~0b1100_0000));
    }
    public DreamcastSlot Slot
    {
        get => (DreamcastSlot)(_value & 0b0001_1111);
        // TODO: verify correctness once "slot 2" support is implemented
        set => _value = (byte)((byte)value | _value & ~0b0001_1111);
    }

    public bool EnumerateAttachedDevices => BitHelpers.ReadBit(_value, bit: 5);
}

// Derived from https://dmitry.gr/index.php?r=05.Projects&proj=25.%20VMU%20Hacking.
// Largely corresponds to 'enum MapleDeviceCommand' in flycast.
enum MapleMessageType : byte
{
    GetDeviceInfo = 1,
    GetExtendedDeviceInfo = 2,
    ResetDevice = 3,
    ShutdownDevice = 4,

    /// <summary>Reply to GetDeviceInfo.</summary>
    DeviceInfoTransfer = 5,
    /// <summary>Reply to GetExtendedDeviceInfo.</summary>
    ExtendedDeviceInfoTransfer = 6,

    /// <summary>Corresponds to 'MDRS_DeviceReply' in flycast.</summary>
    Ack = 7,

    DataTransfer = 8,
    GetCondition = 9,
    GetMemoryInfo = 0xa,
    ReadBlock = 0xb,
    WriteBlock = 0xc,
    CompleteWrite = 0xd,
    SetCondition = 0xe,
    ErrorWithCode = 0xfa,
    ErrorInvalidFlashAddress = 0xfb,
    ResendLastPacket = 0xfc,
    ErrorUnknownCommand = 0xfd,
    ErrorUnknownFunction = 0xfe,
}

enum MapleFunction
{
    Input = 0x0100_0000, // valid to combine with 'GetCondition'
    Storage = 0x0200_0000, // valid to combine with 'ReadBlock'/'WriteBlock'/'CompleteWrite'
    LCD = 0x0400_0000, // valid to combine with 'WriteBlock'
    Clock = 0x0800_0000, // valid to combine with 'SetCondition'
}
