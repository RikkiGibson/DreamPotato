namespace VEmu.Core;

public enum SpecialFunctionRegisterKind : ushort
{
    /// <summary>Accumulator. VMD-50</summary>
    Acc = 0x100,

    /// <summary>Program status word. VMD-52</summary>
    Psw = 0x101,

    /// <summary>B register. VMD-51</summary>
    B = 0x102,
    /// <summary>C register. VMD-51</summary>
    C = 0x103,

    /// <summary>Table reference register lower byte. VMD-54</summary>
    Trl = 0x104,
    /// <summary>Table reference register upper byte. VMD-54</summary>
    Trh = 0x105,

    /// <summary>Stack pointer. VMD-53</summary>
    /// <remarks>Note that a well-behaved stack pointer always refers to 0x80 of RAM bank 0, growing upwards.</remarks>
    Sp = 0x106,

    /// <summary>Power control register. VMD-158</summary>
    Pcon = 0x107,

    /// <summary>Master interrupt enable control register. VMD-138</summary>
    Ie = 0x108,

    /// <summary>Interrupt priority control register. VMD-151</summary>
    Ip = 0x109,

    /// <summary>External memory control register. No VMD page</summary>
    Ext = 0x10d,

    /// <summary>Oscillation control register. VMD-156</summary>
    Ocr = 0x10e,

    /// <summary>Timer 0 control register. VMD-67</summary>
    T0Cnt = 0x110,

    /// <summary>Timer 0 prescaler data. VMD-71</summary>
    T0Prr = 0x111,

    /// <summary>Timer 0 low. VMD-71</summary>
    T0L = 0x112,

    /// <summary>Timer 0 low reload data. VMD-71</summary>
    T0Lr = 0x113,

    /// <summary>Timer 0 high. VMD-72</summary>
    T0H = 0x114,

    /// <summary>Timer 0 high reload data. VMD-72</summary>
    T0Hr = 0x115,

    /// <summary>Timer 1 control register. VMD-83</summary>
    T1Cnt = 0x118,

    /// <summary>Timer 1 low comparison data. VMD-86</summary>
    T1Lc = 0x11a,

    /// <summary>Timer 1 low. VMD-85</summary>
    T1L = 0x11b,

    /// <summary>Timer 1 low reload data. VMD-85</summary>
    T1Lr = 0x9999,

    /// <summary>Timer 1 high comparison data. VMD-87</summary>
    T1Hc = 0x11c,

    /// <summary>Timer 1 high. VMD-86</summary>
    T1H = 0x11d,

    /// <summary>Timer 1 high reload data. VMD-86</summary>
    T1Hr = 0x9998,

    /// <summary>Mode control register. VMD-127</summary>
    Mcr = 0x120,

    /// <summary>Start address register. VMD-129</summary>
    Stad = 0x122,

    /// <summary>Character count register. VMD-130</summary>
    Cnr = 0x123,

    /// <summary>Time division register. VMD-130</summary>
    Tdr = 0x124,

    /// <summary>Bank address register. VMD-130</summary>
    Xbnk = 0x125,

    /// <summary>LCD contrast control register. VMD-131</summary>
    Vccr = 0x127,

    /// <summary>SIO0 control register. VMD-108</summary>
    Scon0 = 0x130,

    /// <summary>SIO0 buffer. VMD-113</summary>
    Sbuf0 = 0x131,

    /// <summary>SIO0 baud rate generator. VMD-113</summary>
    Sbr = 0x132,

    /// <summary>SIO1 control register. VMD-111</summary>
    Scon1 = 0x134,

    /// <summary>SIO1 buffer. VMD-113</summary>
    Sbuf1 = 0x135,

    /// <summary>Port 1 latch. VMD-58</summary>
    P1 = 0x144,

    /// <summary>Port 1 data direction register. VMD-58</summary>
    P1Ddr = 0x145,

    /// <summary>Port 1 function control register. VMD-59</summary>
    P1Fcr = 0x146,

    /// <summary>Port 3 latch. VMD-54</summary>
    P3 = 0x14C,

    /// <summary>Port 3 data direction register. VMD-62</summary>
    P3Ddr = 0x14D,

    /// <summary>Port 3 interrupt function control register. VMD-62</summary>
    P3Int = 0x14E,

    /// <summary>Flash Program Register. Undocumented.</summary>
    FPR = 0x154,

    /// <summary>Port 7 latch. VMD-64</summary>
    P7 = 0x15C,

    /// <summary>External interrupt 0, 1 control. VMD-135</summary>
    I01Cr = 0x15D,

    /// <summary>External interrupt 2, 3 control. VMD-137</summary>
    I23Cr = 0x15E,

    /// <summary>Input signal select. VMD-138</summary>
    Isl = 0x15F,

    /// <summary>Control register. VMD-143</summary>
    Vsel = 0x163,

    /// <summary>System address register 1. VMD-144</summary>
    Vrmad1 = 0x164,

    /// <summary>System address register 2. VMD-144</summary>
    Vrmad2 = 0x165,

    /// <summary>Work RAM access (Send/receive buffer). VMD-144</summary>
    Vtrbf = 0x166,

    /// <summary>Base timer control. VMD-101</summary>
    Btcr = 0x17F,
}

public static class SpecialFunctionRegisterKindExtensions
{
    public static byte Suffix(this SpecialFunctionRegisterKind kind)
    {
        return unchecked((byte)kind);
    }
}