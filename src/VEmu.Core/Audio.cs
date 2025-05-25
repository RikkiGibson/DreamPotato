using System.Buffers.Binary;
using System.Diagnostics;

namespace VEmu.Core;

public class Audio
{
    public const int SampleRate = 44100;
    public const int SampleSize = 2; // 16-bit

    private readonly Cpu _cpu;
    private readonly Logger _logger;

    internal Audio(Cpu cpu, Logger logger)
    {
        _cpu = cpu;
        _logger = logger;
    }

    /// <summary>
    /// 'true' if the emulation state is currently playing sound; otherwise, 'false'.
    /// </summary>
    public bool IsActive
    {
        get;
        internal set
        {
            var changed = field != value;
            field = value;
            if (changed)
                IsActiveChanged?.Invoke(value);
        }
    }

    public event Action<bool>? IsActiveChanged;

    /// <summary>
    /// Sets the volume of audio output (between 0 and <see cref="short.MaxValue"/>).
    /// </summary>
    public short Volume
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
        }
    } = short.MaxValue / 2;

    private readonly byte[] _lowSignal = new byte[2];
    private readonly byte[] _highSignal = new byte[2];

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

        // each sample is 2 bytes.
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
}