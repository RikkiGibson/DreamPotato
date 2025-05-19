using System.Diagnostics;

namespace VEmu.Core;

using SFRs;
using Ids = SpecialFunctionRegisterIds;

/// <summary>See VMD-40, table 2.6</summary>
public class SpecialFunctionRegisters
{
    public const int Size = 0x80;

    // TODO: Elysian docs state there are 143 SFRs, but, the memory space is only 0x80 (128 bytes).
    // Where are the extra 15?

    /// <summary>
    /// Reload data for <see cref="T1H"/> and <see cref="T1L"/>.
    /// </summary>
    private byte _t1hr, _t1lr;

    private readonly byte[] _rawMemory = new byte[Size];
    private readonly Cpu _cpu;
    private readonly byte[] _workRam;
    private readonly Logger _logger;

    public SpecialFunctionRegisters(Cpu cpu, byte[] workRam, Logger logger)
    {
        Debug.Assert(workRam.Length == 0x200);
        _cpu = cpu;
        _workRam = workRam;
        _logger = logger;
    }

    /// <summary>
    /// VMD-40
    /// </summary>
    public void Reset()
    {
        Array.Clear(_rawMemory);
        // NB: Memory owns clearing _workRam

        // Manual indicates that BIOS is typically responsible for setting these values.
        // It's nice to be able to run without a BIOS, so let's set them up here.
        Write(Ids.Ie, 0b1000_0000);
        Write(Ids.Ext, (byte)new Ext() { InstructionBank = InstructionBank.ROM });
        Write(Ids.P1Fcr, 0b1011_1111);
        Write(Ids.P3Int, 0b1111_1101);
        Write(Ids.P3, 0b1111_1111);
        Write(Ids.P7, (byte)new P7() { LowVoltage = true });
        Write(Ids.Isl, 0b1100_0000);
        Write(Ids.Vsel, 0b1111_1100);
        Write(Ids.Btcr, 0b0100_0001);
        Write(Ids.Vccr, (byte)new Vccr() { DisplayControl = true });

        // Stack points to last element, so, when it is empty, it points to before the start of the stack space.
        Write(Ids.Sp, Memory.StackStart - 1);
    }

    public byte Read(byte address)
    {
        Debug.Assert(address < Size);

        // TODO: there are many more special cases for reading/writing SFRs than this.
        switch (address)
        {
            case Ids.Vtrbf:
                return readWorkRam();

            case Ids.P7:
                { // breakpoint holder
                }
                goto default;

            case Ids.P3:
                { // breakpoint holder
                }
                goto default;

            default:
                return _rawMemory[address];
        }

        byte readWorkRam()
        {
            var address = (BitHelpers.ReadBit(Vrmad2, bit: 0) ? 0x100 : 0) | Vrmad1;
            var memory = _workRam[address];
            if (Vsel.Ince)
            {
                address++;
                Vrmad1 = (byte)address;
                Vrmad2 = (byte)((address & 0x100) != 0 ? 1 : 0);
            }

            return memory;
        }
    }

    public void Write(byte address, byte value)
    {
        Debug.Assert(address < Size);

        switch (address)
        {
            case Ids.Vtrbf:
                writeWorkRam(value);
                return;

            case Ids.T1L:
                // A write to T1L from user code sets the reload value
                _t1lr = value;
                return;

            case Ids.T1H:
                // A write to T1H from user code sets the reload value
                _t1hr = value;
                return;

            case Ids.Btcr:
                var btcr = new Btcr(value);

                if (btcr.Int0CycleControl)
                    _logger.LogWarning($"Setting unsupported Btcr configuration: 0b{value:b8}", LogCategories.Timers);

                var oldBtcr = new Btcr(_rawMemory[address]);
                if (oldBtcr.Int1CycleRate != btcr.Int1CycleRate)
                    _logger.LogDebug($"Changing base timer cycle rate to 0x{btcr.Int1CycleRate:X}", LogCategories.Timers);

                if (!btcr.CountEnable)
                        _cpu.BaseTimer = 0;

                goto default;

            case Ids.Isl:
                var isl = new Isl(value);
                if (isl is not { BaseTimerClock: BaseTimerClock.QuartzOscillator })
                    _logger.LogWarning($"Setting unsupported Isl configuration: 0b{value:b8}", LogCategories.Timers);

                goto default;

            case Ids.Ocr:
                var ocr = new Ocr(value);
                if (ocr is { ClockGeneratorControl: false, SystemClockSelector: Oscillator.Quartz })
                    _logger.LogWarning($"Setting unsupported Ocr configuration: 0b{value:b8}", LogCategories.SystemClock);

                var oldOcr = new Ocr(_rawMemory[address]);
                if (oldOcr.CpuClockHz != ocr.CpuClockHz)
                    _logger.LogDebug($"System clock changed from (CGC={oldOcr.ClockGeneratorControl}, {oldOcr.SystemClockSelector}, Hz={oldOcr.CpuClockHz}) to (CGC={ocr.ClockGeneratorControl}, {ocr.SystemClockSelector}, Hz={ocr.CpuClockHz})", LogCategories.SystemClock);

                goto default;

            case Ids.Ext:
                var ext = new Ext(value);
                if (ext is { Ext3: false })
                    _logger.LogWarning($"Setting unexpected Ext value. Ext3 should be 1, but was 0, in 0b{value:b8}");

                goto default;

            case Ids.Pcon:
                var oldPcon = new Pcon(_rawMemory[address]);
                var newPcon = new Pcon(value);

                if (!oldPcon.HaltMode && newPcon.HaltMode)
                    _logger.LogTrace("Entering halt mode", LogCategories.Halt);

                if (oldPcon.HaltMode && !newPcon.HaltMode)
                    _logger.LogTrace("Exiting halt mode", LogCategories.Halt);

                goto default;

            case Ids.P7:
                { // breakpoint holder
                }
                goto default;

            case Ids.Sp:
                { // breakpoint holder
                }
                goto default;

            case Ids.P3Int:
                if (_rawMemory[address] != value)
                    _logger.LogTrace($"P3Int changed: Old=0b{_rawMemory[address]:b} New=0b{value:b}");

                goto default;

            default:
                _rawMemory[address] = value;
                return;
        }

        void writeWorkRam(byte value)
        {
            var address = (BitHelpers.ReadBit(Vrmad2, bit: 0) ? 0x100 : 0) | Vrmad1;
            _workRam[address] = value;

            if (Vsel.Ince)
            {
                address++;
                Vrmad1 = (byte)address;
                Vrmad2 = (byte)((address & 0x100) != 0 ? 1 : 0);
            }
        }
    }

    /// <summary>Accumulator. VMD-50</summary>
    public byte Acc
    {
        get => Read(Ids.Acc);
        set => Write(Ids.Acc, value);
    }

    /// <summary>Program status word. VMD-52</summary>
    // TODO: operations which modify Acc are supposed to set or reset P accordingly.
    public Psw Psw
    {
        get => new(Read(Ids.Psw));
        set => Write(Ids.Psw, (byte)value);
    }

    /// <summary>B register. VMD-51</summary>
    public byte B
    {
        get => Read(Ids.B);
        set => Write(Ids.B, value);
    }
    /// <summary>C register. VMD-51</summary>
    public byte C
    {
        get => Read(Ids.C);
        set => Write(Ids.C, value);
    }

    /// <summary>Table reference register lower byte. VMD-54</summary>
    public byte Trl
    {
        get => Read(Ids.Trl);
        set => Write(Ids.Trl, value);
    }
    /// <summary>Table reference register upper byte. VMD-54</summary>
    public byte Trh
    {
        get => Read(Ids.Trh);
        set => Write(Ids.Trh, value);
    }

    /// <summary>Stack pointer. VMD-53</summary>
    /// <remarks>Note that a well-behaved stack pointer always refers to 0x80 of RAM bank 0, growing upwards.</remarks>
    public byte Sp
    {
        get => Read(Ids.Sp);
        set => Write(Ids.Sp, value);
    }

    /// <summary>Power control register. VMD-158</summary>
    public Pcon Pcon
    {
        get => new(Read(Ids.Pcon));
        set => Write(Ids.Pcon, (byte)value);
    }

    /// <summary>Master interrupt enable control register. VMD-138</summary>
    public Ie Ie
    {
        get => new(Read(Ids.Ie));
        set => Write(Ids.Ie, (byte)value);
    }

    /// <summary>Interrupt priority control register. VMD-151</summary>
    public Ip Ip
    {
        get => new(Read(Ids.Ip));
        set => Write(Ids.Ip, (byte)value);
    }

    /// <summary>External memory control register. Undocumented.</summary>
    public Ext Ext
    {
        get => new(Read(Ids.Ext));
        set => Write(Ids.Ext, (byte)value);
    }

    /// <summary>Oscillation control register. VMD-156</summary>
    public Ocr Ocr
    {
        get => new(Read(Ids.Ocr));
        set => Write(Ids.Ocr, (byte)value);
    }

    /// <summary>Timer 0 control register. VMD-67</summary>
    public T0Cnt T0Cnt
    {
        get => new(Read(Ids.T0Cnt));
        set => Write(Ids.T0Cnt, (byte)value);
    }

    /// <summary>Timer 0 prescaler data. VMD-71</summary>
    public byte T0Prr
    {
        get => Read(Ids.T0Prr);
        set => Write(Ids.T0Prr, value);
    }

    /// <summary>Timer 0 low. VMD-71</summary>
    public byte T0L
    {
        get => Read(Ids.T0L);
        set => Write(Ids.T0L, value);
    }

    /// <summary>Timer 0 low reload data. VMD-71</summary>
    public byte T0Lr
    {
        get => Read(Ids.T0Lr);
        set => Write(Ids.T0Lr, value);
    }

    /// <summary>Timer 0 high. VMD-72</summary>
    public byte T0H
    {
        get => Read(Ids.T0H);
        set => Write(Ids.T0H, value);
    }

    /// <summary>Timer 0 high reload data. VMD-72</summary>
    public byte T0Hr
    {
        get => Read(Ids.T0Hr);
        set => Write(Ids.T0Hr, value);
    }

    /// <summary>Timer 1 control register. VMD-83</summary>
    public T1Cnt T1Cnt
    {
        get => new(Read(Ids.T1Cnt));
        set => Write(Ids.T1Cnt, (byte)value);
    }

    /// <summary>Timer 1 low comparison data. VMD-86</summary>
    public byte T1Lc
    {
        get => Read(Ids.T1Lc);
        set => Write(Ids.T1Lc, value);
    }

    /// <summary>
    /// Timer 1 low. VMD-85.
    /// Note that ordinarily, user code can only read this register.
    /// Since user code does not use this property, setting this causes the raw timer value to be updated.
    /// </summary>
    public byte T1L
    {
        get => Read(Ids.T1L);
        set => _rawMemory[Ids.T1L] = value;
    }

    /// <summary>
    /// Timer 1 low reload data. VMD-85.
    /// Note that ordinarily, user code can only write this register.
    /// Since user code does not use this property, reading this returns the raw reload value, not the timer value.
    /// </summary>
    public byte T1Lr
    {
        get => _t1lr;
        set => Write(Ids.T1L, value);
    }

    /// <summary>Timer 1 high comparison data. VMD-87</summary>
    public byte T1Hc
    {
        get => Read(Ids.T1Hc);
        set => Write(Ids.T1Hc, value);
    }

    /// <summary>
    /// Timer 1 high. VMD-86.
    /// Since user code does not use this property, setting this causes the raw timer value to be updated.
    /// </summary>
    public byte T1H
    {
        get => Read(Ids.T1H);
        set => _rawMemory[Ids.T1H] = value;
    }

    /// <summary>
    /// Timer 1 high reload data. VMD-86.
    /// Note that ordinarily, user code can only write this register.
    /// Since user code does not use this property, reading this returns the raw reload value, not the timer value.
    /// </summary>
    public byte T1Hr
    {
        get => _t1hr;
        set => Write(Ids.T1H, value);
    }

    /// <summary>Mode control register. VMD-127</summary>
    public byte Mcr
    {
        get => Read(Ids.Mcr);
        set => Write(Ids.Mcr, value);
    }

    /// <summary>Start address register. VMD-129</summary>
    public byte Stad
    {
        get => Read(Ids.Stad);
        set => Write(Ids.Stad, value);
    }

    /// <summary>Character count register. Affects operation of the LCD. Not intended to be used by applications. VMD-130</summary>
    public byte Cnr
    {
        get => Read(Ids.Cnr);
        set => Write(Ids.Cnr, value);
    }

    /// <summary>Time division register. Affects operation of the LCD. Not intended to be used by applications. VMD-130</summary>
    public byte Tdr
    {
        get => Read(Ids.Tdr);
        set => Write(Ids.Tdr, value);
    }

    /// <summary>Bank address register. Bits 1-0 control whether XRAM bank 0, 1, or 2 is in use. VMD-125</summary>
    public byte Xbnk
    {
        get => Read(Ids.Xbnk);
        set => Write(Ids.Xbnk, value);
    }

    /// <summary>LCD contrast control register. VMD-131</summary>
    public Vccr Vccr
    {
        get => new(Read(Ids.Vccr));
        set => Write(Ids.Vccr, (byte)value);
    }

    /// <summary>SIO0 control register. VMD-108</summary>
    public byte Scon0
    {
        get => Read(Ids.Scon0);
        set => Write(Ids.Scon0, value);
    }

    /// <summary>SIO0 buffer. VMD-113</summary>
    public byte Sbuf0
    {
        get => Read(Ids.Sbuf0);
        set => Write(Ids.Sbuf0, value);
    }

    /// <summary>SIO0 baud rate generator. VMD-113</summary>
    public byte Sbr
    {
        get => Read(Ids.Sbr);
        set => Write(Ids.Sbr, value);
    }

    /// <summary>SIO1 control register. VMD-111</summary>
    public byte Scon1
    {
        get => Read(Ids.Scon1);
        set => Write(Ids.Scon1, value);
    }

    /// <summary>SIO1 buffer. VMD-113</summary>
    public byte Sbuf1
    {
        get => Read(Ids.Sbuf1);
        set => Write(Ids.Sbuf1, value);
    }

    /// <summary>Port 1 latch. VMD-58</summary>
    public byte P1
    {
        get => Read(Ids.P1);
        set => Write(Ids.P1, value);
    }

    /// <summary>Port 1 data direction register. VMD-58</summary>
    public byte P1Ddr
    {
        get => Read(Ids.P1Ddr);
        set => Write(Ids.P1Ddr, value);
    }

    /// <summary>Port 1 function control register. VMD-59</summary>
    public byte P1Fcr
    {
        get => Read(Ids.P1Fcr);
        set => Write(Ids.P1Fcr, value);
    }

    /// <summary>
    /// Port 3. Buttons SLEEP, MODE, B, A, directions. VMD-54.
    /// When this property is written, it indicates a change in an external signal, and may generate <see cref="Interrupts.P3"/>.
    /// When 'Write(Ids.P3, value)' is called, it is simply a write to the latch.
    /// </summary>
    public P3 P3
    {
        get => new(Read(Ids.P3));
        set
        {
            var valueRaw = (byte)value;
            var p3int = P3Int;
            if (p3int.Enable)
            {
                var p3Raw = Read(Ids.P3);
                // NB: Continuous interrupts are generated in Cpu.Step
                if (!p3int.Continuous && (p3Raw & valueRaw) != p3Raw)
                {
                    _logger.LogDebug($"Requesting interrupt P3 Continuous={p3int.Continuous} Before=0b{p3Raw:b} After=0b{valueRaw:b}");
                    p3int.Source = true;
                    _cpu.RequestedInterrupts |= Interrupts.P3;
                }
                P3Int = p3int;
            }
            Write(Ids.P3, valueRaw);
        }
    }

    /// <summary>Port 3 data direction register. VMD-62</summary>
    public byte P3Ddr
    {
        get => Read(Ids.P3Ddr);
        set => Write(Ids.P3Ddr, value);
    }

    /// <summary>Port 3 interrupt function control register. VMD-62</summary>
    public P3Int P3Int
    {
        get => new(Read(Ids.P3Int));
        set => Write(Ids.P3Int, (byte)value);
    }

    /// <summary>Flash Program Register. Undocumented.</summary>
    public FPR FPR
    {
        get => new(Read(Ids.FPR));
        set => Write(Ids.FPR, (byte)value);
    }

    /// <summary>Port 7 latch. VMD-64</summary>
    public P7 P7
    {
        get => new(Read(Ids.P7));
        set => Write(Ids.P7, (byte)value);
    }

    /// <summary>External interrupt 0, 1 control. VMD-135</summary>
    public I01Cr I01Cr
    {
        get => new(Read(Ids.I01Cr));
        set => Write(Ids.I01Cr, (byte)value);
    }

    /// <summary>External interrupt 2, 3 control. VMD-137</summary>
    public I23Cr I23Cr
    {
        get => new(Read(Ids.I23Cr));
        set => Write(Ids.I23Cr, (byte)value);
    }

    /// <summary>Input signal select. VMD-138</summary>
    public Isl Isl
    {
        get => new(Read(Ids.Isl));
        set => Write(Ids.Isl, (byte)value);
    }

#region Work RAM
    /// <summary>Control register. VMD-143</summary>
    /// TODO: the application is only supposed to be able to alter bit 4.
    public Vsel Vsel
    {
        get => new(Read(Ids.Vsel));
        set => Write(Ids.Vsel, (byte)value);
    }

    /// <summary>Bits 0-7 of Vramad (work RAM address). VMD-144</summary>
    public byte Vrmad1
    {
        get => Read(Ids.Vrmad1);
        set => Write(Ids.Vrmad1, value);
    }

    /// <summary>Bit 8 of Vramad (work RAM address). VMD-144</summary>
    public byte Vrmad2
    {
        get => Read(Ids.Vrmad2);
        set => Write(Ids.Vrmad2, value);
    }

    /// <summary>Work RAM value. (determined by <see cref="Vrmad1"/> and <see cref="Vrmad2"/>). VMD-144</summary>
    public byte Vtrbf
    {
        get => Read(Ids.Vtrbf);
        set => Write(Ids.Vtrbf, value);
    }
#endregion
    /// <summary>Base timer control. VMD-101</summary>
    public Btcr Btcr
    {
        get => new(Read(Ids.Btcr));
        set => Write(Ids.Btcr, (byte)value);
    }
}