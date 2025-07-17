using System;
using System.Diagnostics;

namespace DreamPotato.Core;

/// <summary>
/// Manages main RAM, SFRs, XRAM and Work RAM.
/// Doesn't manage ROM/flash.
/// </summary>
public class Memory
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

    internal const int XramBank01Size = 0x80;
    internal const int XramBank2Size = 6;

    /// <summary>
    /// Video memory. Bank 0, 0x180-0x1ff.
    /// Note that 0x20 of the bytes are "dead", only 0x60 of the space is usable.
    /// </summary>
    private readonly byte[] _xram0 = new byte[XramBank01Size];

    /// <summary>
    /// Video memory. Bank 1, 0x180-0x1ff.
    /// Note that 0x20 of the bytes are "dead", only 0x60 of the space is usable.
    /// </summary>
    private readonly byte[] _xram1 = new byte[XramBank01Size];

    /// <summary>
    /// Video memory. Bank 2, 0x180-0x186.
    /// Icons, only modifiable by system.
    /// </summary>
    private readonly byte[] _xram2 = new byte[XramBank2Size];

    public const int WorkRamSize = 0x200;
    private readonly byte[] _workRam = new byte[WorkRamSize];

    private readonly Logger _logger;
    public Memory(Cpu cpu, Logger logger)
    {
        SFRs = new SpecialFunctionRegisters(cpu, workRam: _workRam, logger);
        _logger = logger;
    }

    public void Reset()
    {
        Array.Clear(_mainRam0);
        Array.Clear(_mainRam1);
        SFRs.Reset();
        Array.Clear(_xram0);
        Array.Clear(_xram1);
        Array.Clear(_xram2);
        Array.Clear(_workRam);
    }

    internal void SaveState(Stream writeStream)
    {
        writeStream.Write(_mainRam0);
        writeStream.Write(_mainRam1);
        SFRs.SaveState(writeStream);
        writeStream.Write(_xram0);
        writeStream.Write(_xram1);
        writeStream.Write(_xram2);
        writeStream.Write(_workRam);
    }

    internal void LoadState(Stream readStream)
    {
        readStream.ReadExactly(_mainRam0);
        readStream.ReadExactly(_mainRam1);
        SFRs.LoadState(readStream);
        readStream.ReadExactly(_xram0);
        readStream.ReadExactly(_xram1);
        readStream.ReadExactly(_xram2);
        readStream.ReadExactly(_workRam);
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
            case >= 0x180 and < 0x200:
                return ReadXram((byte)(address - 0x180));
            default:
                _logger.LogDebug($"Read out of range: 0x{address:X}");
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
            case >= 0x180 and < 0x200:
                WriteXram((byte)(address - 0x180), value);
                return;
            default:
                _logger.LogDebug($"Write out of range: 0x{address:X}");
                return;
        }
    }

    public const byte StackStart = 0x80;

    public void PushStack(byte value)
    {
        // Stack is always bank 0, 0x90-0xff; do not use the same routines as for memory access from user code.
        // Stack also points to last element, rather than pointing past last element. So it is inc'd before writing.
        if (SFRs.Sp + 1 is not (>= StackStart and <= 0xff))
        {
            _logger.LogError($"Stack pointer (0x{SFRs.Sp:X2}) outside of expected range (0x80-0xff)!");
        }

        SFRs.Sp++;
        _mainRam0[SFRs.Sp] = value;
    }

    public byte PopStack()
    {
        // Stack is always bank 0, 0x90-0xff; do not use the same routines as for memory access from user code.
        // Stack also points to last element, rather than pointing past last element. So it is dec'd after reading.
        if (SFRs.Sp is not (>= StackStart and <= 0xff))
        {
            _logger.LogError($"Stack pointer (0x{SFRs.Sp:X2}) outside of expected range (0x80-0xff)!");
        }

        var value = _mainRam0[SFRs.Sp];
        SFRs.Sp--;
        return value;
    }

    public byte ReadIndirect(int regId)
    {
        var address = ReadIndirectAddressRegister(regId);
        var value = Read(address);
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

        var irbk = (byte)SFRs.Psw & 0b11000; // Mask out IRBK1, IRBK0 bits (VMD-44).
        var registerAddress = (ushort)((irbk >> 1) | regId); // compose (IRBK1, IRBK0, j1, j0)
        Debug.Assert(registerAddress is >= 0 and < 16);

        // 9-bit address, where the 8th bit is j1 from instruction data (indicating the result address is in range 0x100-1x1ff)
        var bit8 = (regId & 0b10) == 0b10 ? 0x100 : 0;
        // TODO: confirm whether each main mem bank has own set of IARs, or if only bank 0 has them
        var address = (ushort)(bit8 | Read(registerAddress));
        return address;
    }

    // Direct_ functions sidestep hardware behaviors such as auto-increment VTRBF.
    // Do not use these to execute VMU code, they should be used for testing, front-ends etc only.

    /// <summary>
    /// Use only to initialize work RAM state for testing
    /// </summary>
    internal void Direct_WriteWorkRam(int address, byte value)
    {
        Debug.Assert(address < 0x200);
        _workRam[address] = value;
    }

    internal Span<byte> Direct_AccessWorkRam()
    {
        return _workRam;
    }

    /// <summary>
    /// Do not use when executing user code; use for implementing front-end or direct Maple handling only.
    /// </summary>
    internal Span<byte> Direct_AccessXram0() => _xram0;

    /// <inheritdoc cref="Direct_AccessXram0"/>
    internal Span<byte> Direct_AccessXram1() => _xram1;

    /// <inheritdoc cref="Direct_AccessXram0"/>
    internal Span<byte> Direct_AccessXram2() => _xram2;

    private byte ReadMainMemory(ushort address)
    {
        Debug.Assert(address < 0x100);
        var bank = SFRs.Psw.Rambk0 ? _mainRam1 : _mainRam0;
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
        }

        _logger.LogDebug($"Reading from nonexistent XRAM bank {SFRs.Xbnk}! Address: 0x{address:X}");
        return 0xff;

        byte readMainXram(byte[] bank, ushort address)
        {
            if ((address & 0xf) is >= 0xc and <= 0xf)
            {
                _logger.LogDebug($"Reading skipped XRAM {address:X}!");
                return 0xff;
            }

            return bank[address];
        }

        byte readIconXram(byte[] bank, ushort address)
        {
            if (address > 0xf)
            {
                _logger.LogDebug($"Read out of range of icon XRAM! Address: 0x{address:X}");
                // TODO: There is weird undocumented behavior around what this range does.
                return 0xff;
            }

            if ((address & 0xf) is >= 0xc and <= 0xf)
            {
                _logger.LogDebug($"Reading skipped XRAM 0x{address:X}!");
                return 0xff;
            }

            return bank[address];
        }
    }

    private void WriteMainMemory(ushort address, byte value)
    {
        Debug.Assert(address < 0x100);
        var bank = SFRs.Psw.Rambk0 ? _mainRam1 : _mainRam0;
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
        }

        _logger.LogDebug($"Writing to nonexistent XRAM bank {SFRs.Xbnk}! Address: 0x{address:X}");
        return;

        void writeMainXram(byte[] bank, ushort address, byte value)
        {
            if ((address & 0xf) is >= 0xc and <= 0xf)
            {
                _logger.LogDebug($"Writing skipped XRAM {SFRs.Xbnk} {address:X}!");
                return;
            }

            bank[address] = value;
        }

        void writeIconXram(byte[] bank, ushort address, byte value)
        {
            if (address is not (>= 0 and <= 5))
            {
                _logger.LogDebug($"Write out of range of icon XRAM {SFRs.Xbnk}! Address: 0x{address:X}");
                // There is weird undocumented behavior around what this range does.
                // Strictly, we should probably enable free reads and writes wherever they work on real hardware.
                return;
            }

            bank[address] = value;
        }
    }
}