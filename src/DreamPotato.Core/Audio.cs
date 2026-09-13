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
    /// Buffer which samples are currently being appended to.
    /// Both this and <see cref="_prevPcmBuffer"/> contain PCM data at <see cref="SampleRate"/> and <see cref="SampleSize"/>.
    /// </summary>
    private byte[] _currentPcmBuffer = new byte[PcmBufferFilledSize];

    /// <summary>
    /// Buffer which will be filtered and submitted once <see cref="_currentPcmBuffer"/> is filled.
    /// </summary>
    private byte[] _prevPcmBuffer = new byte[PcmBufferFilledSize];

    /// <summary>
    /// Counter of how long (in samples) T1LRUN has been reset.
    /// When the value is <see cref="T1lStagnantCount"/> or higher, we consider the timer to be stagnant.
    /// Capped at <see cref="T1lDisabledMaxCount"/> the net size of the buffers.
    /// </summary>
    /// <remarks>
    /// This is used for filtering. We could alternatively consider filtering based on signal only.
    /// With signal alone, though, it's difficult to distinguish "intended" short signals,
    /// from "unintended" pops created by changing timer parameters while timer is disabled.
    /// It's not obvious whether the subtle differences in purely signal-based filtering are desirable.
    /// For example, if enabling the timer causes signal to remain the same for a while, before changing,
    /// then filtering on signal alone might cause us to miss one of the edges in an "intended" PWM cycle.
    /// Empirical testing (maybe direct recording of the audio pin on real hardware) would be needed.
    /// </remarks>
    private int _t1lDisabledCount = T1lDisabledMaxCount;
    private const int T1lStagnantCount = PcmBufferSampleCount;
    private const int T1lDisabledMaxCount = PcmBufferSampleCount * 2;

    /// <summary>
    /// Pulse generator compare value.
    /// When the timer value is smaller than this, a low signal is generated, otherwise a high signal is generated.
    /// </summary>
    private byte _compare;

    internal Audio(Cpu cpu)
    {
        Debug.Assert(_currentPcmBuffer.Length == _prevPcmBuffer.Length);
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

    /// <summary>How many samples we have written into <see cref="_currentPcmBuffer"/> so far.</summary>
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
            Debug.Assert(_pcmBufferIndex % SampleSize == 0);

            if (t1lRun)
                filterIfNeeded(); // May need to filter when transitioning from disabled to enabled.

            _t1lDisabledCount = t1lRun ? 0 : Math.Min(_t1lDisabledCount + 1, T1lDisabledMaxCount);

            // Append the sample.
            _currentPcmBuffer[_pcmBufferIndex++] = (byte)(sample & 0xff);
            _currentPcmBuffer[_pcmBufferIndex++] = (byte)(sample >> 8 & 0xff);

            if (_pcmBufferIndex != _currentPcmBuffer.Length)
                return; // Current buffer not yet filled

            filterIfNeeded();
            AudioBufferReady?.Invoke(new(_prevPcmBuffer, Start: 0, Length: _prevPcmBuffer.Length));
            _pcmBufferIndex = 0;

            var tmp = _currentPcmBuffer;
            _currentPcmBuffer = _prevPcmBuffer;
            _prevPcmBuffer = tmp;
        }

        void filterIfNeeded()
        {
            if (_t1lDisabledCount < T1lStagnantCount)
                return; // Timer is not stagnant. No need to filter.

            // Filter the samples taken when the timer was stagnant.
            var nToDelete = _t1lDisabledCount;
            var i = _pcmBufferIndex / 2 - 1;
            for (; i >= 0; i--)
            {
                _currentPcmBuffer[i * 2] = 0;
                _currentPcmBuffer[i * 2 + 1] = 0;

                nToDelete--;
                if (nToDelete == 0)
                    return;
            }

            for (var j = PcmBufferSampleCount - 1; j >= 0; j--)
            {
                _prevPcmBuffer[j * 2] = 0;
                _prevPcmBuffer[j * 2 + 1] = 0;

                nToDelete--;
                if (nToDelete == 0)
                    return;
            }
        }
    }
}