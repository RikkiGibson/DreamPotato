
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
        cpu.SFRs.T1Cnt = cpu.SFRs.T1Cnt with { T1lRun = false };
        Assert.NotNull(data);

        int widthSamples = width * Audio.SampleRate / cpuClockHz;
        Assert.Equal<object>("""
               | 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 
            00 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            01 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            02 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            03 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 FF 1F FF 1F 
            04 | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            05 | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            06 | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            07 | FF 1F FF 1F FF 1F FF 1F 01 E0 01 E0 01 E0 01 E0 
            08 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            09 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0A | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0B | 01 E0 01 E0 FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            0C | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            0D | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            0E | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            0F | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            10 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            11 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            12 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 FF 1F FF 1F 
            """, new ReadOnlySpan<byte>(data, 0, 0x130).AsHexBlock());
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

        cpu.SFRs.T1Cnt = cpu.SFRs.T1Cnt with { T1lRun = false };

        Assert.Equal<object>("""
               | 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 
            00 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 FF 1F FF 1F 
            01 | FF 1F FF 1F FF 1F FF 1F 01 E0 01 E0 01 E0 01 E0 
            02 | 01 E0 01 E0 FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            03 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 FF 1F FF 1F 
            04 | FF 1F FF 1F FF 1F FF 1F 01 E0 01 E0 01 E0 01 E0 
            05 | 01 E0 01 E0 FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            """, new ReadOnlySpan<byte>(data, 0, length: 0x60).AsHexBlock());
    }

    // TODO: Test program which generates pcm audio a la SoulCalibur 3-in-1
}