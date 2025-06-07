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
        var deviceStatusMessage = "09 20 00 01 00 00 00 01\r\n";
        var messageBytes = Encoding.UTF8.GetBytes(deviceStatusMessage);
        messageBroker.ScanAsciiHexFragment(messageBytes);
        cpu.Run(ticksToRun: TimeSpan.TicksPerMillisecond);
        var message = messageBroker.DequeueMessage_TestingOnly();
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
        var setCondition = "0E 01 00 02 00 00 00 08 00 00 00 00\r\n";
        var messageBytes = Encoding.UTF8.GetBytes(setCondition);
        messageBroker.ScanAsciiHexFragment(messageBytes);
        cpu.Run(ticksToRun: TimeSpan.TicksPerMillisecond);
        var message = messageBroker.DequeueMessage_TestingOnly();
        Assert.True(message.HasValue);
        Assert.Equal(MapleMessageType.Ack, message.Type);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Dreamcast }, (byte)message.Recipient);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Slot1 }, (byte)message.Sender);
        Assert.Equal(0, message.Length);
        Assert.Empty(message.AdditionalWords);
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
        messageBroker.ScanAsciiHexFragment(setCondition1);
        messageBroker.ScanAsciiHexFragment(setCondition2);
        cpu.Run(ticksToRun: TimeSpan.TicksPerMillisecond);
        var message = messageBroker.DequeueMessage_TestingOnly();
        Assert.True(message.HasValue);
        Assert.Equal(MapleMessageType.Ack, message.Type);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Dreamcast }, (byte)message.Recipient);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Slot1 }, (byte)message.Sender);
        Assert.Equal(0, message.Length);
        Assert.Empty(message.AdditionalWords);
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
        messageBroker.ScanAsciiHexFragment(messageBytes);
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

        var message = messageBroker.DequeueMessage_TestingOnly();
        // No response is expected for an LCD write?
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
        messageBroker.ScanAsciiHexFragment(readBlock);
        cpu.Run(ticksToRun: TimeSpan.TicksPerMillisecond);
        var message = messageBroker.DequeueMessage_TestingOnly();

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

    // flash read write 512 bytes
}