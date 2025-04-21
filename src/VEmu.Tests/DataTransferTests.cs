using VEmu.Core;

namespace VEmu.Tests;

public class DataTransferTests
{
    [Fact]
    public void LD_Direct_Example1()
    {
        // VMC-184
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.LD.Compose(AddressingMode.Direct0), 0x70,
            OpcodePrefix.LD.Compose(AddressingMode.Direct0), 0x71,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xff;
        cpu.RamBank0[0x70] = 0x55;
        cpu.RamBank0[0x71] = 0xaa;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x55, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xaa, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }
}