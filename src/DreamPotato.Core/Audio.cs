using System.Diagnostics;

using DreamPotato.Core.SFRs;

namespace DreamPotato.Core;

public class Audio
{
    // Max sample rate supported by MonoGame.
    public const int SampleRate = 48000;
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

    private static short ComputeSampleVolume(int volume)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(volume, MinVolume);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(volume, MaxVolume);

        // Audio perception is logarithmic. Approximate this by squaring the volume setting value.
        // Take the fraction of the total possible squared volume and multiply by the maximum sample amplitude.
        var percentage = Math.Pow(volume, 2) / Math.Pow(MaxVolume, 2);
        Debug.Assert(percentage is >= 0 and <= 1);
        return (short)(percentage * short.MaxValue);
    }

    public short SampleVolume { get; private set; }

    /// <summary>
    /// Sets the volume of audio output (between <see cref="MinVolume"/> and <see cref="MaxVolume"/>).
    /// </summary>
    public int Volume
    {
        get;
        set
        {
            field = value;
            SampleVolume = ComputeSampleVolume(value);
            _logger.LogDebug($"New Volume: {value}, SampleVolume: {SampleVolume}", LogCategories.Audio);
        }
    }

    /// <summary>How many samples we have written into <see cref="_pcmBuffer"/> so far.</summary>
    private int _pcmBufferIndex;

    /// <summary>Partially accumulated value of the partial sample.</summary>
    private double _partialSignal;

    /// <summary>A value between [0, 1) which represents the proportion of a partial sample which has elapsed so far.</summary>
    private double _partialSample;

    internal bool AddPulse(int cpuClockHz, byte t1l)
    {
        Debug.Assert(_partialSample is >= 0 and < 1.0);
        Debug.Assert(_partialSignal is >= short.MinValue and <= short.MaxValue);

        var samplesPerCycle = (double)SampleRate / cpuClockHz;
        var pulseValue = t1l >= _compare;
        var sampleVolume = SampleVolume;
        if (!pulseValue)
            sampleVolume = (short)-sampleVolume;

        if (_partialSample != 0)
        {
            // Append to partial sample.
            var remaining = 1.0 - _partialSample;
            var isComplete = remaining < samplesPerCycle;
            var toAdd = isComplete ? remaining : samplesPerCycle;
            _partialSignal += sampleVolume * (double)toAdd;
            _partialSample += toAdd;
            samplesPerCycle -= toAdd;

            if (isComplete)
            {
                appendSample((short)Math.Round(_partialSignal));
                _partialSample = 0;
                _partialSignal = 0;
            }
        }

        if (samplesPerCycle == 0)
            return pulseValue;

        Debug.Assert(_partialSample == 0);
        var nSamples = (int)samplesPerCycle;
        for (var i = 0; i < nSamples; i++)
        {
            appendSample(sampleVolume);
        }

        // Setup pending sample for next call.
        _partialSample = samplesPerCycle - nSamples;
        _partialSignal = sampleVolume * _partialSample;
        return pulseValue;

        void appendSample(short sample)
        {
            _pcmBuffer[_pcmBufferIndex++] = (byte)(sample & 0xff);
            _pcmBuffer[_pcmBufferIndex++] = (byte)(sample >> 8 & 0xff);

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
            }
        }

        // TODO2: This seems to cut off some signals in practice
        bool hasRealSignal()
        {
            for (var j = 2; j < _pcmBuffer.Length; j += 2)
            {
                if (_pcmBuffer[j] != _pcmBuffer[0] || _pcmBuffer[j + 1] != _pcmBuffer[1])
                    return true;
            }

            return false;
        }
    }
}