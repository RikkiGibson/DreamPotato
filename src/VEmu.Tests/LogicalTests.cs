using VEmu.Core;

namespace VEmu.Tests;

public class LogicalTests
{

    [Fact]
    public void AND_Immediate_Example1()
    {
        // VMC-173
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xfa,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xaf,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0x0f,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xf0,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xff;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xfa, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xaa, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x0a, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void AND_Immediate_Example2()
    {
        // VMC-173
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xfe,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xfd,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xfb,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xf7,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xef,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xdf,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0xbf,
            OpcodePrefix.AND.Compose(AddressingMode.Immediate), 0x7f,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xff;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xfe, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xfc, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xf8, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xf0, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xe0, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xc0, cpu.SFRs.Acc); // The book says 0xc9, but that doesn't seem to make sense
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x80, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void OR_Direct_Example1()
    {
        // TODO: this one will also be good to return to once MOV is implemented

        // VMC-173
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.OR.Compose(AddressingMode.Direct0), 0x23,
            OpcodePrefix.OR.Compose(AddressingMode.Direct0), 0x23,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0x0;
        cpu.RamBank0[0x23] = 0x55;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x55, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        cpu.RamBank0[0x23] = 0xaa;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }
}