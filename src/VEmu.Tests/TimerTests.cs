using VEmu.Core;

namespace VEmu.Tests;

public class TimerTests
{
    [Fact]
    public void T0L_1()
    {
        var cpu = new Cpu();
        cpu.Reset();

        cpu.SFRs.Ie7_MasterInterruptEnable = true;
        cpu.SFRs.T0Cnt_LowRunFlag = true;
        cpu.SFRs.T0Cnt_LowOverflowFlag = false;
        // Note that INT2 and T0L really are different interrupts. One is internal and other is external. They just happen to use the same interrupt vector address.
        cpu.SFRs.T0Cnt_LowInterruptEnable = true;
        cpu.SFRs.T0Prr = 0xfe;
        cpu.SFRs.T0Lr = 0;

        for (int i = 0; i < 0x80; i++)
        {
            cpu.Step();
        }
        Assert.Equal(0x80, cpu.SFRs.T0L);
        Assert.False(cpu.SFRs.T0Cnt_LowOverflowFlag);

        for (int i = 0; i < 0x80; i++)
        {
            cpu.Step();
        }
        Assert.Equal(0, cpu.SFRs.T0L);
        Assert.True(cpu.SFRs.T0Cnt_LowOverflowFlag);
    }
}