namespace VEmu.Core;

/// <summary>
/// Addresses of special function registers by name. Each member includes only the last byte of address.
/// So, in order to access an SFR within the full address space, 0x100 must be OR'd in first.
/// </summary>
static class SpecialFunctionRegisterIds
{
    /// <summary>Accumulator. VMD-50</summary>
    public const byte Acc = 0x00;

    /// <summary>Program status word. VMD-52</summary>
    public const byte Psw = 0x01;

    /// <summary>B register. VMD-51</summary>
    public const byte B = 0x02;
    /// <summary>C register. VMD-51</summary>
    public const byte C = 0x03;

    /// <summary>Table reference register lower byte. VMD-54</summary>
    public const byte Trl = 0x04;
    /// <summary>Table reference register upper byte. VMD-54</summary>
    public const byte Trh = 0x05;

    /// <summary>Stack pointer. VMD-53</summary>
    /// <remarks>Note that a well-behaved stack pointer always refers to 0x80 of RAM bank 0, growing upwards.</remarks>
    public const byte Sp = 0x06;

    /// <summary>Power control register. VMD-158</summary>
    public const byte Pcon = 0x07;

    /// <summary>Master interrupt enable control register. VMD-138</summary>
    public const byte Ie = 0x08;

    /// <summary>Interrupt priority control register. VMD-151</summary>
    public const byte Ip = 0x09;

    /// <summary>External memory control register. No VMD page</summary>
    public const byte Ext = 0x0d;

    /// <summary>Oscillation control register. VMD-156</summary>
    public const byte Ocr = 0x0e;

    /// <summary>Timer 0 control register. VMD-67</summary>
    public const byte T0Cnt = 0x10;

    /// <summary>Timer 0 prescaler data. VMD-71</summary>
    public const byte T0Prr = 0x11;

    /// <summary>Timer 0 low. VMD-71</summary>
    public const byte T0L = 0x12;

    /// <summary>Timer 0 low reload data. VMD-71</summary>
    public const byte T0Lr = 0x13;

    /// <summary>Timer 0 high. VMD-72</summary>
    public const byte T0H = 0x14;

    /// <summary>Timer 0 high reload data. VMD-72</summary>
    public const byte T0Hr = 0x15;

    /// <summary>Timer 1 control register. VMD-83</summary>
    public const byte T1Cnt = 0x18;

    /// <summary>Timer 1 low comparison data. VMD-86</summary>
    public const byte T1Lc = 0x1a;

    /// <summary>When read: Timer 1 low. When written: Timer 1 low reload data. VMD-85</summary>
    public const byte T1L = 0x1b;

    /// <summary>Timer 1 high comparison data. VMD-87</summary>
    public const byte T1Hc = 0x1c;

    /// <summary>When read: Timer 1 high. When written: Timer 1 high reload data (T1Hr). VMD-86</summary>
    public const byte T1H = 0x1d;

    /// <summary>Mode control register. VMD-127</summary>
    public const byte Mcr = 0x20;

    /// <summary>Start address register. VMD-129</summary>
    public const byte Stad = 0x22;

    /// <summary>Character count register. VMD-130</summary>
    public const byte Cnr = 0x23;

    /// <summary>Time division register. VMD-130</summary>
    public const byte Tdr = 0x24;

    /// <summary>Bank address register. VMD-130</summary>
    public const byte Xbnk = 0x25;

    /// <summary>LCD contrast control register. VMD-131</summary>
    public const byte Vccr = 0x27;

    /// <summary>SIO0 control register. VMD-108</summary>
    public const byte Scon0 = 0x30;

    /// <summary>SIO0 buffer. VMD-113</summary>
    public const byte Sbuf0 = 0x31;

    /// <summary>SIO0 baud rate generator. VMD-113</summary>
    public const byte Sbr = 0x32;

    /// <summary>SIO1 control register. VMD-111</summary>
    public const byte Scon1 = 0x34;

    /// <summary>SIO1 buffer. VMD-113</summary>
    public const byte Sbuf1 = 0x35;

    /// <summary>Port 1 latch. VMD-58</summary>
    public const byte P1 = 0x44;

    /// <summary>Port 1 data direction register. VMD-58</summary>
    public const byte P1Ddr = 0x45;

    /// <summary>Port 1 function control register. VMD-59</summary>
    public const byte P1Fcr = 0x46;

    /// <summary>Port 3 latch. VMD-54</summary>
    public const byte P3 = 0x4C;

    /// <summary>Port 3 data direction register. VMD-62</summary>
    public const byte P3Ddr = 0x4D;

    /// <summary>Port 3 interrupt function control register. VMD-62</summary>
    public const byte P3Int = 0x4E;

    /// <summary>Flash Program Register. Undocumented.</summary>
    public const byte FPR = 0x54;

    /// <summary>Port 7 latch. Used for low voltage detection and Dreamcast connection status. VMD-64</summary>
    public const byte P7 = 0x5C;

    /// <summary>External interrupt 0, 1 control. VMD-135</summary>
    public const byte I01Cr = 0x5D;

    /// <summary>External interrupt 2, 3 control. VMD-137</summary>
    public const byte I23Cr = 0x5E;

    /// <summary>Input signal select. (Cannot be manipulated by application.) VMD-138</summary>
    public const byte Isl = 0x5F;

    /// <summary>Control register. VMD-143</summary>
    public const byte Vsel = 0x63;

    /// <summary>System address register 1. VMD-144</summary>
    public const byte Vrmad1 = 0x64;

    /// <summary>System address register 2. VMD-144</summary>
    public const byte Vrmad2 = 0x65;

    /// <summary>Work RAM access (Send/receive buffer). VMD-144</summary>
    public const byte Vtrbf = 0x66;

    /// <summary>Base timer control. VMD-101</summary>
    public const byte Btcr = 0x7F;
}
