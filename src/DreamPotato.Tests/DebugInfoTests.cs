using DreamPotato.Core;
using DreamPotato.Core.SFRs;

namespace DreamPotato.Tests;

public class DebugInfoTests
{
    [Fact]
    public void DebugInfo_Overlap_01()
    {
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
}