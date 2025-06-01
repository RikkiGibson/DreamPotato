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
        // ReadOnlySpan<byte> deviceStatusMessage = [0x09, 0x20, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01];

        // NOTE: ordinarily usage of this method is restricted to the socket servicing thread.
        // Since this test does not use a socket, it's not a problem here.
        messageBroker.AppendMessageFragment(messageBytes);

        while (true)
        {
            cpu.Run(ticksToRun: TimeSpan.TicksPerMillisecond);
        }
    }
}