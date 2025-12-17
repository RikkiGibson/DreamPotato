using DreamPotato.Core;
using DreamPotato.Core.SFRs;

namespace DreamPotato.Tests;

public class SerialTests
{
    /// <summary>Run the Serial Communications Sample from VMT-72.</summary>
    [Fact]
    public void SerialCommunicationsSample()
    {
        // not1  ext, $00
        // jmpf  $0139
        ReadOnlySpan<byte> stubBiosTickHandler = [
            (OpcodeMask.NOT1 | 0b1_0000 /*d8*/ | 0b000 /*b2-0*/), SpecialFunctionRegisterIds.Ext,
            OpcodeMask.JMPF, 0x01, 0x39,
        ];

        var cpuTx = new Cpu() { DisplayName = "SioTx", DreamcastSlot = DreamcastSlot.Slot1 };
        stubBiosTickHandler.CopyTo(cpuTx.ROM.AsSpan(0x130));
        cpuTx.Reset();
        cpuTx.SFRs.Btcr = new Btcr { CountEnable = true, Int0Enable = true };
        cpuTx.SFRs.Psw = new Psw { Rambk0 = true };

        File.ReadAllBytes("TestSource/SioTx.vms").CopyTo(cpuTx.FlashBank0);
        cpuTx.SetInstructionBank(InstructionBank.FlashBank0);

        var cpuRx = new Cpu() { DisplayName = "SioRx", DreamcastSlot = DreamcastSlot.Slot2 };
        stubBiosTickHandler.CopyTo(cpuRx.ROM.AsSpan(0x130));
        cpuRx.Reset();
        cpuRx.SFRs.Btcr = new Btcr { CountEnable = true, Int0Enable = true };
        cpuRx.SFRs.Psw = new Psw { Rambk0 = true };

        File.ReadAllBytes("TestSource/SioRx.vms").CopyTo(cpuRx.FlashBank0);
        cpuRx.SetInstructionBank(InstructionBank.FlashBank0);
        cpuRx.ConnectVmu(cpuTx);

        const long halfSecond = TimeSpan.TicksPerSecond / 2;
        cpuRx.Run(ticksToRun: halfSecond);
        Assert.Equal<object>("", cpuRx.Display.ToTestDisplayString());

        cpuRx.Run(ticksToRun: halfSecond);
        Assert.Equal<object>("""
            |
            |
            |
            |
            |                ▄██▀▀█▄ ▄██▀▀█▄
            |                ██   ██ ██   ██
            |                ██  ▄██ ██  ▄██
            |                 ▀▀▀▀▀   ▀▀▀▀▀
            """, cpuRx.Display.ToTestDisplayString());
        Assert.Equal(0, cpuRx.SFRs.Sbuf1);

        cpuRx.Run(ticksToRun: halfSecond);
        Assert.Equal<object>("""
            |
            |
            |
            |
            |                ▄██▀▀█▄   ▄██
            |                ██   ██    ██
            |                ██  ▄██    ██
            |                 ▀▀▀▀▀    ▀▀▀▀
            """, cpuRx.Display.ToTestDisplayString());
        Assert.Equal(1, cpuRx.SFRs.Sbuf1);

        cpuRx.Run(ticksToRun: halfSecond);
        Assert.Equal<object>("""
            |
            |
            |
            |
            |                ▄██▀▀█▄ ▄█▀▀▀█▄
            |                ██   ██ ▀▀  ▄█▀
            |                ██  ▄██  ▄█▀▀
            |                 ▀▀▀▀▀  ▀▀▀▀▀▀▀
            """, cpuRx.Display.ToTestDisplayString());
        Assert.Equal(2, cpuRx.SFRs.Sbuf1);

        cpuRx.Run(ticksToRun: halfSecond);
        Assert.Equal<object>("""
            |
            |
            |
            |
            |                ▄██▀▀█▄ ▄██▀▀█▄
            |                ██   ██    ▄▄█▀
            |                ██  ▄██ ▄▄▄  ██
            |                 ▀▀▀▀▀   ▀▀▀▀▀
            """, cpuRx.Display.ToTestDisplayString());
        Assert.Equal(3, cpuRx.SFRs.Sbuf1);
    }

    [Fact]
    public void FileTransfer()
    {
        if (!File.Exists("Data/american_v1.05.bin"))
            Assert.Skip("Test requires a BIOS");

        var vmuTx = new Vmu();
        var cpuTx = vmuTx._cpu;
        cpuTx.DisplayName = "Sender";
        cpuTx.DreamcastSlot = DreamcastSlot.Slot1;
        vmuTx.LoadRom();
        vmuTx.LoadGameVms("TestSource/helloworld.vms", date: DateTimeOffset.Parse("09/09/1999"));

        const long halfSecond = TimeSpan.TicksPerSecond / 2;
        cpuTx.Run(halfSecond);
        string logo = """
            |            ▄████ ▄████ ▄████  ▄█▄
            |            ▀█▄▄  ██▄▄  ██▄▄▄ ██▀██
            |            ▄▄▄██ ██▄▄▄ ██▄██ ██▄██
            |            ▀▀▀▀   ▀▀▀▀  ▀▀▀▀ ▀▀ ▀▀
            |      █   █   ▀                      ▀█
            |      █   █  ▀█   ▄▀▀▀  █   █  ▀▀▀▄   █
            |      ▀▄ ▄▀   █    ▀▀▀▄ █  ▄█ ▄▀▀▀█   █
            |        ▀    ▀▀▀  ▀▀▀▀   ▀▀ ▀  ▀▀▀▀  ▀▀▀
            |      █▄ ▄█
            |      █ █ █ ▄▀▀▀▄ █▀▄▀▄ ▄▀▀▀▄ █▄▀▀▄ █   █
            |      █   █ █▀▀▀▀ █ ▀ █ █   █ █      ▀▀▀█
            |      ▀   ▀  ▀▀▀  ▀   ▀  ▀▀▀  ▀      ▀▀▀
            |            █   █         ▀    █
            |            █   █ █▄▀▀▄  ▀█   ▀█▀
            |            █   █ █   █   █    █  ▄
            |             ▀▀▀  ▀   ▀  ▀▀▀    ▀▀
            """;
        Assert.Equal<object>(logo, vmuTx.Display.ToTestDisplayString());

        pressButtonAndWait(cpuTx, new P3(0xff) { ButtonA = false });
        Assert.Equal<object>("""
            |█▀▀▀▀                          ▄█   ▄▀▀▀▄  ▄▀▀
            |█▄▄▄  █▄▀▀▄ ▄▀▀▀▄ ▄▀▀▀▄         █   ▀▄▄▄█ █▄▄▄
            |█     █     █▀▀▀▀ █▀▀▀▀         █      ▄▀ █   █
            |▀     ▀      ▀▀▀   ▀▀▀         ▀▀▀   ▀▀    ▀▀▀
            |▄▀▀▀▄                         ▄▀▀▀▄ ▄▀▀▀▄ ▄▀▀▀▄
            |█ ▄▄▄  ▀▀▀▄ █▀▄▀▄ ▄▀▀▀▄       █ ▄▀█ █ ▄▀█ █ ▄▀█
            |█   █ ▄▀▀▀█ █ ▀ █ █▀▀▀▀       █▀  █ █▀  █ █▀  █
            | ▀▀▀▀  ▀▀▀▀ ▀   ▀  ▀▀▀         ▀▀▀   ▀▀▀   ▀▀▀
            """, vmuTx.Display.ToTestDisplayString());

        // Press Right 3 times
        for (int i = 0; i < 3; i++)
            pressButtonAndWait(cpuTx, new P3(0xff) { Right = false });

        Assert.Equal<object>("""
            |█▀▀▀██▀▄▄▄▀█████████████████████████████████████
            |   ▄ █ █████▀▄▄▄▀█ ▄▄▄▀█ ███ ███████████████████
            |▄ ▀ ▄█ ███▀█ ███ █ ▄▄▄███▄▄▄ ███████████████████
            |███████▄▄▄███▄▄▄██▄██████▄▄▄████████████████████
            | ▄▄▄  █▀▀▄         ▀█          █
            |███▀█ █   █ ▄▀▀▀▄   █   ▄▀▀▀▄ ▀█▀   ▄▀▀▀▄
            |▀█▄█▀ █  ▄▀ █▀▀▀▀   █   █▀▀▀▀  █  ▄ █▀▀▀▀
            |      ▀▀▀    ▀▀▀   ▀▀▀   ▀▀▀    ▀▀   ▀▀▀
            | ▄█▄   ▄▄   █   █
            |██▀██  ▀▀   ▀▄ ▄▀ ▄▀▀▀▄ ▄▀▀▀
            |██▄██  ██     █   █▀▀▀▀  ▀▀▀▄
            |▀▀ ▀▀         ▀    ▀▀▀  ▀▀▀▀
            |██▀█▄  ▄▄   █   █
            |██▄█▀  ▀▀   █▀▄ █ ▄▀▀▀▄
            |██ ██  ██   █  ▀█ █   █
            |▀▀▀▀▀       ▀   ▀  ▀▀▀
            """, vmuTx.Display.ToTestDisplayString());

        pressButtonAndWait(cpuTx, new P3(0xff) { ButtonA = false });
        pressButtonAndWait(cpuTx, new P3(0xff) { Left = false });
        pressButtonAndWait(cpuTx, new P3(0xff) { ButtonA = false });

        Assert.Equal<object>("""
            |█            ▀█    ▀█
            |█▄▀▀▄ ▄▀▀▀▄   █     █   ▄▀▀▀▄ █   █ ▄▀▀▀▄ █▄▀▀▄
            |█   █ █▀▀▀▀   █     █   █   █ █ █ █ █   █ █
            |▀   ▀  ▀▀▀   ▀▀▀   ▀▀▀   ▀▀▀   ▀ ▀   ▀▀▀  ▀
            | ▀█       █
            |  █   ▄▀▀▄█
            |  █   █   █
            | ▀▀▀   ▀▀▀▀   ▀     ▀
            |▄▀▀▀▄                                █
            |█     ▄▀▀▀▄ █▄▀▀▄ █▄▀▀▄ ▄▀▀▀▄ ▄▀▀▀  ▀█▀
            |█   ▄ █   █ █   █ █   █ █▀▀▀▀ █   ▄  █  ▄
            | ▀▀▀   ▀▀▀  ▀   ▀ ▀   ▀  ▀▀▀   ▀▀▀    ▀▀
            |█   █ █▄ ▄█                               █████
            |█   █ █ █ █                               █   █
            |▀▄ ▄▀ █   █                               ▀███▀
            |  ▀   ▀   ▀                               ▀▀▀▀▀
            """, vmuTx.Display.ToTestDisplayString());

        var vmuRx = new Vmu();
        var cpuRx = vmuRx._cpu;
        cpuRx.DisplayName = "Receiver";
        cpuRx.DreamcastSlot = DreamcastSlot.Slot2;
        vmuRx.LoadRom();
        vmuRx.LoadNewVmu(DateTimeOffset.Parse("09/09/1999"), autoInitializeRTCDate: true);

        cpuRx.Run(halfSecond);
        Assert.Equal<object>(logo, vmuRx.Display.ToTestDisplayString());

        pressButtonAndWait(cpuRx, new P3(0xff) { ButtonA = false });
        pressButtonAndWait(cpuRx, new P3(0xff) { Right = false });
        Assert.Equal<object>("""
            |█   █                                         █
            |█▀▄ █ ▄▀▀▀▄       ▄▀▀▀   ▀▀▀▄ █   █ ▄▀▀▀▄ ▄▀▀▄█
            |█  ▀█ █   █        ▀▀▀▄ ▄▀▀▀█ ▀▄ ▄▀ █▀▀▀▀ █   █
            |▀   ▀  ▀▀▀        ▀▀▀▀   ▀▀▀▀   ▀    ▀▀▀   ▀▀▀▀
            |                   ▄▀▀▄   ▀    ▀█
            |                  ▄█▄    ▀█     █   ▄▀▀▀▄ ▄▀▀▀
            |                   █      █     █   █▀▀▀▀  ▀▀▀▄
            |                   ▀     ▀▀▀   ▀▀▀   ▀▀▀  ▀▀▀▀
            """, vmuRx.Display.ToTestDisplayString());

        // Connect VMUs
        vmuRx.ConnectOrDisconnectVmu(vmuTx);
        cpuRx.Run(TimeSpan.TicksPerSecond * 2);

        Assert.Equal<object>("""
            |█            ▀█    ▀█
            |█▄▀▀▄ ▄▀▀▀▄   █     █   ▄▀▀▀▄ █   █ ▄▀▀▀▄ █▄▀▀▄
            |█   █ █▀▀▀▀   █     █   █   █ █ █ █ █   █ █
            |▀   ▀  ▀▀▀   ▀▀▀   ▀▀▀   ▀▀▀   ▀ ▀   ▀▀▀  ▀
            | ▀█       █
            |  █   ▄▀▀▄█
            |  █   █   █
            | ▀▀▀   ▀▀▀▀   ▀     ▀
            |▄▀▀▀▄                         ▄▀▀▀▄
            |█     ▄▀▀▀▄ █▀▀▀▄ █   █          ▄▀
            |█   ▄ █   █ █▀▀▀   ▀▀▀█         ▀
            | ▀▀▀   ▀▀▀  ▀      ▀▀▀          ▀
            |█   █                   ██████ ███ █████████████
            |▀▄ ▄▀ ▄▀▀▀▄ ▄▀▀▀        ██████ ▄▀█ █▀▄▄▄▀███████
            |  █   █▀▀▀▀  ▀▀▀▄       ██████ ██▄ █ ███ ███████
            |  ▀    ▀▀▀  ▀▀▀▀        ██████▄███▄██▄▄▄████████
            """, vmuTx.Display.ToTestDisplayString());

        Assert.Equal<object>("""
            |█   █         ▀    █      ▀          ▄▄▄▄
            |█ ▄ █  ▀▀▀▄  ▀█   ▀█▀    ▀█   █▄▀▀▄ █   █
            |█ █ █ ▄▀▀▀█   █    █  ▄   █   █   █  ▀▀▀█
            | ▀ ▀   ▀▀▀▀  ▀▀▀    ▀▀   ▀▀▀  ▀   ▀  ▀▀▀
            | ▄▀▀▄                       █        █
            |▄█▄   ▄▀▀▀▄ █▄▀▀▄       ▄▀▀▄█  ▀▀▀▄ ▀█▀    ▀▀▀▄
            | █    █   █ █           █   █ ▄▀▀▀█  █  ▄ ▄▀▀▀█
            | ▀     ▀▀▀  ▀            ▀▀▀▀  ▀▀▀▀   ▀▀   ▀▀▀▀
            """, vmuRx.Display.ToTestDisplayString());

        // Moment of truth: actually copy the file.
        //
        pressButtonAndWait(cpuTx, new P3(0xff) { Left = false });
        pressButtonAndWait(cpuTx, new P3(0xff) { ButtonA = false });

        var copyingText = """
            |█            ▀█    ▀█
            |█▄▀▀▄ ▄▀▀▀▄   █     █   ▄▀▀▀▄ █   █ ▄▀▀▀▄ █▄▀▀▄
            |█   █ █▀▀▀▀   █     █   █   █ █ █ █ █   █ █
            |▀   ▀  ▀▀▀   ▀▀▀   ▀▀▀   ▀▀▀   ▀ ▀   ▀▀▀  ▀
            | ▀█       █
            |  █   ▄▀▀▄█
            |  █   █   █
            | ▀▀▀   ▀▀▀▀   ▀     ▀
            |▄▀▀▀▄                     ▀          ▄▄▄▄   █
            |█     ▄▀▀▀▄ █▀▀▀▄ █   █  ▀█   █▄▀▀▄ █   █   █
            |█   ▄ █   █ █▀▀▀   ▀▀▀█   █   █   █  ▀▀▀█
            | ▀▀▀   ▀▀▀  ▀      ▀▀▀   ▀▀▀  ▀   ▀  ▀▀▀    ▀
            """;

        while (vmuTx.Display.ToTestDisplayString() == copyingText)
            cpuRx.Run(halfSecond);

        Assert.Equal<object>("""
            |█            ▀█    ▀█
            |█▄▀▀▄ ▄▀▀▀▄   █     █   ▄▀▀▀▄ █   █ ▄▀▀▀▄ █▄▀▀▄
            |█   █ █▀▀▀▀   █     █   █   █ █ █ █ █   █ █
            |▀   ▀  ▀▀▀   ▀▀▀   ▀▀▀   ▀▀▀   ▀ ▀   ▀▀▀  ▀
            | ▀█       █
            |  █   ▄▀▀▄█
            |  █   █   █
            | ▀▀▀   ▀▀▀▀   ▀     ▀
            |▄▀▀▀▄               ▀             █
            |█     ▄▀▀▀▄ █▀▀▀▄  ▀█   ▄▀▀▀▄ ▄▀▀▄█
            |█   ▄ █   █ █▀▀▀    █   █▀▀▀▀ █   █
            | ▀▀▀   ▀▀▀  ▀      ▀▀▀   ▀▀▀   ▀▀▀▀
            """, vmuTx.Display.ToTestDisplayString());

        cpuRx.Run(TimeSpan.TicksPerSecond);
        pressButtonAndWait(cpuRx, new P3(0xff) { ButtonMode = false });
        pressButtonAndWait(cpuRx, new P3(0xff) { ButtonA = false });

        // Run the copied game
        Assert.Equal<object>("""
            |█ █      █   █          █ █          █    █  █
            |█▀█ ▄██  █   █  ▄▀▄     ███ ▄▀▄ ▄▀   █  ▄▀█  █
            |█ █ ▀▄▄  ▀▄  ▀▄ ▀▄▀  █  █▀█ ▀▄▀ █    ▀▄ ▀▄█  ▄
            """, vmuRx.Display.ToTestDisplayString());
        Assert.Equal(Icons.Game, vmuRx.Display.GetIcons());

        static void pressButtonAndWait(Cpu cpu, P3 pressedState)
        {
            cpu.SFRs.P3 = pressedState;
            cpu.Run(halfSecond);
            cpu.SFRs.P3 = new P3(0xff);
            cpu.Run(TimeSpan.TicksPerSecond * 2);
        }
    }
}