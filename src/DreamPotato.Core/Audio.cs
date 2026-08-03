using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;

using DreamPotato.Core.SFRs;

using LibSampleRateDotNet;

namespace DreamPotato.Core;

public class Audio
{
    public const int OutputSampleRate = 48000;
    private const int SampleSize = 2; // 16-bit
    private const int BufferDurationMilliseconds = 100;

    public const int DefaultVolume = 50;

    public const int MinVolume = 0;
    public const int MaxVolume = 100;

    private readonly Cpu _cpu;
    private readonly Logger _logger;

    /// <summary>Note: can change whenever the cycle clock oscillator changes.</summary>
    private int InputSampleRate => _cpu.SFRs.Ocr.CpuClockHz;
    private int PcmBufferFilledSize => InputSampleRate * SampleSize * BufferDurationMilliseconds / 1000;

    /// <summary>
    /// PCM buffer used by MonoGame. 16-bit sample size.
    /// </summary>
    private readonly byte[] _monoGameOutBuffer = new byte[2 * OutputSampleRate * SampleSize * BufferDurationMilliseconds / 1000];

    /// <summary>PCM input buffer used by libsamplerate (Secret Rabbit Code).</summary>
    private readonly float[] _srcInBuffer = new float[2 * MaxInputSampleRate * BufferDurationMilliseconds / 1000];

    private const int MaxInputSampleRate = OscillatorHz.Rc / 6;
    /// <summary>PCM output buffer used by libsamplerate (Secret Rabbit Code).</summary>
    private readonly float[] _srcOutBuffer = new float[2 * OutputSampleRate * BufferDurationMilliseconds / 1000];

    // Note: if we ever care about tearing down these instances, then, 'Audio' and its containers should probably be IDisposable
    private readonly unsafe SRC_STATE_tag* srcState;

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
        unsafe
        {
            int error = 0;
            srcState = LibSampleRate.src_new(
                LibSampleRate.SRC_SINC_MEDIUM_QUALITY,
                channels: 1,
                &error);

            if (srcState == null)
            {
                var errorString = Marshal.PtrToStringUTF8((IntPtr)LibSampleRate.src_strerror(error));
                throw new Exception($"LibSampleRate error: {errorString}");
            }
        }
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
                SubmitAudioBuffer();
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
            _highSignalFloat = (float)sampleVolume / short.MaxValue;
            _lowSignalFloat = -_highSignalFloat;
        }
    }

    private readonly byte[] _highSignal = new byte[2];
    private readonly byte[] _lowSignal = new byte[2];

    private float _highSignalFloat;
    private float _lowSignalFloat;

    /// <summary>Writer-index into <see cref="_srcInBuffer"/>.</summary>
    private int _srcInBufferIndex;

    /// <summary>Writer-index into <see cref="_srcOutBuffer"/>.</summary>
    private int _srcOutBufferIndex;

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
        // NOTE: this is likely wrong now that audio internally stores a compare value
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

        var samplesPerTimerPeriod = timerTicksPerPeriod * OutputSampleRate / cpuClockHz;
        var samplesAtLowSignal = timerTicksAtLowSignal * OutputSampleRate / cpuClockHz;

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

        // 1 clock cycle = 1 sample at all times
        var pulseValue = t1l >= _compare;
        var signal = pulseValue ? _highSignalFloat : _lowSignalFloat;
        _srcInBuffer[_srcInBufferIndex++] = signal;

        if (_srcOutBufferIndex + (_srcInBufferIndex * (double)OutputSampleRate / InputSampleRate) >= PcmBufferFilledSize)
        {
            // Time to resample then submit the audio buffer.
            _logger.LogDebug($"Submitting audio buffer of length {_srcInBufferIndex}", LogCategories.Audio);
            unsafe
            {
                fixed (float* srcInPtr = _srcInBuffer)
                fixed (float* srcOutPtr = _srcOutBuffer)
                fixed (byte* monogameOutPtr = _monoGameOutBuffer)
                {
                    // See 'libsamplerate/source/docs/api_full.md', section 'Process'.
                    var srcData = new SRC_DATA()
                    {
                        // : A pointer to the input data samples.
                        data_in = srcInPtr,
                        // : The number of frames of data pointed to by data_in.
                        input_frames = new CLong(_srcInBufferIndex),
                        // : A pointer to the output data samples.
                        data_out = srcOutPtr + _srcOutBufferIndex,
                        // : Maximum number of frames pointer to by data_out.
                        output_frames = new CLong(_srcOutBuffer.Length - _srcOutBufferIndex),
                        // : Equal to output_sample_rate / input_sample_rate.
                        src_ratio = (double)OutputSampleRate / InputSampleRate,
                        // : Equal to 0 if more input data is available and 1 otherwise.
                        end_of_input = 0
                    };

                    if (LibSampleRate.src_process(this.srcState, &srcData) is not 0 and var errorCode)
                    {
                        var errorString = Marshal.PtrToStringUTF8((IntPtr)LibSampleRate.src_strerror(errorCode));
                        throw new Exception($"LibSampleRate error: {errorString}");
                    }

                    if (srcData.input_frames_used.Value != _srcInBufferIndex)
                        throw new Exception($"LibSampleRate did not use all input frames: _srcInBufferIndex: {_srcInBufferIndex}, input_frames_used: {srcData.input_frames_used.Value}");

                    _srcOutBufferIndex += (int)srcData.output_frames_gen.Value;
                    LibSampleRate.src_float_to_short_array(srcOutPtr, (short*)monogameOutPtr, _srcOutBufferIndex);
                }
            }

            AudioBufferReady?.Invoke(new(_monoGameOutBuffer, Start: 0, Length: _srcOutBufferIndex * SampleSize));
            _srcInBufferIndex = 0;
            _srcOutBufferIndex = 0;
        }

        return pulseValue;
    }

    internal void SubmitAudioBuffer()
    {
        if (_srcInBufferIndex == 0)
            return;

        _logger.LogDebug($"EndAudio: Submitting audio buffer of length {_srcInBufferIndex}", LogCategories.Audio);
        if (_cpu.SFRs.Ocr.CpuClockHz is not (OscillatorHz.Quartz / 6 or OscillatorHz.Quartz / 12))
        {
            _logger.LogWarning(
                $"Sample rate not compatible with clock {_cpu.SFRs.Ocr.SystemClockSelector}.",
                LogCategories.Audio);
        }

        // AudioBufferReady?.Invoke(new(_monoGameOutBuffer, 0, _srcInBufferIndex));
        _srcInBufferIndex = 0;
    }
}