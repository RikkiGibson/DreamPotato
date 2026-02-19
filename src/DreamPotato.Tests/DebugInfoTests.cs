using DreamPotato.Core;
using DreamPotato.Core.SFRs;

namespace DreamPotato.Tests;

public class DebugInfoTests
{
    [Fact]
    public void DebugInfo_Overlap_01()
    {
        // Before: |NOP|NOP|NOP|NOP|NOP|
        // After:  |NOP| JMPF 1234 |NOP|
        // offset  | 0 | 1 | 2 | 3 | 4 |
        var cpu = new Cpu();
        var bankInfo = new BankDebugInfo(cpu, InstructionBank.ROM);

        bankInfo.SetInstruction(new(new Instruction(Offset: 0, Operations.NOP), executed: false));
        bankInfo.SetInstruction(new(new Instruction(Offset: 1, Operations.NOP), executed: false));
        bankInfo.SetInstruction(new(new Instruction(Offset: 2, Operations.NOP), executed: false));
        bankInfo.SetInstruction(new(new Instruction(Offset: 3, Operations.NOP), executed: false));
        bankInfo.SetInstruction(new(new Instruction(Offset: 4, Operations.NOP), executed: false));

        bankInfo.SetInstruction(new(new Instruction(Offset: 1, Operations.JMPF_a16, Arg0: 0xFFFF), executed: false));

        var instrs = bankInfo.Instructions;
        Assert.Equal([
            new(new Instruction(Offset: 0, Operations.NOP), executed: false),
            new(new Instruction(Offset: 1, Operations.JMPF_a16, Arg0: 0xFFFF), executed: false),
            new(new Instruction(Offset: 4, Operations.NOP), executed: false),
        ], instrs);
    }
    
    [Fact]
    public void DebugInfo_Overlap_02()
    {
        // Before: | LD 42 |NOP| ST 42 |
        // After:  |   | JMPF 1234 |   |
        // offset  | 0 | 1 | 2 | 3 | 4 |
        var cpu = new Cpu();
        var bankInfo = new BankDebugInfo(cpu, InstructionBank.ROM);

        bankInfo.SetInstruction(new(new Instruction(Offset: 0, Operations.LD_d9, Arg0: 0x42), executed: false));
        bankInfo.SetInstruction(new(new Instruction(Offset: 2, Operations.NOP), executed: false));
        bankInfo.SetInstruction(new(new Instruction(Offset: 3, Operations.ST_d9, Arg0: 0x42), executed: false));

        bankInfo.SetInstruction(new(new Instruction(Offset: 1, Operations.JMPF_a16, Arg0: 0xFFFF), executed: false));

        var instrs = bankInfo.Instructions;
        Assert.Equal([
            new(new Instruction(Offset: 1, Operations.JMPF_a16, Arg0: 0xFFFF), executed: false)
        ], instrs);
    }

    // TODO2: Clear(Offset) when exact match
    // TODO2: Clear(Offset) when "in middle" of an instr
}