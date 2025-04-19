namespace VEmu.Core;

/// <summary>See VMD-40, table 2.6</summary>
public struct SpecialFunctionRegisters(byte[] RamBank0)
{
    /// <summary>Accumulator. VMD-50</summary>
    public ref byte Acc => ref RamBank0[0x100];
    /// <summary>Program status word. VMD-52</summary>
    public ref byte Psw => ref RamBank0[0x101];

    /// <summary>B register. VMD-51</summary>
    public ref byte B => ref RamBank0[0x102];
    /// <summary>C register. VMD-51</summary>
    public ref byte C => ref RamBank0[0x103];

    /// <summary>Table reference register lower byte. VMD-54</summary>
    public ref byte Trl => ref RamBank0[0x104];
    /// <summary>Table reference register upper byte. VMD-54</summary>
    public ref byte Trh => ref RamBank0[0x105];

    /// <summary>Stack pointer. VMD-53</summary>
    public ref byte Sp => ref RamBank0[0x106];

    /// <summary>Power control register. VMD-158</summary>
    public ref byte Pcon => ref RamBank0[0x107];

    /// <summary>Master interrupt enable control register. VMD-138</summary>
    public ref byte Ie => ref RamBank0[0x108];

    /// <summary>Interrupt priority control register. VMD-151</summary>
    public ref byte Ip => ref RamBank0[0x109];

    /// <summary>External memory control register. No VMD page</summary>
    public ref byte Ext => ref RamBank0[0x10d];

    /// <summary>Oscillation control register. VMD-156</summary>
    public ref byte Ocr => ref RamBank0[0x10e];

    /// <summary>Timer 0 control register. VMD-67</summary>
    public ref byte T0Cnt => ref RamBank0[0x110];

    /// <summary>Timer 0 prescaler data. VMD-71</summary>
    public ref byte T0Prr => ref RamBank0[0x111];

    /// <summary>Timer 0 low. VMD-71</summary>
    public ref byte T0L => ref RamBank0[0x112];

    /// <summary>Timer 0 low reload data. VMD-71</summary>
    public ref byte T0Lr => ref RamBank0[0x113];

    /// <summary>Timer 0 high. VMD-72</summary>
    public ref byte T0H => ref RamBank0[0x114];

    /// <summary>Timer 0 high reload data. VMD-72</summary>
    public ref byte T0Hr => ref RamBank0[0x115];

    /// <summary>Timer 1 control register. VMD-83</summary>
    public ref byte T1Cnt => ref RamBank0[0x118];

    /// <summary>Timer 1 low comparison data. VMD-86</summary>
    public ref byte T1Lc => ref RamBank0[0x11a];

    /// <summary>Timer 1 low. VMD-85</summary>
    public ref byte T1L => ref RamBank0[0x11b];

    /// <summary>Timer 1 low reload data. VMD-85</summary>
    public ref byte T1Lr => throw new NotImplementedException(); //ref RamBank0[0x???];

    /// <summary>Timer 1 high comparison data. VMD-87</summary>
    public ref byte T1Hc => ref RamBank0[0x11c];

    /// <summary>Timer 1 high. VMD-86</summary>
    public ref byte T1H => ref RamBank0[0x11d];

    /// <summary>Timer 1 high reload data. VMD-86</summary>
    public ref byte T1Hr => throw new NotImplementedException(); //ref RamBank0[0x];

    /// <summary>Mode control register. VMD-127</summary>
    public ref byte Mcr => ref RamBank0[0x120];

    /// <summary>Start address register. VMD-129</summary>
    public ref byte Stad => ref RamBank0[0x122];

    /// <summary>Character count register. VMD-130</summary>
    public ref byte Cnr => ref RamBank0[0x123];

    /// <summary>Time division register. VMD-130</summary>
    public ref byte Tdr => ref RamBank0[0x124];

    /// <summary>Bank address register. VMD-130</summary>
    public ref byte Xbnk => ref RamBank0[0x125];

    /// <summary>LCD contrast control register. VMD-131</summary>
    public ref byte Vccr => ref RamBank0[0x127];

    /// <summary>SIO0 control register. VMD-108</summary>
    public ref byte Scon0 => ref RamBank0[0x130];

    /// <summary>SIO0 buffer. VMD-113</summary>
    public ref byte Sbuf0 => ref RamBank0[0x131];

    /// <summary>SIO0 baud rate generator. VMD-113</summary>
    public ref byte Sbr => ref RamBank0[0x132];

    /// <summary>SIO1 control register. VMD-111</summary>
    public ref byte Scon1 => ref RamBank0[0x134];

    /// <summary>SIO1 buffer. VMD-113</summary>
    public ref byte Sbuf1 => ref RamBank0[0x135];

    /// <summary>Port 1 latch. VMD-58</summary>
    public ref byte P1 => ref RamBank0[0x144];

    /// <summary>Port 1 data direction register. VMD-58</summary>
    public ref byte P1Ddr => ref RamBank0[0x145];

    /// <summary>Port 1 function control register. VMD-59</summary>
    public ref byte P1Fcr => ref RamBank0[0x146];

    /// <summary>Port 3 data direction register. VMD-62</summary>
    public ref byte P3Ddr => ref RamBank0[0x14D];

    /// <summary>Port 3 interrupt function control register. VMD-62</summary>
    public ref byte P3Int => ref RamBank0[0x14E];

    /// <summary>Port 7 latch. VMD-64</summary>
    public ref byte P7 => ref RamBank0[0x15C];

    /// <summary>External interrupt 0, 1 control. VMD-135</summary>
    public ref byte I01Cr => ref RamBank0[0x15D];

    /// <summary>External interrupt 2, 3 control. VMD-137</summary>
    public ref byte I23Cr => ref RamBank0[0x15E];

    /// <summary>Input signal select. VMD-138</summary>
    public ref byte Isl => ref RamBank0[0x15F];

    /// <summary>Control register. VMD-143</summary>
    public ref byte Vsel => ref RamBank0[0x163];

    /// <summary>System address register 1. VMD-144</summary>
    public ref byte Vrmad1 => ref RamBank0[0x164];

    /// <summary>System address register 2. VMD-144</summary>
    public ref byte Vrmad2 => ref RamBank0[0x165];

    /// <summary>Send/receive buffer. VMD-144</summary>
    public ref byte Vtrbf => ref RamBank0[0x166];

    /// <summary>Base timer control. VMD-101</summary>
    public ref byte Btcr => ref RamBank0[0x17F];
}