
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

        while (data is null)
            cpu.Step();

        // Audio is double buffered, so the first buffer we receive is empty
        for (int i = 0; i < data.Length; i++)
            Assert.Equal(0, data[i]);

        data = null;
        while (data is null)
            cpu.Step();

        Assert.Equal<object>("""
               | 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 
            00 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            01 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            02 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            03 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            04 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            05 | DB 07 FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            06 | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            07 | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            08 | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            09 | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            0A | 48 10 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0B | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0C | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0D | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0E | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0F | 01 E0 92 17 FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            10 | FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
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

        while (data is null)
            cpu.Step();

        // Audio is double buffered, so the first buffer we receive is empty
        for (int i = 0; i < data.Length; i++)
            Assert.Equal(0, data[i]);

        data = null;
        while (data is null)
            cpu.Step();

        Assert.Equal<object>("""
               | 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 
            00 | 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            01 | 2B 1B FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            02 | A9 E9 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            03 | 83 11 FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            04 | 51 F3 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            05 | DB 07 FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            06 | F9 FC 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            07 | 34 FE FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            08 | A0 06 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            09 | 8C F4 FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            0A | 48 10 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0B | E4 EA FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            0C | F0 19 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0D | 3C E1 FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            0E | FF 1F 9A E3 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            0F | 01 E0 92 17 FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            10 | FF 1F 42 ED 01 E0 01 E0 01 E0 01 E0 01 E0 01 E0 
            11 | 01 E0 EA 0D FF 1F FF 1F FF 1F FF 1F FF 1F FF 1F 
            """, new ReadOnlySpan<byte>(data, 0, length: 0x120).AsHexBlock());
    }

    // TODO: Test program which generates pcm audio a la SoulCalibur 3-in-1
    // TODO: Test pop filtering
}