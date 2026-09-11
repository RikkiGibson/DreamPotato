using System.Diagnostics;

using DreamPotato.Core.SFRs;

namespace DreamPotato.Core;

public class Audio
{
    public const int SampleRate = 44100;
    public const int SampleSize = 2; // 16-bit
    public const int BufferDurationMilliseconds = 4;
    private const int MaxTotalBufferDurationMilliseconds = 96;
    public const int MaxQueuedBufferCount = MaxTotalBufferDurationMilliseconds / BufferDurationMilliseconds;

    public const int DefaultVolume = 50;

    public const int MinVolume = 0;
    public const int MaxVolume = 100;

    private readonly Cpu _cpu;
    private Logger _logger => _cpu.Logger;

    private const int PcmBufferSampleCount = SampleRate * BufferDurationMilliseconds / 1000;
    private const int PcmBufferFilledSize = PcmBufferSampleCount * SampleSize;

    /// <summary>
    /// PCM data at <see cref="SampleRate"/> and <see cref="SampleSize"/>.
    /// Note: data is double-buffered (hence 2 different arrays.)
    /// </summary>
    private readonly byte[] _pcmBuffer1 = new byte[PcmBufferFilledSize];

    /// <summary>
    /// PCM data at <see cref="SampleRate"/> and <see cref="SampleSize"/>.
    /// Note: data is double-buffered (hence 2 different arrays.)
    /// </summary>
    private readonly byte[] _pcmBuffer2 = new byte[PcmBufferFilledSize];

    private byte[] _currentPcmBuffer;

    /// <summary>
    /// Counter of how long (in samples) T1LRUN has been reset.
    /// </summary>
    private int _t1lDisabledCount = T1lDisabledMaxCount;
    private const int T1lDisabledMaxCount = PcmBufferSampleCount;

    /// <summary>
    /// Pulse generator compare value.
    /// When the timer value is smaller than this, a low signal is generated, otherwise a high signal is generated.
    /// </summary>
    private byte _compare;

    internal Audio(Cpu cpu)
    {
        _cpu = cpu;
        _currentPcmBuffer = _pcmBuffer1;
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

    internal bool AddPulse(int cpuClockHz, byte t1l, bool t1lRun)
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
            _currentPcmBuffer[_pcmBufferIndex++] = (byte)(sample & 0xff);
            _currentPcmBuffer[_pcmBufferIndex++] = (byte)(sample >> 8 & 0xff);

            // TODO2: all the code in most recent commit needs more scrutiny
            if (t1lRun)
            {
                if (_t1lDisabledCount == T1lDisabledMaxCount)
                {
                    // Transitioning from stagnant to running.
                    filter(_currentPcmBuffer,
                        prevBuffer: _currentPcmBuffer == _pcmBuffer1 ? _pcmBuffer2 : _pcmBuffer1,
                        endIndex: _pcmBufferIndex);
                }

                _t1lDisabledCount = 0;
            }
            else
            {
                _t1lDisabledCount = Math.Min(_t1lDisabledCount + 1, T1lDisabledMaxCount);
            }

            if (_pcmBufferIndex != _currentPcmBuffer.Length)
                return;

            var prevBuffer = _currentPcmBuffer == _pcmBuffer1 ? _pcmBuffer2 : _pcmBuffer1;
            Debug.Assert(prevBuffer.Length == _currentPcmBuffer.Length);
            if (_t1lDisabledCount == T1lDisabledMaxCount)
            {
                filter(_currentPcmBuffer, prevBuffer, _pcmBufferIndex);
            }

            AudioBufferReady?.Invoke(new(prevBuffer, Start: 0, Length: prevBuffer.Length));
            _pcmBufferIndex = 0;
            _currentPcmBuffer = prevBuffer;
        }

        void filter(byte[] currentBuffer, byte[] prevBuffer, int endIndex)
        {
            Debug.Assert(endIndex % SampleSize == 0);
            if (endIndex < SampleSize)
                return; // Nothing to filter.

            var last0 = currentBuffer[endIndex - 2];
            var last1 = currentBuffer[endIndex - 1];
            var i = endIndex / 2 - 1;
            for (; i >= 0; i--)
            {
                if (currentBuffer[i * 2] != last0
                    || currentBuffer[i * 2 + 1] != last1)
                {
                    break;
                }

                currentBuffer[i * 2] = 0;
                currentBuffer[i * 2 + 1] = 0;
            }

            if (i != -1)
                return; // Done filtering, didn't exhaust 'currentBuffer'.

            // Still more filtering to do.
            for (var j = prevBuffer.Length / 2 - 1; j >= 0; j--)
            {
                if (prevBuffer[j * 2] != last0
                    || prevBuffer[j * 2 + 1] != last1)
                {
                    break;
                }

                prevBuffer[j * 2] = 0;
                prevBuffer[j * 2 + 1] = 0;
            }
        }
    }
}