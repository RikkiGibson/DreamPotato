using DreamPotato.Core;
using DreamPotato.Core.SFRs;

namespace DreamPotato.Tests;

public class HelloWorldTest(ITestOutputHelper outputHelper)
{
    [Fact]
    public void HelloWorld()
    {
        // Execute the assembled version of 'helloworld.s'
        var cpu = new Cpu() { DreamcastSlot = DreamcastSlot.Slot1 };
        cpu.Reset();

        File.ReadAllBytes("TestSource/helloworld.vms").CopyTo(cpu.FlashBank0);
        cpu.SetInstructionBank(InstructionBank.FlashBank0);

        try
        {
            var ticks = 100 * TimeSpan.TicksPerMillisecond;
            Assert.Equal(1000055, cpu.Run(ticks));
            var display = new Display(cpu);

            Assert.Equal<object>("""
                |█ █      █   █          █ █          █    █  █
                |█▀█ ▄██  █   █  ▄▀▄     ███ ▄▀▄ ▄▀   █  ▄▀█  █
                |█ █ ▀▄▄  ▀▄  ▀▄ ▀▄▀  █  █▀█ ▀▄▀ █    ▀▄ ▀▄█  ▄
                """,
                display.ToTestDisplayString());
        }
        catch
        {
            var logs = cpu.Logger.GetLogs(50);
            foreach (var str in logs)
            {
                outputHelper.WriteLine(str);
            }

            throw;
        }
    }
}