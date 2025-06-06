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
        Assert.True(message.HasValue);
        Assert.Equal(MapleMessageType.Ack, message.Type);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Dreamcast }, (byte)message.Recipient);
        Assert.Equal((byte)new MapleAddress { Port = DreamcastPort.A, Slot = DreamcastSlot.Slot1 }, (byte)message.Sender);
        Assert.Equal(0, message.Length);
        Assert.Empty(message.AdditionalWords);
    }

    // flash read write 512 bytes
}