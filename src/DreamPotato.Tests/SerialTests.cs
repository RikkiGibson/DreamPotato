using DreamPotato.Core;
using DreamPotato.Core.SFRs;

namespace DreamPotato.Tests;

public class SerialTests
{
    [Fact]
    public void Main()
    {
        var cpuTx = new Cpu() { DreamcastSlot = DreamcastSlot.Slot1 };
        cpuTx.Reset();

        File.ReadAllBytes("TestSource/SioTx.vms").CopyTo(cpuTx.FlashBank0);
        cpuTx.SetInstructionBank(InstructionBank.FlashBank0);

        var cpuRx = new Cpu() { DreamcastSlot = DreamcastSlot.Slot2 };
        cpuRx.Reset();

        File.ReadAllBytes("TestSource/SioRx.vms").CopyTo(cpuRx.FlashBank0);
        cpuRx.SetInstructionBank(InstructionBank.FlashBank0);
        cpuRx.ConnectVmu(cpuTx);

        var quarterSecond = TimeSpan.TicksPerSecond / 4;
        var halfSecond = TimeSpan.TicksPerSecond / 2;
        cpuRx.Run(ticksToRun: quarterSecond);
    }
}