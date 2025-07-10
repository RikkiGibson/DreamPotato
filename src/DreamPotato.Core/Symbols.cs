namespace DreamPotato.Core;

static class BuiltInCodeSymbols
{
    internal const ushort BIOSWriteFlash = 0x100;
    internal const ushort BIOSVerifyFlash = 0x110;
    internal const ushort BIOSExit = 0x1f0;
    internal const ushort BIOSReadFlash = 0x120;
    internal const ushort BIOSClockTick = 0x130;

    internal const ushort BIOSAfterDateIsSet = 0x2e1;
}

static class BuiltInRamSymbols
{
    /// <summary>The larger two digits of the year</summary>
    internal const ushort DateTime_Century_Bcd = 0x10;

    /// <summary>The smaller two digits of the year</summary>
    internal const ushort DateTime_Year_Bcd = 0x11;

    internal const ushort DateTime_Month_Bcd = 0x12;
    internal const ushort DateTime_Day_Bcd = 0x13;
    internal const ushort DateTime_Hour_Bcd = 0x14;
    internal const ushort DateTime_Minute_Bcd = 0x15;
    internal const ushort DateTime_Second_Bcd = 0x16;

    /// <summary>Most significant byte of the 16-bit year</summary>
    internal const ushort DateTime_Year_Msb = 0x17;

    /// <summary>Least significant byte of the 16-bit year</summary>
    internal const ushort DateTime_Year_Lsb = 0x18;
    internal const ushort DateTime_Month = 0x19;
    internal const ushort DateTime_Day = 0x1a;
    internal const ushort DateTime_Hour = 0x1b;
    internal const ushort DateTime_Minute = 0x1c;
    internal const ushort DateTime_Second = 0x1d;
    internal const ushort DateTime_HalfSecond = 0x1e;
    internal const ushort DateTime_LeapYear = 0x1f;

    /// <summary>0xff if the date is set, 0 if the date is not set.</summary>
    internal const ushort DateTime_DateSet = 0xff;
}