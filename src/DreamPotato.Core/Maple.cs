namespace DreamPotato.Core;

internal struct MapleMessage
{
    public bool HasValue;

    public byte Type;
    public byte Recipient; // TODO: sticking an 0x20 onto the recipient ID seems to indicate a request to enumerate attached devices (e.g. VMUs and jump packs)
    public byte Sender;

    /// <summary>
    /// The number of additional 32-bit words being sent in the packet.
    /// </summary>
    public byte Length;

    public byte Checksum;

    public byte[] AdditionalWords;

    public byte[] RawBytes;
}

enum DreamcastPort
{
    A = 0,
    B = 1,
    C = 2,
    D = 3,
}

enum DreamcastSlot
{
    // i.e. the Dreamcast itself is being addressed.
    Dreamcast = 0,

    Slot1 = 1,
    Slot2 = 2,
}

struct MapleAddress
{
    private byte _value;

    public MapleAddress(byte value) => _value = value;
    public static explicit operator byte(MapleAddress value) => value._value;

    DreamcastPort Port => (DreamcastPort)(_value >> 6);
    DreamcastSlot Slot => (DreamcastSlot)(_value & 0b0001_1111);
}

// Derived from https://dmitry.gr/index.php?r=05.Projects&proj=25.%20VMU%20Hacking.
// Corresponds to 'enum MapleDeviceCommand' in flycast.
enum MapleMessageType
{
    GetDeviceInfo = 1,
    GetExtendedDeviceInfo = 2,
    ResetDevice = 3,
    ShutdownDevice = 4,

    /// <summary>Reply to GetDeviceInfo.</summary>
    DeviceInfoTransfer = 5,
    /// <summary>Reply to GetExtendedDeviceInfo.</summary>
    ExtendedDeviceInfoTransfer = 6,

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