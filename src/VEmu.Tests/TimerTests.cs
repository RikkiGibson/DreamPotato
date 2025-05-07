using System.Runtime.Intrinsics.X86;

using VEmu.Core;
using VEmu.Core.SFRs;

namespace VEmu.Tests;

public class TimerTests
{
    [Fact]
    public void T0L_1()
    {
        var cpu = new Cpu();
        cpu.Reset();

        cpu.SFRs.Ie = new() { MasterInterruptEnable = true };
        // Note that INT2 and T0L really are different interrupts. One is internal and other is external. They just happen to use the same interrupt vector address.
        cpu.SFRs.T0Cnt = new() { T0lRun = true, T0lOvf = false, T0lIe = true };
        cpu.SFRs.T0Prr = 0xfe;
        cpu.SFRs.T0Lr = 0;

        for (int i = 0; i < 0x80; i++)
        {
            cpu.Step();
        }
        Assert.Equal(0x80, cpu.SFRs.T0L);
        Assert.False(cpu.SFRs.T0Cnt.T0lOvf);

        for (int i = 0; i < 0x80; i++)
        {
            cpu.Step();
        }
        Assert.Equal(0, cpu.SFRs.T0L);
        Assert.True(cpu.SFRs.T0Cnt.T0lOvf);
    }

    [Fact]
    public void T0_16BitMode_1()
    {
        var cpu = new Cpu();
        cpu.Reset();
        // Note that the program here consists entirely of NOPs.

        cpu.SFRs.Ie = new() { MasterInterruptEnable = true };
        cpu.SFRs.T0Cnt = new T0Cnt() { T0lRun = true, T0lIe = true, T0Long = true, T0hRun = true, T0hIe = true };
        cpu.SFRs.T0Prr = 0xfe;
        cpu.SFRs.T0Lr = 0;
        cpu.SFRs.T0Hr = 0;

        for (int i = 0; i < 0x80; i++)
        {
            cpu.Step();
        }
        Assert.Equal(0x80, cpu.SFRs.T0L);
        Assert.Equal(0, cpu.SFRs.T0H);
        Assert.False(cpu.SFRs.T0Cnt.T0lOvf);
        Assert.False(cpu.SFRs.T0Cnt.T0hOvf);
        Assert.Equal(Interrupts.None, cpu.Interrupts);

        for (int i = 0; i < 0x80; i++)
        {
            cpu.Step();
        }
        Assert.Equal(0, cpu.SFRs.T0L);
        Assert.Equal(1, cpu.SFRs.T0H);
        Assert.False(cpu.SFRs.T0Cnt.T0lOvf); // NB: T0lOvf is not used in 16-bit mode
        Assert.False(cpu.SFRs.T0Cnt.T0hOvf);
        Assert.Equal(Interrupts.None, cpu.Interrupts); // TODO: it is unclear to me if T0L interrupt should be generated in 16-bit mode.

        for (int i = 0; i < 0xff00; i++)
        {
            cpu.Step();
        }
        Assert.Equal(0, cpu.Pc); // Pc also overflowed, being the same size as (T0H, T0L).
        Assert.Equal(0, cpu.SFRs.T0L);
        Assert.Equal(0, cpu.SFRs.T0H);
        Assert.False(cpu.SFRs.T0Cnt.T0lOvf);
        Assert.True(cpu.SFRs.T0Cnt.T0hOvf);
        Assert.Equal(Interrupts.T0H, cpu.Interrupts);

        // TODO: note that interrupt servicing sets PC right before executing next instruction.
        // So there isn't a point we can externally observe Pc being exactly the same value as the interrupt vector.
        cpu.Step();
        Assert.Equal(InterruptVectors.T0H+1, cpu.Pc);
        Assert.Equal(Interrupts.None, cpu.Interrupts);
    }
}