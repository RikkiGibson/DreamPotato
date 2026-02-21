using System.Buffers.Binary;
using System.Diagnostics;

using DreamPotato.Core.SFRs;

namespace DreamPotato.Core;

public class Audio
{
    // This won't work well with RC oscillator sounds.
    // Could consider using a separate sample rate of like 43600 there (872000 / 43600 = 20).
    // But that would really just be a way to avoid doing our own resampling
    public const int SampleRate = OscillatorHz.Quartz;
    public const int SampleSize = 2; // 16-bit
    public const int BufferDurationMilliseconds = 100;

    public const int DefaultVolume = 50;

    public const int MinVolume = 0;
    public const int MaxVolume = 100;

    // TODO: should probably drop dependency on this.
    private readonly Cpu _cpu;
    private readonly Logger _logger;

    private const int PcmBufferFilledSize = SampleRate * SampleSize * BufferDurationMilliseconds / 1000;
    /// <summary>
    /// PCM data at <see cref="SampleRate"/> and <see cref="SampleSize"/>.
    /// </summary>
    private readonly byte[] _pcmBuffer = new byte[2 * PcmBufferFilledSize];

    // TODO: include in save states?
    /// <summary>
    /// Pulse generator compare value.
    /// When the timer value is smaller than this, a low signal is generated, otherwise a high signal is generated.
    /// </summary>
    private byte _compare;

    internal Audio(Cpu cpu, Logger logger)
    {
        _cpu = cpu;
        _logger = logger;
        Volume = DefaultVolume;
    }

    internal void OnT1LRunChanged(bool t1lRun, byte t1lr, byte t1lc)
    {
        _compare = t1lc;
        IsActive = CalcIsActive(t1lRun, t1lr, t1lc);
    }

    internal void OnT1LReloaded(T1Cnt t1cnt, byte t1lr, byte t1lc)
    {
        if (t1cnt.ELDT1C)
            _compare = t1lc;

        IsActive = CalcIsActive(t1cnt.T1lRun, t1lr, t1lc);
    }

    /// <summary>
    /// 'true' if the emulation state is currently playing sound; otherwise, 'false'.
    /// </summary>
    public bool IsActive
    {
        get;
        internal set
        {
            var ended = field && !value;
            field = value;
            if (ended)
                EndAudio();
        }
    }

    private bool CalcIsActive(bool t1lRun, byte t1lr, byte t1lc)
    {
        if (Volume == 0)
            return false;

        if (!t1lRun)
            return false;

        // Audio signal goes from low to high according to the following pattern:
        // T1Lr       T1Lc       0xff
        // |__________|‾‾‾‾‾‾‾‾‾‾|
        // T1L starts at T1Lr, and signal is low,
        // until it reaches T1Lc where it is high until we reload again.
        // For example, the highest pitch the timer can produce, is
        // with T1Lr=254, T1Lc=255, which alternates low and high every cycle.
        // If T1Lc is not greater than T1Lr, there is no point where the signal is low, and thus no sound.
        return t1lc > t1lr;
    }

    public record struct AudioBufferReadyEventArgs(byte[] Buffer, int Start, int Length);
    public event Action<AudioBufferReadyEventArgs>? AudioBufferReady;

    public short GetSampleVolume()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(Volume, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Volume, 100);

        // Audio perception is logarithmic. Approximate this by squaring the volume setting value.
        // Take the fraction of the total possible squared volume and multiply by the maximum sample amplitude.
        var percentage = Math.Pow(Volume, 2) / Math.Pow(MaxVolume, 2);
        Debug.Assert(percentage is >= 0 and <= 1);
        return (short)(percentage * short.MaxValue);
    }

    /// <summary>
    /// Sets the volume of audio output (between <see cref="MinVolume"/> and <see cref="MaxVolume"/>).
    /// </summary>
    public int Volume
    {
        get;
        set
        {
            field = value;
            var sampleVolume = GetSampleVolume();
            _logger.LogDebug($"New Volume: {value}, SampleVolume: {sampleVolume}", LogCategories.Audio);
            ArgumentOutOfRangeException.ThrowIfNegative(sampleVolume);
            BinaryPrimitives.WriteInt16LittleEndian(_highSignal, sampleVolume);
            BinaryPrimitives.WriteInt16LittleEndian(_lowSignal, (short)-sampleVolume);
        }
    }

    private readonly byte[] _highSignal = new byte[2];
    private readonly byte[] _lowSignal = new byte[2];

    /// <summary>How many samples we have written into the pcm buffer so far.</summary>
    private int _pcmBufferIndex;

    /// <summary>When CPU speed is not evenly divisible by sample rate, tracks how far we were into a single sample.</summary>
    private int _pcmRemainder;

    /// <summary>
    /// Fills <paramref name="buffer"/> with PCM data based on the current audio state.
    /// </summary>
    /// <returns>End index of the PCM data in <paramref name="buffer"/>.</returns>
    /// <remarks>This is currently only used for testing</remarks>
    public int Generate(Span<byte> buffer)
    {
        if (!IsActive)
            return -1;

        _logger.LogDebug($"Generating audio buffer of size {buffer.Length}", LogCategories.Audio);

        var cpuClockHz = _cpu.SFRs.Ocr.CpuClockHz;
        // TODO: this is likely wrong now that audio internally stores a compare value
        var t1lc = _cpu.SFRs.T1Lc;
        var t1lr = _cpu.SFRs.T1Lr;

        // Duty cycle:
        // while t1lc < t1l, signal is low.
        // while t1lc >= t1l, signal is high.

        // Typical setup: (R=Reload, C=Compare, M=Max)
        // R----C----M

        var timerTicksPerPeriod = 0xff - t1lr + 1;
        var timerTicksAtLowSignal = t1lc - t1lr;
        if (timerTicksPerPeriod < 2 || timerTicksAtLowSignal <= 0)
        {
            _logger.LogWarning($"Could not play sound with T1lc={t1lc:X} T1lr={t1lr:X}");
            return -1;
        }
        Debug.Assert(timerTicksAtLowSignal < timerTicksPerPeriod);

        var samplesPerTimerPeriod = timerTicksPerPeriod * SampleRate / cpuClockHz;
        var samplesAtLowSignal = timerTicksAtLowSignal * SampleRate / cpuClockHz;

        var sampleVolume = GetSampleVolume();
        BinaryPrimitives.WriteInt16LittleEndian(_highSignal, sampleVolume);
        BinaryPrimitives.WriteInt16LittleEndian(_lowSignal, (short)-sampleVolume);

        int bufferIndex;
        for (bufferIndex = 0; bufferIndex <= buffer.Length - samplesPerTimerPeriod * 2;)
        {
            for (int i = 0; i < samplesAtLowSignal; i++)
            {
                buffer[bufferIndex++] = _lowSignal[0];
                buffer[bufferIndex++] = _lowSignal[1];
            }

            for (int i = samplesAtLowSignal; i < samplesPerTimerPeriod; i++)
            {
                buffer[bufferIndex++] = _highSignal[0];
                buffer[bufferIndex++] = _highSignal[1];
            }
        }

        return bufferIndex;
    }

    /// <summary>
    /// Appends a pulse <see cref="value"/> to the PCM buffer for 1 cycle at <see cref="cpuClockHz"/>
    /// Returns the pulse value that was appended (low or high)
    /// </summary>
    internal bool AddPulse(int cpuClockHz, byte t1l)
    {
        Debug.Assert(IsActive);

        if (cpuClockHz is not (OscillatorHz.Quartz / 6 or OscillatorHz.Quartz / 12))
        {
            _logger.LogWarning(
                $"Sample rate not compatible with clock {_cpu.SFRs.Ocr.SystemClockSelector}.",
                LogCategories.Audio);
        }

        var sampleRateAndRemainder = SampleRate + _pcmRemainder;
        var samplesPerCycle = sampleRateAndRemainder / cpuClockHz;
        _pcmRemainder = sampleRateAndRemainder % cpuClockHz;

        var pulseValue = t1l >= _compare;
        var signal = pulseValue ? _highSignal : _lowSignal;
        for (int i = 0; i < samplesPerCycle; i++)
        {
            _pcmBuffer[_pcmBufferIndex++] = signal[0];
            _pcmBuffer[_pcmBufferIndex++] = signal[1];
        }

        if (_pcmBufferIndex >= PcmBufferFilledSize)
        {
            _logger.LogDebug($"Submitting audio buffer of length {_pcmBufferIndex}", LogCategories.Audio);
            AudioBufferReady?.Invoke(new(_pcmBuffer, Start: 0, Length: _pcmBufferIndex));
            _pcmBufferIndex = 0;
            _pcmRemainder = 0;
        }

        return pulseValue;
    }

    private void EndAudio()
    {
        if (_pcmBufferIndex == 0)
            return;

        _logger.LogDebug($"EndAudio: Submitting audio buffer of length {_pcmBufferIndex}", LogCategories.Audio);
        if (_cpu.SFRs.Ocr.CpuClockHz is not ((OscillatorHz.Quartz / 6) or (OscillatorHz.Quartz / 12)))
        {
            _logger.LogWarning(
                $"Sample rate not compatible with clock {_cpu.SFRs.Ocr.SystemClockSelector}.",
                LogCategories.Audio);
        }

        AudioBufferReady?.Invoke(new(_pcmBuffer, 0, _pcmBufferIndex));
        _pcmBufferIndex = 0;
    }
}