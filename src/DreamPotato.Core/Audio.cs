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
    public const int BufferDurationMilliseconds = 8;
    private const int MaxTotalBufferDurationMilliseconds = 96;
    public const int MaxQueuedBufferCount = MaxTotalBufferDurationMilliseconds / BufferDurationMilliseconds;

    public const int DefaultVolume = 50;

    public const int MinVolume = 0;
    public const int MaxVolume = 100;

    private readonly Cpu _cpu;
    private Logger _logger => _cpu.Logger;

    private const int PcmBufferFilledSize = SampleRate * BufferDurationMilliseconds / 1000 * SampleSize;
    /// <summary>
    /// PCM data at <see cref="SampleRate"/> and <see cref="SampleSize"/>.
    /// </summary>
    private readonly byte[] _pcmBuffer = new byte[PcmBufferFilledSize];
    private readonly byte[] _emptyPcmBuffer = new byte[PcmBufferFilledSize];

    /// <summary>
    /// Pulse generator compare value.
    /// When the timer value is smaller than this, a low signal is generated, otherwise a high signal is generated.
    /// </summary>
    private byte _compare;

    internal Audio(Cpu cpu)
    {
        _cpu = cpu;
        Volume = DefaultVolume;
    }

    internal void OnT1LRunChanged(byte t1lc)
    {
        _compare = t1lc;
    }

    internal void OnT1LReloaded(T1Cnt t1cnt, byte t1lc)
    {
        if (t1cnt.ELDT1C)
            _compare = t1lc;
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
    /// Appends a pulse <see cref="value"/> to the PCM buffer for 1 cycle at <see cref="cpuClockHz"/>
    /// Returns the pulse value that was appended (low or high)
    /// </summary>
    internal bool AddPulse(int cpuClockHz, byte t1l)
    {
        var sampleRateAndRemainder = SampleRate + _pcmRemainder;
        var samplesPerCycle = sampleRateAndRemainder / cpuClockHz;
        _pcmRemainder = sampleRateAndRemainder % cpuClockHz;

        var pulseValue = t1l >= _compare;
        var signal = pulseValue ? _highSignal : _lowSignal;
        for (int i = 0; i < samplesPerCycle; i++)
        {
            _pcmBuffer[_pcmBufferIndex++] = signal[0];
            _pcmBuffer[_pcmBufferIndex++] = signal[1];

            if (_pcmBufferIndex == _pcmBuffer.Length)
            {
                if (!hasRealSignal())
                {
                    Debug.Assert(_emptyPcmBuffer.All(b => b == 0));
                    AudioBufferReady?.Invoke(new(_emptyPcmBuffer, Start: 0, Length: _pcmBufferIndex));
                }
                else
                {
                    AudioBufferReady?.Invoke(new(_pcmBuffer, Start: 0, Length: _pcmBufferIndex));
                }

                _pcmBufferIndex = 0;
                _pcmRemainder = 0;
            }
        }

        // TODO2: This seems to cut off some signals in practice
        bool hasRealSignal()
        {
            for (var j = 0; j < _pcmBuffer.Length; j += 2)
            {
                if (_pcmBuffer[j] != signal[0] || _pcmBuffer[j + 1] != signal[1])
                    return true;
            }

            return false;
        }

        return pulseValue;
    }
}