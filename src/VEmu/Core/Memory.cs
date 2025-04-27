using System.Diagnostics;

namespace VEmu.Core;

/// <summary>
/// Manages main RAM, SFRs, XRAM and Work RAM.
/// Doesn't manage ROM/flash.
/// </summary>
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

    internal readonly SpecialFunctionRegisters SFRs;

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

    private readonly byte[] _workRam = new byte[0x200];

    public Memory()
    {
        SFRs = new SpecialFunctionRegisters(workRam: _workRam);
    }

    public byte Read(ushort address)
    {
        Debug.Assert(address < 0x200);
        switch (address)
        {
            case >= 0 and < 0x100:
                return ReadMainMemory(address);
            case >= 0x100 and < 0x180:
                return SFRs.Read((byte)(address - 0x100));
            case >= 0x180 and < 0x1B0:
                return ReadXram((byte)(address - 0x180));
            default:
                // TODO: log warning: read out of range
                return 0xff;
        }
    }

    public void Write(ushort address, byte value)
    {
        Debug.Assert(address < 0x200);
        switch (address)
        {
            case >= 0 and < 0x100:
                WriteMainMemory(address, value);
                return;
            case >= 0x100 and < 0x180:
                SFRs.Write((byte)(address - 0x100), value);
                return;
            case >= 0x180 and < 0x1B0:
                WriteXram((byte)(address - 0x180), value);
                return;
            default:
                // TODO: log warning: write out of range
                return;
        }
    }

    public void PushStack(byte value)
    {
        // Stack is always bank 0, 0x90-0xff; do not use the same routines as for memory access from user code.
        // Stack also points to last element, rather than pointing past last element. So it is inc'd before writing.
        if (SFRs.Sp + 1 is not (>= 0x90 and <= 0xff))
        {
            // TODO: log fatal: invalid stack pointer
            //throw new InvalidOperationException($"Stack pointer (0x{SFRs.Sp:X2}) outside of expected range (0x90-0xff).");
        }

        SFRs.Sp++;
        _mainRam0[SFRs.Sp] = value;
    }

    public byte PopStack()
    {
        // Stack is always bank 0, 0x90-0xff; do not use the same routines as for memory access from user code.
        // Stack also points to last element, rather than pointing past last element. So it is dec'd after reading.
        if (SFRs.Sp - 1 is not (>= 0x90 and <= 0xff))
        {
            // TODO: log fatal: invalid stack pointer
            //throw new InvalidOperationException($"Stack pointer (0x{SFRs.Sp:X4}) outside of expected range (0x90-0xff).");
        }

        var value = _mainRam0[SFRs.Sp];
        SFRs.Sp--;
        return value;
    }

    /// <summary>
    /// Get the address stored in an indirect address register R0-R3.
    /// Address is offset based on regId, so R2-3 already have 0x100 OR'd in.
    /// </summary>
    public ushort ReadIndirectAddressRegister(int regId)
    {
        Debug.Assert(regId is >= 0 and < 4);

        // There are 16 indirect registers, each 1 byte in size.
        // - bit 3: IRBK1
        // - bit 2: IRBK0
        // - bit 1: j1 (regId, from instruction data)
        // - bit 0: j0 (regId, from instruction data)

        var irbk = SFRs.Psw & 0b11000; // Mask out IRBK1, IRBK0 bits (VMD-44).
        var registerAddress = (ushort)((irbk >> 1) | regId); // compose (IRBK1, IRBK0, j1, j0)
        Debug.Assert(registerAddress is >= 0 and < 16);

        // 9-bit address, where the 8th bit is j1 from instruction data (indicating the result address is in range 0x100-1x1ff)
        var bit8 = (regId & 0b10) == 0b10 ? 0x100 : 0;
        // TODO: confirm whether each main mem bank has own set of IARs, or if only bank 0 has them
        var address = (ushort)(bit8 | Read(registerAddress));
        return address;
    }

    /// <summary>
    /// Use only to initialize work RAM state for testing
    /// </summary>
    public void Direct_WriteWorkRam(int address, byte value)
    {
        Debug.Assert(address < 0x200);
        _workRam[address] = value;
    }

    private byte ReadMainMemory(ushort address)
    {
        Debug.Assert(address < 0x100);
        var bank = SFRs.Rambk0 ? _mainRam1 : _mainRam0;
        return bank[address];
    }

    private byte ReadXram(ushort address)
    {
        Debug.Assert(address < 0x100);
        switch (SFRs.Xbnk)
        {
            case 0:
                return readMainXram(_xram0, address);
            case 1:
                return readMainXram(_xram1, address);
            case 2:
                return readIconXram(_xram2, address);
            case 3:
                // TODO log warning: reading nonexistent bank
                return 0xff;
        }

        // TODO: log warning: Xbnk out of range
        return 0xff;

        byte readMainXram(byte[] bank, ushort address)
        {
            if ((address & 0xf) is >= 0xc and <= 0xf)
            {
                // TODO: log warning: reading skipped bytes
                return 0xff;
            }

            return bank[address];
        }

        byte readIconXram(byte[] bank, ushort address)
        {
            if (address > 0xf)
            {
                // TODO: log warning: read out of range
                // There is weird undocumented behavior around what this range does.
                return 0xff;
            }

            if ((address & 0xf) is >= 0xc and <= 0xf)
            {
                // TODO: log warning: reading skipped bytes
                return 0xff;
            }

            return bank[address];
        }
    }

    private void WriteMainMemory(ushort address, byte value)
    {
        Debug.Assert(address < 0x100);
        var bank = SFRs.Rambk0 ? _mainRam1 : _mainRam0;
        bank[address] = value;
    }

    private void WriteXram(ushort address, byte value)
    {
        Debug.Assert(address < 0x100);
        switch (SFRs.Xbnk)
        {
            case 0:
                writeMainXram(_xram0, address, value);
                return;
            case 1:
                writeMainXram(_xram1, address, value);
                return;
            case 2:
                writeIconXram(_xram2, address, value);
                return;
            case 3:
                // TODO log warning: writing nonexistent bank
                return;
        }

        // TODO: log warning: Xbnk out of range
        return;

        void writeMainXram(byte[] bank, ushort address, byte value)
        {
            if ((address & 0xf) is >= 0xc and <= 0xf)
            {
                // TODO: log warning: write skipped bytes
                return;
            }

            bank[address] = value;
        }

        void writeIconXram(byte[] bank, ushort address, byte value)
        {
            if (address > 0xf)
            {
                // TODO: log warning: write out of range
                // There is weird undocumented behavior around what this range does.
                return;
            }

            if ((address & 0xf) is >= 0xc and <= 0xf)
            {
                // TODO: log warning: reading skipped bytes
                return;
            }

            bank[address] = value;
        }
    }
}