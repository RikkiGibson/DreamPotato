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
        cpu.Memory.Write(0x70, 0x55);
        cpu.Memory.Write(0x71, 0xaa);

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
        cpu.Memory.Write(0, 0x70);
        cpu.Memory.Write(1, 0x7f);
        cpu.Memory.Write(0x70, 0xf0);
        cpu.Memory.Write(0x7f, 0x0f);

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
        cpu.Memory.Write(2, 0x04); // Trl
        cpu.Memory.Write(3, 0x05); // Trh
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
        cpu.Memory.Write(0x70, 0x55);
        cpu.Memory.Write(0x71, 0xaa);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0xff, cpu.Memory.Read(0x70));
        Assert.Equal(0xaa, cpu.Memory.Read(0x71));
        Assert.Equal(0, cpu.SFRs.Psw);

        cpu.SFRs.Acc = 0x00;
        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.SFRs.Acc);
        Assert.Equal(0xff, cpu.Memory.Read(0x70));
        Assert.Equal(0x00, cpu.Memory.Read(0x71));
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
        Assert.Equal(0xff, cpu.Memory.Read(0));
        Assert.Equal(0x00, cpu.Memory.Read(1));
        Assert.Equal(0x00, cpu.Memory.Read(2));
        Assert.Equal(0x00, cpu.Memory.Read(3));
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xff, cpu.Memory.Read(0));
        Assert.Equal(0xfe, cpu.Memory.Read(1));
        Assert.Equal(0x00, cpu.Memory.Read(2));
        Assert.Equal(0x00, cpu.Memory.Read(3));
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xff, cpu.Memory.Read(0));
        Assert.Equal(0xfe, cpu.Memory.Read(1));
        Assert.Equal(0xfd, cpu.Memory.Read(2));
        Assert.Equal(0x00, cpu.Memory.Read(3));
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xff, cpu.Memory.Read(0));
        Assert.Equal(0xfe, cpu.Memory.Read(1));
        Assert.Equal(0xfd, cpu.Memory.Read(2));
        Assert.Equal(0xfc, cpu.Memory.Read(3));
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xff, cpu.Memory.Read(0));
        Assert.Equal(0xfe, cpu.Memory.Read(1));
        Assert.Equal(0xfd, cpu.Memory.Read(2));
        Assert.Equal(0xfb, cpu.Memory.Read(3));
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xff, cpu.Memory.Read(0));
        Assert.Equal(0xfe, cpu.Memory.Read(1));
        Assert.Equal(0xfa, cpu.Memory.Read(2));
        Assert.Equal(0xfb, cpu.Memory.Read(3));
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xff, cpu.Memory.Read(0));
        Assert.Equal(0xf9, cpu.Memory.Read(1));
        Assert.Equal(0xfa, cpu.Memory.Read(2));
        Assert.Equal(0xfb, cpu.Memory.Read(3));
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xf8, cpu.Memory.Read(0));
        Assert.Equal(0xf9, cpu.Memory.Read(1));
        Assert.Equal(0xfa, cpu.Memory.Read(2));
        Assert.Equal(0xfb, cpu.Memory.Read(3));
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

        cpu.Memory.Write(2, 0xff);
        cpu.Memory.Write(3, 0xff);
        cpu.SFRs.Acc = 0xff;
        cpu.SFRs.B = 0xff;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x00, cpu.Memory.Read(2));
        Assert.Equal(0xff, cpu.Memory.Read(3));
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0xff, cpu.SFRs.B);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x00, cpu.Memory.Read(2));
        Assert.Equal(0x02, cpu.Memory.Read(3));
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0xff, cpu.SFRs.B);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.Memory.Read(2));
        Assert.Equal(0x02, cpu.Memory.Read(3));
        Assert.Equal(0xfd, cpu.SFRs.Acc);
        Assert.Equal(0xff, cpu.SFRs.B);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.Memory.Read(2));
        Assert.Equal(0x02, cpu.Memory.Read(3));
        Assert.Equal(0xfd, cpu.SFRs.Acc);
        Assert.Equal(0xfc, cpu.SFRs.B);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.Memory.Read(2));
        Assert.Equal(0x02, cpu.Memory.Read(3));
        Assert.Equal(0xfb, cpu.SFRs.Acc);
        Assert.Equal(0xfc, cpu.SFRs.B);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x00, cpu.Memory.Read(2));
        Assert.Equal(0x02, cpu.Memory.Read(3));
        Assert.Equal(0xfb, cpu.SFRs.Acc);
        Assert.Equal(0xfa, cpu.SFRs.B);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void LDC_Example()
    {
        // VMC-192
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x05, 0x01, // Trh
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x04, 0x23, // Trl
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x00, 0x00, // Acc
            (byte)Opcode.LDC,
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x00, 0x01, // Acc
            (byte)Opcode.LDC,
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x00, 0x02, // Acc
            (byte)Opcode.LDC,
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x00, 0x03, // Acc
            (byte)Opcode.LDC,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.SFRs.Acc = 0xff;
        cpu.ROM[0x123] = 0x30;
        cpu.ROM[0x124] = 0xff;
        cpu.ROM[0x125] = 0x57;
        cpu.ROM[0x126] = 0xea;

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x01, cpu.SFRs.Trh);
        Assert.Equal(0, cpu.SFRs.Trl);
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x01, cpu.SFRs.Trh);
        Assert.Equal(0x23, cpu.SFRs.Trl);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x01, cpu.SFRs.Trh);
        Assert.Equal(0x23, cpu.SFRs.Trl);
        Assert.Equal(0, cpu.SFRs.Acc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x30, cpu.SFRs.Acc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x1, cpu.SFRs.Acc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xff, cpu.SFRs.Acc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x02, cpu.SFRs.Acc);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x57, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x03, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0xea, cpu.SFRs.Acc);
        Assert.Equal(0, cpu.SFRs.Psw);
    }

    [Fact]
    public void PUSH_Example()
    {
        // VMC-193
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x00, 0xaa, // acc
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x02, 0x55, // b
            OpcodePrefix.MOV.Compose(AddressingMode.Direct0), 0x00, 0x12, // r0
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x06, 0x1f, // sp
            (byte)OpcodePrefix.PUSH | 1, 0x00, // acc
            (byte)OpcodePrefix.PUSH | 1, 0x02, // b
            (byte)OpcodePrefix.PUSH, 0x00,
            (byte)OpcodePrefix.POP | 1, 0x02, // b
            (byte)OpcodePrefix.POP | 1, 0x00, // acc
            (byte)OpcodePrefix.POP, 0x00,
        ];
        instructions.CopyTo(cpu.ROM);

        cpu.Memory.Write(0, 0xff);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(2, cpu.Step());
        Assert.Equal(2, cpu.Step());
        Assert.Equal(2, cpu.Step());

        Assert.Equal(0xaa, cpu.SFRs.Acc);
        Assert.Equal(0x55, cpu.SFRs.B);
        Assert.Equal(0x12, cpu.Memory.Read(0));
        Assert.Equal(0x55, cpu.SFRs.B);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x20, cpu.SFRs.Sp);
        Assert.Equal(0xaa, cpu.Memory.Read(cpu.SFRs.Sp));

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x21, cpu.SFRs.Sp);
        Assert.Equal(0x55, cpu.Memory.Read(cpu.SFRs.Sp));

        Assert.Equal(2, cpu.Step());
        Assert.Equal(0x22, cpu.SFRs.Sp);
        Assert.Equal(0x12, cpu.Memory.Read(cpu.SFRs.Sp));
    }

    [Fact]
    public void XCH_Direct_Example1()
    {
        // VMC-195
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x00, 0xff, // acc
            OpcodePrefix.MOV.Compose(AddressingMode.Direct0), 0x23, 0x55,
            OpcodePrefix.XCH.Compose(AddressingMode.Direct0), 0x23,
            OpcodePrefix.XCH.Compose(AddressingMode.Direct0), 0x23,
            OpcodePrefix.XCH.Compose(AddressingMode.Direct0), 0x23,
            OpcodePrefix.XCH.Compose(AddressingMode.Direct0), 0x23,
        ];
        instructions.CopyTo(cpu.ROM);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(2, cpu.Step());

        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0x55, cpu.Memory.Read(0x23));

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x55, cpu.SFRs.Acc);
        Assert.Equal(0xff, cpu.Memory.Read(0x23));

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0x55, cpu.Memory.Read(0x23));

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x55, cpu.SFRs.Acc);
        Assert.Equal(0xff, cpu.Memory.Read(0x23));

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xff, cpu.SFRs.Acc);
        Assert.Equal(0x55, cpu.Memory.Read(0x23));
    }

    [Fact]
    public void XCH_Indirect_Example2()
    {
        // VMC-195
        var cpu = new Cpu();
        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), 0x00, 0xaa, // acc
            OpcodePrefix.MOV.Compose(AddressingMode.Direct0), 0x03, 0x04, // R3->Trl
            OpcodePrefix.MOV.Compose(AddressingMode.Indirect3), 0x55,
            OpcodePrefix.XCH.Compose(AddressingMode.Indirect3),
            OpcodePrefix.XCH.Compose(AddressingMode.Indirect3),
            OpcodePrefix.XCH.Compose(AddressingMode.Indirect3),
        ];
        instructions.CopyTo(cpu.ROM);

        Assert.Equal(2, cpu.Step());
        Assert.Equal(2, cpu.Step());
        Assert.Equal(1, cpu.Step());

        Assert.Equal(0xaa, cpu.SFRs.Acc);
        Assert.Equal(0x04, cpu.Memory.Read(3));
        Assert.Equal(0x55, cpu.SFRs.Trl);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x55, cpu.SFRs.Acc);
        Assert.Equal(0xaa, cpu.SFRs.Trl);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0xaa, cpu.SFRs.Acc);
        Assert.Equal(0x55, cpu.SFRs.Trl);

        Assert.Equal(1, cpu.Step());
        Assert.Equal(0x55, cpu.SFRs.Acc);
        Assert.Equal(0xaa, cpu.SFRs.Trl);
    }
}