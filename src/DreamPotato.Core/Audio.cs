using System.Buffers.Binary;
using System.Diagnostics;

namespace DreamPotato.Core;

public class Audio
{
    public const int SampleRate = 48000;
    public const int SampleSize = 2; // 16-bit
    public const int BufferDurationMilliseconds = 50;

    private readonly Cpu _cpu;
    private readonly Logger _logger;

    private const int PcmBufferFilledSize = SampleRate * SampleSize * BufferDurationMilliseconds / 1000;
    /// <summary>
    /// PCM data at <see cref="SampleRate"/> and <see cref="SampleSize"/>.
    /// </summary>
    private readonly byte[] _pcmBuffer = new byte[2 * PcmBufferFilledSize];

    internal Audio(Cpu cpu, Logger logger)
    {
        _cpu = cpu;
        _logger = logger;
        Volume = short.MaxValue / 8;
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

    public record struct AudioBufferReadyEventArgs(byte[] Buffer, int Start, int Length);
    public event Action<AudioBufferReadyEventArgs>? AudioBufferReady;

    /// <summary>
    /// Sets the volume of audio output (between 0 and <see cref="short.MaxValue"/>).
    /// </summary>
    public short Volume
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            BinaryPrimitives.WriteInt16LittleEndian(_highSignal, value);
            BinaryPrimitives.WriteInt16LittleEndian(_lowSignal, (short)-value);
            field = value;
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
    public int Generate(Span<byte> buffer)
    {
        if (!IsActive)
            return -1;

        _logger.LogDebug($"Generating audio buffer of size {buffer.Length}", LogCategories.Audio);

        var cpuClockHz = _cpu.SFRs.Ocr.CpuClockHz;
        var t1lc = _cpu.SFRs.T1Lc;
        var t1lr = _cpu.SFRs.T1Lr;

        // Duty cycle:
        // while t1lc < t1l, signal is low.
        // while t1lc >= t1l, signal is high.

        // Typical setup: (R=Reload, C=Compare, M=Max)
        // R----C----M

        var timerTicksPerPeriod = 0xff - t1lr;
        var timerTicksAtLowSignal = t1lc - t1lr;
        if (timerTicksPerPeriod < 2 || timerTicksAtLowSignal <= 0)
        {
            _logger.LogWarning($"Could not play sound with T1lc={t1lc:X} T1lr={t1lr:X}");
            return -1;
        }
        Debug.Assert(timerTicksAtLowSignal < timerTicksPerPeriod);

        var samplesPerTimerPeriod = timerTicksPerPeriod * SampleRate / cpuClockHz;
        var samplesAtLowSignal = timerTicksAtLowSignal * SampleRate / cpuClockHz;

        BinaryPrimitives.WriteInt16LittleEndian(_highSignal, Volume);
        BinaryPrimitives.WriteInt16LittleEndian(_lowSignal, (short)-Volume);

        int bufferIndex;
        for (bufferIndex = 0; bufferIndex < buffer.Length - samplesPerTimerPeriod * 2;)
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
    /// Appends a pulse <see cref="value"/> to the PCM buffer for 1 cycle at <see cref="cpuClockHz"/>.
    /// </summary>
    internal void AddPulse(int cpuClockHz, bool value)
    {
        var sampleRateAndRemainder = SampleRate + _pcmRemainder;
        var samplesPerCycle = sampleRateAndRemainder / cpuClockHz;
        _pcmRemainder = sampleRateAndRemainder % cpuClockHz;

        var signal = value ? _highSignal : _lowSignal;
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
    }

    private void EndAudio()
    {
        if (_pcmBufferIndex == 0)
            return;

        _logger.LogDebug($"EndAudio: Submitting audio buffer of length {_pcmBufferIndex}", LogCategories.Audio);
        AudioBufferReady?.Invoke(new(_pcmBuffer, 0, _pcmBufferIndex));
        _pcmBufferIndex = 0;
    }
}