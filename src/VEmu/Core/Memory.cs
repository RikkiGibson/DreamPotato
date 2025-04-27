namespace VEmu.Core;

class Memory
{
    // Memory map:
    // - Bank 0
    // 0x00-0xff: main memory
    //    0x00-0x8f: System RAM
    //    0x90-0xff: Stack area
    // 0x100-0x17f: SFRs
    // 0x180-0x1bf: XRAM

    // - Bank 1
    // 0x00-0xff: main memory (application RAM)
    // 0x100-0xa7f: UNUSED
    // 0x180-0x1bf: XRAM

    // - Bank 2
    // 0x00-0x17f: UNUSED
    // 0x180-0x18f: XRAM (icons, only modifiable by system)
    // 0x190-0x1bf: UNUSED

    /// <summary>
    /// Main memory. Bank 0, 0x00-0xff.
    /// - 0x00-0x8f: System RAM
    /// - 0x90-0xff: Stack area
    /// </summary>
    private readonly byte[] _mainRam0 = new byte[0x100];

    /// <summary>
    /// Main memory. Bank 1, 0x00-0xff.
    /// User/application RAM.
    /// </summary>
    private readonly byte[] _mainRam1 = new byte[0x100];

    private readonly byte[] _sfrs = new byte[0x80];

    /// <summary>
    /// Video memory. Bank 0, 0x180-0x1bf.
    /// </summary>
    private readonly byte[] _xram0 = new byte[0x40];

    /// <summary>
    /// Video memory. Bank 1, 0x180-0x1bf.
    /// </summary>
    private readonly byte[] _xram1 = new byte[0x40];

    /// <summary>
    /// Video memory. Bank 2, 0x180-0x18f.
    /// Icons, only modifiable by system.
    /// </summary>
    private readonly byte[] _xram2 = new byte[0x10];

    public byte ReadMemory(ushort address)
    {
        return 0;
    }
}