using VEmu.Core;

using Xunit.Abstractions;

namespace VEmu.Tests;

public class WorkRamTests
{
    [Fact]
    public void StoreAndLoadSingleValue()
    {
        var cpu = new Cpu();
        cpu.WorkRam[0x11f] = 0x40;
        cpu.SFRs.Vrmad1 = 0x1f;
        cpu.SFRs.Vrmad2 = 1;

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.LD.Compose(AddressingMode.Direct1), SpecialFunctionRegisterKind.Vtrbf.Suffix(),
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), SpecialFunctionRegisterKind.Vtrbf.Suffix(), 0x01,
            OpcodePrefix.LD.Compose(AddressingMode.Direct1), SpecialFunctionRegisterKind.Vtrbf.Suffix(),
        ];

        instructions.CopyTo(cpu.CurrentROMBank);
        cpu.Step();
        Assert.Equal(0x40, cpu.SFRs.Acc);

        cpu.Step();
        cpu.Step();
        Assert.Equal(0x01, cpu.SFRs.Acc);
    }

    [Fact]
    public void StoreAndLoadSingleValue_WrongAddress()
    {
        var cpu = new Cpu();
        cpu.WorkRam[0x11f] = 0x40;
        cpu.SFRs.Vrmad1 = 0x10;
        cpu.SFRs.Vrmad2 = 1;

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.LD.Compose(AddressingMode.Direct1), SpecialFunctionRegisterKind.Vtrbf.Suffix(),
            OpcodePrefix.MOV.Compose(AddressingMode.Direct1), SpecialFunctionRegisterKind.Vtrbf.Suffix(), 0x01,
            OpcodePrefix.LD.Compose(AddressingMode.Direct1), SpecialFunctionRegisterKind.Vtrbf.Suffix(),
        ];

        instructions.CopyTo(cpu.CurrentROMBank);
        cpu.Step();
        Assert.Equal(0, cpu.SFRs.Acc);

        cpu.Step();
        cpu.Step();
        Assert.Equal(1, cpu.SFRs.Acc);
    }

    [Fact]
    public void StoreAndLoadMultipleValues()
    {
        var cpu = new Cpu();
        cpu.WorkRam[0x110] = 0x20;
        cpu.WorkRam[0x111] = 0x40;
        cpu.SFRs.Vrmad1 = 0x10;
        cpu.SFRs.Vrmad2 = 1;

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.LD.Compose(AddressingMode.Direct1), SpecialFunctionRegisterKind.Vtrbf.Suffix(),
            OpcodePrefix.INC.Compose(AddressingMode.Direct1), SpecialFunctionRegisterKind.Vrmad1.Suffix(),
            OpcodePrefix.LD.Compose(AddressingMode.Direct1), SpecialFunctionRegisterKind.Vtrbf.Suffix(),
        ];

        instructions.CopyTo(cpu.CurrentROMBank);
        cpu.Step();
        Assert.Equal(0x20, cpu.SFRs.Acc);

        cpu.Step();
        cpu.Step();
        Assert.Equal(0x40, cpu.SFRs.Acc);
    }

    [Fact]
    public void AutoIncrementRead()
    {
        var cpu = new Cpu();
        cpu.WorkRam[0x110] = 0x20;
        cpu.WorkRam[0x111] = 0x40;
        cpu.SFRs.Vrmad1 = 0x10;
        cpu.SFRs.Vrmad2 = 1;
        cpu.SFRs.Vsel4_Ince = true;

        ReadOnlySpan<byte> instructions = [
            OpcodePrefix.LD.Compose(AddressingMode.Direct1), SpecialFunctionRegisterKind.Vtrbf.Suffix(),
            OpcodePrefix.LD.Compose(AddressingMode.Direct1), SpecialFunctionRegisterKind.Vtrbf.Suffix(),
        ];

        instructions.CopyTo(cpu.CurrentROMBank);
        cpu.Step();
        Assert.Equal(0x20, cpu.SFRs.Acc);

        cpu.Step();
        Assert.Equal(0x40, cpu.SFRs.Acc);
    }
}