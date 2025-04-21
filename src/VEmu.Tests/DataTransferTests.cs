using VEmu.Core;

namespace VEmu.Tests;

public class DataTransferTests
{
    [Fact]
    public void LD_Direct_Example1()
    {
        // VMC-186
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

    [Fact]
    public void LD_Direct_Example2()
    {
        // VMC-186
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.LD.Compose(AddressingMode.Direct1), 0x02, // B
            OpcodePrefix.LD.Compose(AddressingMode.Direct1), 0x06, // SP
            OpcodePrefix.LD.Compose(AddressingMode.Direct1), 0x02, // B
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xff;
        cpu.SFRs.B = 0xf0;
        cpu.SFRs.Sp = 0x0f;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xf0, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x0f, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xf0, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void LD_Indirect_Example1()
    {
        // VMC-187
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.LD.Compose(AddressingMode.Indirect0),
            OpcodePrefix.LD.Compose(AddressingMode.Indirect1),
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xff;
        cpu.IndirectAddressRegisters[0] = 0x70;
        cpu.IndirectAddressRegisters[1] = 0x7f;
        cpu.RamBank0[0x70] = 0xf0;
        cpu.RamBank0[0x7f] = 0x0f;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xf0, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x0f, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void LD_Indirect_Example2()
    {
        // VMC-187
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.LD.Compose(AddressingMode.Indirect2),
            OpcodePrefix.LD.Compose(AddressingMode.Indirect3),
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xff;
        cpu.IndirectAddressRegisters[2] = 0x04; // Trl
        cpu.IndirectAddressRegisters[3] = 0x05; // Trh
        cpu.SFRs.Trl = 0xaa;
        cpu.SFRs.Trh = 0x55;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xaa, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x55, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void ST_Direct_Example1()
    {
        // VMC-186
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.ST.Compose(AddressingMode.Direct0), 0x70,
            OpcodePrefix.ST.Compose(AddressingMode.Direct0), 0x71,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xff;
        cpu.RamBank0[0x70] = 0x55;
        cpu.RamBank0[0x71] = 0xaa;

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0xff, cpu.RamBank0[0x70]);
        Assert.Equal(0xaa, cpu.RamBank0[0x71]);
        Assert.Equal(0, cpu.SFRs.Psw);

        cpu.SFRs.Acc = 0x00;
        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.SFRs.Acc);
        Assert.Equal(0xff, cpu.RamBank0[0x70]);
        Assert.Equal(0x00, cpu.RamBank0[0x71]);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void MOV_Direct_Example1()
    {
        // VMC-186
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct0), 0x00, 0xff,
            OpcodePrefix.MOV.Compose(AddressingMode.Direct0), 0x01, 0xfe,
            OpcodePrefix.MOV.Compose(AddressingMode.Direct0), 0x02, 0xfd,
            OpcodePrefix.MOV.Compose(AddressingMode.Direct0), 0x03, 0xfc,
            OpcodePrefix.MOV.Compose(AddressingMode.Direct0), 0x03, 0xfb,
            OpcodePrefix.MOV.Compose(AddressingMode.Direct0), 0x02, 0xfa,
            OpcodePrefix.MOV.Compose(AddressingMode.Direct0), 0x01, 0xf9,
            OpcodePrefix.MOV.Compose(AddressingMode.Direct0), 0x00, 0xf8,
        ];
        instructions.CopyTo(cpu.ROM);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xff, cpu.IndirectAddressRegisters[0]);
        Assert.Equal(0x00, cpu.IndirectAddressRegisters[1]);
        Assert.Equal(0x00, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0x00, cpu.IndirectAddressRegisters[3]);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xff, cpu.IndirectAddressRegisters[0]);
        Assert.Equal(0xfe, cpu.IndirectAddressRegisters[1]);
        Assert.Equal(0x00, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0x00, cpu.IndirectAddressRegisters[3]);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xff, cpu.IndirectAddressRegisters[0]);
        Assert.Equal(0xfe, cpu.IndirectAddressRegisters[1]);
        Assert.Equal(0xfd, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0x00, cpu.IndirectAddressRegisters[3]);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xff, cpu.IndirectAddressRegisters[0]);
        Assert.Equal(0xfe, cpu.IndirectAddressRegisters[1]);
        Assert.Equal(0xfd, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0xfc, cpu.IndirectAddressRegisters[3]);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xff, cpu.IndirectAddressRegisters[0]);
        Assert.Equal(0xfe, cpu.IndirectAddressRegisters[1]);
        Assert.Equal(0xfd, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0xfb, cpu.IndirectAddressRegisters[3]);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xff, cpu.IndirectAddressRegisters[0]);
        Assert.Equal(0xfe, cpu.IndirectAddressRegisters[1]);
        Assert.Equal(0xfa, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0xfb, cpu.IndirectAddressRegisters[3]);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xff, cpu.IndirectAddressRegisters[0]);
        Assert.Equal(0xf9, cpu.IndirectAddressRegisters[1]);
        Assert.Equal(0xfa, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0xfb, cpu.IndirectAddressRegisters[3]);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xf8, cpu.IndirectAddressRegisters[0]);
        Assert.Equal(0xf9, cpu.IndirectAddressRegisters[1]);
        Assert.Equal(0xfa, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0xfb, cpu.IndirectAddressRegisters[3]);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void MOV_Indirect_Example2()
    {
        // VMC-186
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct0), 0x02, 0x00, // ACC
            OpcodePrefix.MOV.Compose(AddressingMode.Direct0), 0x03, 0x02, // B
            OpcodePrefix.MOV.Compose(AddressingMode.Indirect2), 0xfd,
            OpcodePrefix.MOV.Compose(AddressingMode.Indirect3), 0xfc,
            OpcodePrefix.MOV.Compose(AddressingMode.Indirect2), 0xfb,
            OpcodePrefix.MOV.Compose(AddressingMode.Indirect3), 0xfa,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.IndirectAddressRegisters[2] = 0xff;
        cpu.IndirectAddressRegisters[3] = 0xff;
        cpu.SFRs.Acc = 0xff;
        cpu.SFRs.B = 0xff;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x00, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0xff, cpu.IndirectAddressRegisters[3]);
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0xff, cpu.SFRs.B);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x00, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0x02, cpu.IndirectAddressRegisters[3]);
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0xff, cpu.SFRs.B);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0x02, cpu.IndirectAddressRegisters[3]);
        Assert.Equal(0xfd, cpu.SFRs.Acc);
        Assert.Equal(0xff, cpu.SFRs.B);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0x02, cpu.IndirectAddressRegisters[3]);
        Assert.Equal(0xfd, cpu.SFRs.Acc);
        Assert.Equal(0xfc, cpu.SFRs.B);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0x02, cpu.IndirectAddressRegisters[3]);
        Assert.Equal(0xfb, cpu.SFRs.Acc);
        Assert.Equal(0xfc, cpu.SFRs.B);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.IndirectAddressRegisters[2]);
        Assert.Equal(0x02, cpu.IndirectAddressRegisters[3]);
        Assert.Equal(0xfb, cpu.SFRs.Acc);
        Assert.Equal(0xfa, cpu.SFRs.B);
        Assert.Equal(0, cpu.SFRs.Psw);
    }
}