using System.Buffers.Binary;
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
    /// When the value is <see cref="T1lDisabledMaxCount"/>, we consider the timer to be stagnant.
    /// </summary>
    /// <remarks>
    /// This is used for filtering. We could alternatively consider filtering
    /// based on a signal value remaining unchanged for certain number of samples.
    /// It's not obvious whether the subtle differences in purely signal-based filtering are desirable.
    /// For example, if enabling the timer causes signal to remain the same for a while, before changing,
    /// then filtering on signal alone might cause us to miss one of the edges in an "intended" PWM cycle.
    /// Empirical testing (maybe direct recording of the audio pin on real hardware) would be needed.
    /// </remarks>
    private int _t1lDisabledCount = T1lDisabledMaxCount;
    private const int T1lDisabledMaxCount = PcmBufferSampleCount;

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
                filter1(); // May need to filter when transitioning from disabled to enabled.

            _t1lDisabledCount = t1lRun ? 0 : Math.Min(_t1lDisabledCount + 1, T1lDisabledMaxCount);

            // Append the sample.
            _currentPcmBuffer[_pcmBufferIndex++] = (byte)(sample & 0xff);
            _currentPcmBuffer[_pcmBufferIndex++] = (byte)(sample >> 8 & 0xff);

            if (_pcmBufferIndex != _currentPcmBuffer.Length)
                return; // Current buffer not yet filled

            filter1();
            filter2();
            AudioBufferReady?.Invoke(new(_prevPcmBuffer, Start: 0, Length: _prevPcmBuffer.Length));
            _pcmBufferIndex = 0;

            var tmp = _currentPcmBuffer;
            _currentPcmBuffer = _prevPcmBuffer;
            _prevPcmBuffer = tmp;
        }

        // When the timer is stagnant it leaves the signal at a constant level for a long period.
        // These signals sound like pops or distortion.
        // In our case the period we care about is 'T1lDisabledMaxCount' or more samples.
        // These artifacts will generally be before/after a meaningful tone.
        void filter1()
        {
            if (_t1lDisabledCount != T1lDisabledMaxCount)
                return; // Timer is not stagnant. No need to filter.

            var (last0, last1) = _pcmBufferIndex < SampleSize
                ? (_prevPcmBuffer[^2], _currentPcmBuffer[^1])
                : (_currentPcmBuffer[_pcmBufferIndex - 2], _currentPcmBuffer[_pcmBufferIndex - 1]);

            var i = _pcmBufferIndex / 2 - 1;
            for (; i >= 0; i--)
            {
                if (_currentPcmBuffer[i * 2] != last0
                    || _currentPcmBuffer[i * 2 + 1] != last1)
                {
                    break;
                }

                _currentPcmBuffer[i * 2] = 0;
                _currentPcmBuffer[i * 2 + 1] = 0;
            }

            if (i != -1)
                return; // Finished filtering without exhausting '_currentPcmBuffer'

            // Still more filtering to do.
            for (var j = _prevPcmBuffer.Length / 2 - 1; j >= 0; j--)
            {
                if (_prevPcmBuffer[j * 2] != last0
                    || _prevPcmBuffer[j * 2 + 1] != last1)
                {
                    break;
                }

                _prevPcmBuffer[j * 2] = 0;
                _prevPcmBuffer[j * 2 + 1] = 0;
            }
        }

        // Ensure that the double-buffers have a reasonable minimum number of edge transitions.
        // That is, points where signal changes between positive/negative.
        // If there are only a very small number of such transitions, the signal is likely just a pop.
        void filter2()
        {
            const int MinTransitions = 3;
            var numTransitions = 0;
            var firstSample = BinaryPrimitives.ReadInt16LittleEndian(_prevPcmBuffer.AsSpan(0, length: 2));
            bool positive = firstSample >= 0;
            for (int i = 1; i < PcmBufferSampleCount; i++)
            {
                if (!checkSample(_prevPcmBuffer, i))
                    return;
            }

            for (int i = 0; i < PcmBufferSampleCount; i++)
            {
                if (!checkSample(_currentPcmBuffer, i))
                    return;
            }

            if (numTransitions < MinTransitions)
                Array.Clear(_prevPcmBuffer);

            return;

            // returns false if filtering should stop
            bool checkSample(byte[] buffer, int sampleIndex)
            {
                var sample = BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(sampleIndex * 2, length: 2));
                var newPositive = sample >= 0;
                if (newPositive != positive)
                {
                    numTransitions++;
                    if (numTransitions >= MinTransitions)
                        return false;
                }

                positive = newPositive;
                return true;
            }
        }
    }
}