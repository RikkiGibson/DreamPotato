
using DreamPotato.Core;
using DreamPotato.Core.SFRs;

namespace DreamPotato.Tests;

public class AudioTests
{
    [Fact]
    public void PulseLength_01()
    {
        var vmu = new Vmu();
        byte[]? data = null;
        vmu.Audio.AudioBufferReady +=
            args => data = args.Buffer.AsSpan(args.Start, args.Length).ToArray();

        var cpu = vmu._cpu;
        cpu.Reset();
        cpu.SFRs.Ocr = cpu.SFRs.Ocr with { ClockGeneratorControl = true, SystemClockSelector = Oscillator.Quartz };
        var cpuClockHz = cpu.SFRs.Ocr.CpuClockHz;
        cpu.SFRs.T1Lr = 246;
        cpu.SFRs.T1Lc = 251;
        cpu.SFRs.T1Cnt = new T1Cnt() { ELDT1C = true, T1lRun = true };

        int width = (0xff - cpu.SFRs.T1Lr) * 2;
        while (data is null)
            cpu.Step();

        Assert.Equal<object>("""
               | 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 
            00 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            01 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            02 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            03 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            04 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            05 | 01 E0 01 E0 01 E0 55 E3 FF 1F FF 1F FF 1F FF 1F 
            06 | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            07 | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            08 | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            09 | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            0A | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 57 19 
            0B | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0C | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0D | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0E | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0F | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            10 | 01 E0 01 E0 01 E0 FD E9 FF 1F FF 1F FF 1F FF 1F 
            """, new ReadOnlySpan<byte>(data, 0, 0x110).AsHexBlock());
    }

    [Fact]
    public void PulseLength_02()
    {
        // Edge case: verify that the startup tone is accurate
        var vmu = new Vmu();
        byte[]? data = null;
        vmu.Audio.AudioBufferReady +=
            args => data = args.Buffer.AsSpan(args.Start, args.Length).ToArray();

        var cpu = vmu._cpu;
        cpu.Reset();
        cpu.SFRs.Ocr = cpu.SFRs.Ocr with { ClockGeneratorControl = true, SystemClockSelector = Oscillator.Quartz };
        var cpuClockHz = cpu.SFRs.Ocr.CpuClockHz;
        cpu.SFRs.T1Lr = 254;
        cpu.SFRs.T1Lc = 255;
        cpu.SFRs.T1Cnt = new T1Cnt() { ELDT1C = true, T1lRun = true };

        int width = (0xff - cpu.SFRs.T1Lr) * 2;
        while (data is null)
            cpu.Step();

        Assert.Equal<object>("""
               | 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 
            00 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            01 | 78 ED FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            02 | FF 1F 11 05 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            03 | 01 E0 01 E0 65 08 FF 1F FF 1F FF 1F FF 1F FF 1F 
            04 | FF 1F FF 1F FF 1F 24 EA 01 E0 01 E0 01 E0 01 E0 
            05 | 01 E0 01 E0 01 E0 55 E3 FF 1F FF 1F FF 1F FF 1F 
            06 | FF 1F FF 1F FF 1F FF 1F 34 0F 01 E0 01 E0 01 E0 
            07 | 01 E0 01 E0 01 E0 01 E0 01 E0 43 FE FF 1F FF 1F 
            08 | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 47 F4 01 E0 
            09 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 30 19 
            0A | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 57 19 
            0B | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0C | 20 F4 FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            0D | FF 1F 6A FE 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0E | 01 E0 01 E0 0D 0F FF 1F FF 1F FF 1F FF 1F FF 1F 
            0F | FF 1F FF 1F FF 1F 7C E3 01 E0 01 E0 01 E0 01 E0 
            10 | 01 E0 01 E0 01 E0 FD E9 FF 1F FF 1F FF 1F FF 1F 
            11 | FF 1F FF 1F FF 1F FF 1F 8C 08 01 E0 01 E0 01 E0 
            """, new ReadOnlySpan<byte>(data, 0, length: 0x120).AsHexBlock());
    }

    // TODO: Test program which generates pcm audio a la SoulCalibur 3-in-1
}