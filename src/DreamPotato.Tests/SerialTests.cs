using DreamPotato.Core;
using DreamPotato.Core.SFRs;

namespace DreamPotato.Tests;

public class SerialTests
{
    [Fact]
    public void Main()
    {
        // not1  ext, $00
        // jmpf  $0139
        ReadOnlySpan<byte> stubBiosTickHandler = [
            (OpcodeMask.NOT1 | 0b1_0000 /*d8*/ | 0b000 /*b2-0*/), SpecialFunctionRegisterIds.Ext,
            OpcodeMask.JMPF, 0x01, 0x39,
        ];

        var cpuTx = new Cpu() { DisplayName = "SioTx", DreamcastSlot = DreamcastSlot.Slot1 };
        cpuTx.SFRs.Btcr = new Btcr { CountEnable = true, Int0Enable = true };
        stubBiosTickHandler.CopyTo(cpuTx.ROM.AsSpan(0x130));
        cpuTx.Reset();

        File.ReadAllBytes("TestSource/SioTx.vms").CopyTo(cpuTx.FlashBank0);
        cpuTx.SetInstructionBank(InstructionBank.FlashBank0);

        var cpuRx = new Cpu() { DisplayName = "SioRx", DreamcastSlot = DreamcastSlot.Slot2 };
        cpuRx.SFRs.Btcr = new Btcr { CountEnable = true, Int0Enable = true };
        stubBiosTickHandler.CopyTo(cpuRx.ROM.AsSpan(0x130));
        cpuRx.Reset();

        File.ReadAllBytes("TestSource/SioRx.vms").CopyTo(cpuRx.FlashBank0);
        cpuRx.SetInstructionBank(InstructionBank.FlashBank0);
        cpuRx.ConnectVmu(cpuTx);

        var halfSecond = TimeSpan.TicksPerSecond / 2;
        cpuRx.Run(ticksToRun: halfSecond);
        Assert.Equal<object>("""
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            

            """, cpuRx.Display.GetBlockString());

        cpuRx.Run(ticksToRun: halfSecond);
        Assert.Equal<object>("""
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            

            """, cpuRx.Display.GetBlockString());

        cpuRx.Run(ticksToRun: halfSecond);
        Assert.Equal<object>("""
              ▄██                                           
               ██                                           
               ██                                           
              ▀▀▀▀                                          
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            
                                                            

            """, cpuRx.Display.GetBlockString());

        // TODO: consecutive digits work on the real hardware setup but not here.
        // More work needed to figure out what is missing there.
    }
}