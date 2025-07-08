using System.Text;

using DreamPotato.Core;

namespace DreamPotato.Tests;

public class MapleTests
{
    [Fact]
    public void GetDeviceStatus()
    {
        var rom = File.ReadAllBytes("Data/american_v1.05.bin");
        var cpu = new Cpu();
        var messageBroker = cpu.MapleMessageBroker;
        rom.AsSpan().CopyTo(cpu.ROM);
        cpu.Reset();
        cpu.ConnectDreamcast();

        // MDCF_GetCondition, destAP (requesting attached devices), originAP, length, MFID_0_Input
        var deviceStatusMessage = "09 20 00 01 00 00 00 01\r\n"u8;
        Queue<MapleMessage> inbound = [];
        messageBroker.ScanAsciiHexFragment(asciiMessageBuilder: [], inbound, deviceStatusMessage);
        cpu.Run(ticksToRun: TimeSpan.TicksPerMillisecond);
        var message = messageBroker.HandleMapleMessage(inbound.Dequeue());
        Assert.True(message.HasValue);
        Assert.Equal(MapleMessageType.Ack, message.Type);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Dreamcast }, (byte)message.Recipient);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Slot1 }, (byte)message.Sender);
        Assert.Equal(0, message.Length);
        Assert.Empty(message.AdditionalWords);
    }

    [Fact]
    public void SetConditionClock()
    {
        var rom = File.ReadAllBytes("Data/american_v1.05.bin");
        var cpu = new Cpu();
        var messageBroker = cpu.MapleMessageBroker;
        rom.AsSpan().CopyTo(cpu.ROM);
        cpu.Reset();
        cpu.ConnectDreamcast();

        // MDCF_SetCondition, destAP (VMU in slot 0), originAP (Dreamcast), length (???), MFID_3_Clock
        // Apparently this function is used to make the VMU beep. The first two bytes are meaningful, the second two are discarded(?)
        // The first is a period, the second is a duty cycle, for PWM.
        var setCondition = "0E 01 00 02 00 00 00 08 00 00 00 00\r\n"u8;
        Queue<MapleMessage> inbound = [];
        messageBroker.ScanAsciiHexFragment(asciiMessageBuilder: [], inbound, setCondition);
        cpu.Run(ticksToRun: TimeSpan.TicksPerMillisecond);
        var message = messageBroker.HandleMapleMessage(inbound.Dequeue());
        Assert.False(message.HasValue); // no reply expected
    }

    [Fact]
    public void Fragment_01()
    {
        var rom = File.ReadAllBytes("Data/american_v1.05.bin");
        var cpu = new Cpu();
        var messageBroker = cpu.MapleMessageBroker;
        rom.AsSpan().CopyTo(cpu.ROM);
        cpu.Reset();
        cpu.ConnectDreamcast();

        // MDCF_SetCondition, destAP (VMU in slot 0), originAP (Dreamcast), length (???), MFID_3_Clock
        // Apparently this function is used to make the VMU beep. The first two bytes are meaningful, the second two are discarded(?)
        // The first is a period, the second is a duty cycle, for PWM.
        var setCondition1 = "0E 01 00 02 "u8;
        var setCondition2 = "00 00 00 08 00 00 00 00\r\n"u8;
        Queue<MapleMessage> inbound = [];
        List<byte> asciiMessageBuilder = [];
        messageBroker.ScanAsciiHexFragment(asciiMessageBuilder, inbound, setCondition1);
        messageBroker.ScanAsciiHexFragment(asciiMessageBuilder, inbound, setCondition2);
        cpu.Run(ticksToRun: TimeSpan.TicksPerMillisecond);

        var message = messageBroker.HandleMapleMessage(inbound.Dequeue());
        Assert.False(message.HasValue); // No reply expected
    }

    [Fact]
    public void WriteLcd()
    {
        var rom = File.ReadAllBytes("Data/american_v1.05.bin");
        var cpu = new Cpu();
        var messageBroker = cpu.MapleMessageBroker;
        rom.AsSpan().CopyTo(cpu.ROM);
        cpu.Reset();
        cpu.ConnectDreamcast();

        // MDCF_BlockWrite, destAP (VMU in slot 0), originAP (Dreamcast), length, MFID_2_LCD
        var writeLcdMessage = "0C 01 00 32 00 00 00 04 00 00 00 00 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF F1 8F FF FF FF FF F5 AF FF FF FF FF F2 4F FF FF FF FF DC 3B FF FF FF FF C9 97 FF FF FF FF 2B D4 FF FF FF BC 57 E8 3D FF FF 98 9C 38 19 FF FF D1 30 0C 8B FF FF F2 67 E6 47 FF FF E0 C8 13 23 FF FF A1 99 99 8F FF FF A1 F1 87 99 FF FF 21 4C 30 9A FF FE E1 32 78 8F FF FE 27 2D 7C C5 7F FC ED A9 1D C1 BF F9 3A 93 05 4B 1F FD B4 CC 83 CB FF E2 78 42 42 4A 47 FF 80 E1 27 2F FF C0 03 F8 9F A4 03 80 07 FC 5F E8 01 8F FF FF FF FF F1 BF FF FF FF FF FD FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF\r\n";
        var messageBytes = Encoding.UTF8.GetBytes(writeLcdMessage);
        Queue<MapleMessage> inbound = [];
        messageBroker.ScanAsciiHexFragment(asciiMessageBuilder: [], inbound, messageBytes);
        var message = messageBroker.HandleMapleMessage(inbound.Dequeue());
        cpu.Run(ticksToRun: TimeSpan.TicksPerMillisecond);

        var display = new Display(cpu);
        Assert.Equal<object>("""
            ████████████████████████████████████████████████
            ████████████████████▀▀▀██▀▀▀████████████████████
            ████████████████████ ▀▄▀▀▄▀ ████████████████████
            ██████████████████ ▀█▀ ▄▄ ▀█▀▄██████████████████
            █████████▀████▀▀ ▄▀▄▀▄████▄▀▄▀  ▀▀████▀█████████
            █████████▄ █▀  ▄▀ ▄█▀▀    ▀▀█▄  ▄  ▀█ ▄█████████
            ███████████▀  ▀ ▄█▀ ▄▀▀▀▀▀▀▄ ▀█▄ ▀▄  ▀██████████
            █████████ █    ██▄▄█▀  ██  ▀▀▄▄██  ▄█▀▀█████████
            ███████▀▄▄█    █ ▀▄▄▀▀▄  ▄██▄   █  ▀█▄█▄████████
            ██████▀ ▄▄█ ▄█▀█▄ █ █▀ █ ▀▀███ ▄██   ▀ █▄▀██████
            █████▄ █▄ ██▀▄▀ █▄ ▀▄▄▀▀▄    ▀▄█▄█  █ ██▄▄▄█████
            ███▄▄▄█▄▄▀▀▀▀   ▄█▄   ▀▄ ▀▄  ▄█▄ ▀▄ █▄█▄▄█▄▄▄███
            █▀           ▄███████▄  ▀▄ ██████▄█ ▄▀        ▀█
            █ ▄▄████████████████████████████████████████▄▄ █
            ████████████████████████████████████████████████
            ████████████████████████████████████████████████

            """, display.GetBlockString());

        // No response is expected for an LCD write
        Assert.False(message.HasValue);
    }

    [Fact]
    public void ReadBlock_01()
    {
        var rom = File.ReadAllBytes("Data/american_v1.05.bin");
        var cpu = new Cpu();
        var messageBroker = cpu.MapleMessageBroker;
        rom.AsSpan().CopyTo(cpu.ROM);

        // fill the block we are going to read from
        byte value = 0;
        for (int i = 0; i < Memory.WorkRamSize; i++)
            cpu.FlashBank1[i + 0xFE00] = value++;

        cpu.Reset();
        cpu.ConnectDreamcast();

        // MDCF_ReadBlock, destAP (VMU in slot 0), originAP (Dreamcast), length, MFID_1_Storage, block number
        var readBlock = "0B 01 00 02 00 00 00 02 00 00 00 FF\r\n"u8;
        Queue<MapleMessage> inbound = [];
        messageBroker.ScanAsciiHexFragment(asciiMessageBuilder: [], inbound, readBlock);
        cpu.Run(ticksToRun: TimeSpan.TicksPerMillisecond);
        var message = messageBroker.HandleMapleMessage(inbound.Dequeue());

        Assert.True(message.HasValue);
        Assert.Equal(MapleMessageType.DataTransfer, message.Type);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Dreamcast }, (byte)message.Recipient);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Slot1 }, (byte)message.Sender);
        Assert.Equal(130, message.Length);
        Assert.Equal(130, message.AdditionalWords.Length);
        Assert.Equal((int)MapleFunction.Storage, message.AdditionalWords[0]);
        Assert.Equal(unchecked((int)0xff00_0000), message.AdditionalWords[1]);
        Assert.Equal(0x03020100, message.AdditionalWords[2]);
        Assert.Equal(unchecked((int)0xfffe_fdfc), message.AdditionalWords[^1]);

        var rawSocketData = new byte[2048];
        var length = cpu.MapleMessageBroker.EncodeAsciiHexData(message, rawSocketData);
        var outboundMessageString = Encoding.UTF8.GetString(rawSocketData.AsSpan(start: 0, length));
        Assert.Equal<object>("""
            08 00 01 82 00 00 00 02 00 00 00 FF 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10 11 12 13 14 15 16 17 18 19 1A 1B 1C 1D 1E 1F 20 21 22 23 24 25 26 27 28 29 2A 2B 2C 2D 2E 2F 30 31 32 33 34 35 36 37 38 39 3A 3B 3C 3D 3E 3F 40 41 42 43 44 45 46 47 48 49 4A 4B 4C 4D 4E 4F 50 51 52 53 54 55 56 57 58 59 5A 5B 5C 5D 5E 5F 60 61 62 63 64 65 66 67 68 69 6A 6B 6C 6D 6E 6F 70 71 72 73 74 75 76 77 78 79 7A 7B 7C 7D 7E 7F 80 81 82 83 84 85 86 87 88 89 8A 8B 8C 8D 8E 8F 90 91 92 93 94 95 96 97 98 99 9A 9B 9C 9D 9E 9F A0 A1 A2 A3 A4 A5 A6 A7 A8 A9 AA AB AC AD AE AF B0 B1 B2 B3 B4 B5 B6 B7 B8 B9 BA BB BC BD BE BF C0 C1 C2 C3 C4 C5 C6 C7 C8 C9 CA CB CC CD CE CF D0 D1 D2 D3 D4 D5 D6 D7 D8 D9 DA DB DC DD DE DF E0 E1 E2 E3 E4 E5 E6 E7 E8 E9 EA EB EC ED EE EF F0 F1 F2 F3 F4 F5 F6 F7 F8 F9 FA FB FC FD FE FF 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10 11 12 13 14 15 16 17 18 19 1A 1B 1C 1D 1E 1F 20 21 22 23 24 25 26 27 28 29 2A 2B 2C 2D 2E 2F 30 31 32 33 34 35 36 37 38 39 3A 3B 3C 3D 3E 3F 40 41 42 43 44 45 46 47 48 49 4A 4B 4C 4D 4E 4F 50 51 52 53 54 55 56 57 58 59 5A 5B 5C 5D 5E 5F 60 61 62 63 64 65 66 67 68 69 6A 6B 6C 6D 6E 6F 70 71 72 73 74 75 76 77 78 79 7A 7B 7C 7D 7E 7F 80 81 82 83 84 85 86 87 88 89 8A 8B 8C 8D 8E 8F 90 91 92 93 94 95 96 97 98 99 9A 9B 9C 9D 9E 9F A0 A1 A2 A3 A4 A5 A6 A7 A8 A9 AA AB AC AD AE AF B0 B1 B2 B3 B4 B5 B6 B7 B8 B9 BA BB BC BD BE BF C0 C1 C2 C3 C4 C5 C6 C7 C8 C9 CA CB CC CD CE CF D0 D1 D2 D3 D4 D5 D6 D7 D8 D9 DA DB DC DD DE DF E0 E1 E2 E3 E4 E5 E6 E7 E8 E9 EA EB EC ED EE EF F0 F1 F2 F3 F4 F5 F6 F7 F8 F9 FA FB FC FD FE FF

            """,
            outboundMessageString);
    }

    [Fact]
    public void WriteBlock_01()
    {
        var rom = File.ReadAllBytes("Data/american_v1.05.bin");
        var cpu = new Cpu();
        var messageBroker = cpu.MapleMessageBroker;
        rom.AsSpan().CopyTo(cpu.ROM);

        // fill the block we are going to read from
        byte value = 0;
        for (int i = 0; i < Memory.WorkRamSize; i++)
            cpu.FlashBank1[i + 0xFE00] = value++;

        cpu.Reset();
        cpu.ConnectDreamcast();

        // MDCF_WriteBlock, destAP (VMU in slot 0), originAP (Dreamcast), length, MFID_1_Storage, block number
        var writeBlock = """
        0C 01 00 22 00 00 00 02 00 00 00 0A 41 72 63 61 64 69 61 20 49 63 6F 6E 20 20 20 20 20 00 00 00 A0 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF F7 FF FF FF E7 FD 1F FF E7 F4 87 FF D3 E2 43 FF D3 C1 31 FB 93 80 AC FB 93 B0 52 F3 93 3D 5A 61 89 3F DD 41 B1 3F 52 41 F9 DE 2D C1 F9 EC 97 C1 F5 99 CC 91 C4 CC 19 93 E4 E4 93 23 E2 67 F3 63 F1 3A AE C7 F8 1C 1D 8F FC 0B F7 1F

        """u8;
        Queue<MapleMessage> inbound = [];
        messageBroker.ScanAsciiHexFragment(asciiMessageBuilder: [], inbound, writeBlock);
        var message = inbound.Dequeue();

        cpu.Run(ticksToRun: TimeSpan.TicksPerMillisecond);

        Assert.True(message.HasValue);
        Assert.Equal(MapleMessageType.WriteBlock, message.Type);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Slot1 }, (byte)message.Recipient);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Dreamcast }, (byte)message.Sender);
        Assert.Equal(34, message.Length);
        Assert.Equal(34, message.AdditionalWords.Length);

        var outboundMessage = messageBroker.HandleMapleMessage(message);
        Assert.True(outboundMessage.HasValue);
        Assert.Equal(MapleMessageType.Ack, outboundMessage.Type);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Dreamcast }, (byte)outboundMessage.Recipient);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Slot1 }, (byte)outboundMessage.Sender);
        Assert.Equal(0, outboundMessage.Length);
        Assert.Empty(outboundMessage.AdditionalWords);
    }

    [Fact]
    public void WriteComplete_01()
    {
        var rom = File.ReadAllBytes("Data/american_v1.05.bin");
        var cpu = new Cpu();
        var messageBroker = cpu.MapleMessageBroker;
        rom.AsSpan().CopyTo(cpu.ROM);

        // fill the block we are going to read from
        byte value = 0;
        for (int i = 0; i < Memory.WorkRamSize; i++)
            cpu.FlashBank1[i + 0xFE00] = value++;

        cpu.Reset();
        cpu.ConnectDreamcast();

        // MDCF_GetLastError, destAP (VMU in slot 0), originAP (Dreamcast), length, MFID_1_Storage, block/phase number
        var writeBlock = """
        0D 01 00 02 00 00 00 02 00 04 00 A2

        """u8;
        Queue<MapleMessage> inbound = [];
        messageBroker.ScanAsciiHexFragment(asciiMessageBuilder: [], inbound, writeBlock);
        cpu.Run(ticksToRun: TimeSpan.TicksPerMillisecond);
        var message = messageBroker.HandleMapleMessage(inbound.Dequeue());
        Assert.True(message.HasValue);
        Assert.Equal(MapleMessageType.Ack, message.Type);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Dreamcast }, (byte)message.Recipient);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Slot1 }, (byte)message.Sender);
        Assert.Equal(0, message.Length);
        Assert.Empty(message.AdditionalWords);

        var rawSocketData = new byte[2048];
        var length = cpu.MapleMessageBroker.EncodeAsciiHexData(message, rawSocketData);
        var outboundMessageString = Encoding.UTF8.GetString(rawSocketData.AsSpan(start: 0, length));
        Assert.Equal<object>("""
            07 00 01 00

            """,
            outboundMessageString);
    }

    [Fact]
    public void Reconnect()
    {
        var rom = File.ReadAllBytes("Data/american_v1.05.bin");
        var cpu = new Cpu();
        rom.AsSpan().CopyTo(cpu.ROM);

        cpu.Reset();

        // Changing the connection state causes us to send a reconnect message
        cpu.ConnectDreamcast();

        var message = new MapleMessage() { Type = (MapleMessageType)0xff, Sender = new MapleAddress(0xff), Recipient = new MapleAddress(0xff), Length = 0xff, AdditionalWords = [] };
        var rawSocketData = new byte[2048];
        var length = cpu.MapleMessageBroker.EncodeAsciiHexData(message, rawSocketData);
        var outboundMessageString = Encoding.UTF8.GetString(rawSocketData.AsSpan(start: 0, length));
        Assert.Equal<object>("""
            FF FF FF FF

            """,
            outboundMessageString);
        Assert.Equal(13, outboundMessageString.Length);
    }

    // test write nonzero phase:
    // Received message: 0C 01 00 22 00 00 00 02 00 03 00 A2 22 22 22 17 22 16 66 66 17 72 22 21 66 66 12 22 22 22 22 11 11 16 66 66 61 11 11 11 66 66 12 22 22 22 11 16 66 61 66 66 66 66 66 66 66 61 12 22 22 11 11 16 66 61 66 66 66 66 66 66 61 11 11 11 11 10 00 11 66 66 66 66 66 66 66 66 10 00 00 01 10 00 00 01 66 66 11 11 16 66 66 61 10 00 00 00 00 00 00 01 16 66 61 11 16 66 66 11 10 00 00 00 00 00 00 00 11 66 66 11 66 66 61 18 10 00 00 00

    // test write complete:
    // 0D 01 00 02 00 00 00 02 00 04 00 A2
}