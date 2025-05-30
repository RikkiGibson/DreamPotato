using DreamPotato.Core;

namespace DreamPotato.Tests;

public class RcOscillatorTest
{
    [Fact]
    public void Main()
    {
        var cpu = new Cpu();
        cpu.Reset();

        var data = File.ReadAllBytes("TestSource/RcOscillator.vms");
        data.AsSpan().CopyTo(cpu.FlashBank0);
        cpu.SetInstructionBank(Core.SFRs.InstructionBank.FlashBank0);

        cpu.Run(ticksToRun: 1_000_000);
    }
}