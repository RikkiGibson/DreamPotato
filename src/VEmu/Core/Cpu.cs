using System.Diagnostics;

namespace VEmu.Core;

public class Cpu
{
    // TODO: bring in a logger type with configurable severity etc
    // perhaps with a rolling buffer
    internal Logger Logger { get; }

    // VMU-2: standalone instruction cycle time 183us (microseconds).
    // Compare with 1us when connected to console.

    // VMD-35: Accumulator and all registers are mapped to RAM.

    // VMD-38: Memory
    //

    /// <summary>Read-only memory space.</summary>
    internal readonly byte[] ROM = new byte[64 * 1024];
    internal readonly byte[] FlashBank0 = new byte[64 * 1024];
    internal readonly byte[] FlashBank1 = new byte[64 * 1024];

    internal readonly InstructionMap InstructionMap = new();

    /// <summary>
    /// May point to either ROM (BIOS), flash memory bank 0 or bank 1.
    /// </summary>
    /// <remarks>
    /// Note that we need an extra bit of state here. We can't just look at the value of <see cref="SpecialFunctionRegisters.Ext"/>.
    /// The bank is only actually switched when using a jmpf instruction.
    /// </remarks>
    internal byte[] CurrentROMBank;

    internal readonly Memory Memory;

    internal ushort Pc;

    internal Interrupts Interrupts;

    internal InterruptServicingState InterruptServicingState;

    public Cpu()
    {
        CurrentROMBank = ROM;
        Logger = new Logger(LogLevel.Trace, this);
        Memory = new Memory(this, Logger);
    }

    public void Reset()
    {
        // TODO: possibly we should setup SFR initial values during construction, unless a 'noInitialize' flag is set in constructor
        CurrentROMBank = ROM;
        Pc = 0;
        Interrupts = 0;
        InterruptServicingState = InterruptServicingState.Ready;
        Memory.Reset();
    }

    public byte ReadRam(int address)
    {
        Debug.Assert(address < 0x200);
        return Memory.Read((ushort)address);
    }

    public void WriteRam(int address, byte value)
    {
        Debug.Assert(address < 0x200);
        Memory.Write((ushort)address, value);
    }

    internal SpecialFunctionRegisters SFRs => Memory.SFRs;

    internal int Run(int cyclesToRun)
    {
        // TODO: each step, we need to check for halt mode, tick timers, and check for interrupts
        // I think setting some SFR values needs to be able to trigger interrupts as well

        int cyclesSoFar = 0;
        while (cyclesSoFar < cyclesToRun)
        {
            if (BitHelpers.ReadBit(SFRs.Pcon, bit: 0))
            {
                Logger.LogDebug("---HALT---");
            }

            cyclesSoFar += Step();
        }
        return cyclesSoFar;
    }

#region External interrupt triggers
    /// <summary>
    /// Simulate connecting the VMU to a Dreamcast
    /// </summary>
    internal void ConnectDreamcast()
    {
        SFRs.P70 = true;
        Interrupts |= Interrupts.INT0;
    }

    /// <summary>
    /// Simulate low voltage
    /// </summary>
    internal void ReportLowVoltage()
    {
        SFRs.P71 = true;
        Interrupts |= Interrupts.INT1;
    }
#endregion

    private void ServiceInterruptIfNeeded()
    {
        if (InterruptServicingState != InterruptServicingState.Ready)
            return;

        if (!SFRs.Ie7_MasterInterruptEnable)
            return;

        // TODO: check interrupt enable etc
        // TODO: check interrupt priority bits
        if (Interrupts != 0)
        {
            // service next enabled interrupt in priority order.
            if ((Interrupts & Interrupts.INT0) != 0 && SFRs.I01Cr0_Enable0)
            {
                callServiceRoutine(InterruptVectors.INT0);
                Interrupts &= ~Interrupts.INT0;
            }
            else if ((Interrupts & Interrupts.INT1) != 0 && SFRs.I01Cr4_Enable1)
            {
                callServiceRoutine(InterruptVectors.INT1);
                Interrupts &= ~Interrupts.INT1;
            }
        }

        void callServiceRoutine(ushort routineAddress)
        {
            InterruptServicingState = InterruptServicingState.Servicing;
            Memory.PushStack((byte)Pc);
            Memory.PushStack((byte)(Pc >> 8));
            Pc = routineAddress;
        }
    }

    private void AdvanceInterruptState()
    {
        if (InterruptServicingState == InterruptServicingState.Returned)
            InterruptServicingState = InterruptServicingState.Ready;
    }

    /// <returns>Number of cycles consumed by the instruction.</returns>
    internal int Step()
    {
        ServiceInterruptIfNeeded();
        AdvanceInterruptState();

        var inst = InstructionDecoder.Decode(CurrentROMBank, Pc);
        InstructionMap[Pc] = inst;
        Logger.LogTrace($"{inst} Acc={SFRs.Acc:X} B={SFRs.B:X} C={SFRs.C:X} R2={ReadRam(2)}");

        switch (inst.Kind)
        {
            case OperationKind.ADD: Op_ADD(inst); break;
            case OperationKind.ADDC: Op_ADDC(inst); break;
            case OperationKind.SUB: Op_SUB(inst); break;
            case OperationKind.SUBC: Op_SUBC(inst); break;
            case OperationKind.INC: Op_INC(inst); break;
            case OperationKind.DEC: Op_DEC(inst); break;
            case OperationKind.MUL: Op_MUL(inst); break;
            case OperationKind.DIV: Op_DIV(inst); break;
            case OperationKind.AND: Op_AND(inst); break;
            case OperationKind.OR: Op_OR(inst); break;
            case OperationKind.XOR: Op_XOR(inst); break;
            case OperationKind.ROL: Op_ROL(inst); break;
            case OperationKind.ROLC: Op_ROLC(inst); break;
            case OperationKind.ROR: Op_ROR(inst); break;
            case OperationKind.RORC: Op_RORC(inst); break;
            case OperationKind.LD: Op_LD(inst); break;
            case OperationKind.ST: Op_ST(inst); break;
            case OperationKind.MOV: Op_MOV(inst); break;
            case OperationKind.LDC: Op_LDC(inst); break;
            case OperationKind.PUSH: Op_PUSH(inst); break;
            case OperationKind.POP: Op_POP(inst); break;
            case OperationKind.XCH: Op_XCH(inst); break;
            case OperationKind.JMP: Op_JMP(inst); break;
            case OperationKind.JMPF: Op_JMPF(inst); break;
            case OperationKind.BR: Op_BR(inst); break;
            case OperationKind.BRF: Op_BRF(inst); break;
            case OperationKind.BZ: Op_BZ(inst); break;
            case OperationKind.BNZ: Op_BNZ(inst); break;
            case OperationKind.BP: Op_BP(inst); break;
            case OperationKind.BPC: Op_BPC(inst); break;
            case OperationKind.BN: Op_BN(inst); break;
            case OperationKind.DBNZ: Op_DBNZ(inst); break;
            case OperationKind.BE: Op_BE(inst); break;
            case OperationKind.BNE: Op_BNE(inst); break;
            case OperationKind.CALL: Op_CALL(inst); break;
            case OperationKind.CALLF: Op_CALLF(inst); break;
            case OperationKind.CALLR: Op_CALLR(inst); break;
            case OperationKind.RET: Op_RET(inst); break;
            case OperationKind.RETI: Op_RETI(inst); break;
            case OperationKind.CLR1: Op_CLR1(inst); break;
            case OperationKind.SET1: Op_SET1(inst); break;
            case OperationKind.NOT1: Op_NOT1(inst); break;
            case OperationKind.LDF: Op_LDF(inst); break;
            case OperationKind.NOP: Op_NOP(inst); break;
            default: Throw(inst); break;
        }

        return inst.Cycles;

        static void Throw(Instruction inst) => throw new InvalidOperationException($"Unknown operation '{inst}'");
    }

    private byte FetchOperand(Parameter param, ushort arg)
    {
        return param.Kind switch
        {
            ParameterKind.I8 => (byte)arg,
            ParameterKind.D9 => Memory.Read(arg),
            ParameterKind.Ri => Memory.ReadIndirect(arg),
            _ => Throw()
        };

        byte Throw() => throw new InvalidOperationException($"Cannot fetch operand for parameter '{param}'");
    }

    private ushort GetOperandAddress(Parameter param, ushort arg)
    {
        return param.Kind switch
        {
            ParameterKind.D9 => arg,
            ParameterKind.Ri => Memory.ReadIndirectAddressRegister(arg),
            _ => Throw()
        };

        byte Throw() => throw new InvalidOperationException($"Cannot fetch address for parameter '{param}'");
    }

    private void Op_ADD(Instruction inst)
    {
        // ACC <- ACC + operand
        var rhs = FetchOperand(inst.Parameters[0], inst.Arg0);
        var lhs = SFRs.Acc;
        var result = (byte)(lhs + rhs);
        SFRs.Acc = result;

        SFRs.Cy = result < lhs;
        SFRs.Ac = (lhs & 0xf) + (rhs & 0xf) > 0xf;

        // Overflow occurs if either:
        // - both operands had MSB set (i.e. were two's complement negative), but the result has the MSB cleared.
        // - both operands had MSB cleared (i.e. were two's complement positive), but the result has the MSB set.
        SFRs.Ov = (BitHelpers.ReadBit(lhs, bit: 7), BitHelpers.ReadBit(rhs, bit: 7), BitHelpers.ReadBit(result, bit: 7)) switch
        {
            (true, true, false) => true,
            (false, false, true) => true,
            _ => false
        };

        Pc += inst.Size;
    }

    private void Op_ADDC(Instruction inst)
    {
        // ACC <- ACC + CY + operand
        var rhs = FetchOperand(inst.Parameters[0], inst.Arg0);
        var lhs = SFRs.Acc;
        var carry = SFRs.Cy ? 1 : 0;
        var result = (byte)(lhs + carry + rhs);
        SFRs.Acc = result;

        SFRs.Cy = result < lhs;
        SFRs.Ac = (lhs & 0xf) + carry + (rhs & 0xf) > 0xf;

        // Overflow occurs if either:
        // - both operands had MSB set (i.e. were two's complement negative), but the result has the MSB cleared.
        // - both operands had MSB cleared (i.e. were two's complement positive), but the result has the MSB set.
        SFRs.Ov = (BitHelpers.ReadBit((byte)(lhs + carry), bit: 7), BitHelpers.ReadBit(rhs, bit: 7), BitHelpers.ReadBit(result, bit: 7)) switch
        {
            (true, true, false) => true,
            (false, false, true) => true,
            _ => false
        };

        Pc += inst.Size;
    }

    private void Op_SUB(Instruction inst)
    {
        // ACC <- ACC - operand
        var rhs = FetchOperand(inst.Parameters[0], inst.Arg0);
        var lhs = SFRs.Acc;
        var result = (byte)(lhs - rhs);
        SFRs.Acc = result;

        SFRs.Cy = lhs < rhs;
        SFRs.Ac = (lhs & 0xf) < (rhs & 0xf);

        // Overflow occurs if either:
        // - first operand has MSB set (negative number), second operand has MSB cleared (positive number), and the result has the MSB cleared (positive number).
        // - first operand has MSB cleared (positive number), second operand has MSB set (negative number), and the result has the MSB set (negative number).
        SFRs.Ov = (BitHelpers.ReadBit(lhs, bit: 7), BitHelpers.ReadBit(rhs, bit: 7), BitHelpers.ReadBit(result, bit: 7)) switch
        {
            (true, false, false) => true,
            (false, true, true) => true,
            _ => false
        };

        Pc += inst.Size;
    }

    private void Op_SUBC(Instruction inst)
    {
        // ACC <- ACC - CY - operand
        var rhs = FetchOperand(inst.Parameters[0], inst.Arg0);
        var lhs = SFRs.Acc;
        var carry = SFRs.Cy ? 1 : 0;
        var result = (byte)(lhs - carry - rhs);
        SFRs.Acc = result;

        // Carry is set when the subtraction yields a negative result.
        SFRs.Cy = lhs - carry - rhs < 0;
        SFRs.Ac = (lhs & 0xf) - carry - (rhs & 0xf) < 0;

        // Overflow occurs if either:
        // - subtracting a negative changes the sign from negative to positive
        // - first operand has MSB set (negative number), second operand has MSB cleared (positive number), and the result has the MSB cleared (positive number).
        // - first operand has MSB cleared (positive number), second operand has MSB set (negative number), and the result has the MSB set (negative number).
        SFRs.Ov = (BitHelpers.ReadBit((byte)(lhs + carry), bit: 7), BitHelpers.ReadBit(rhs, bit: 7), BitHelpers.ReadBit(result, bit: 7)) switch
        {
            (true, false, false) => true,
            (false, true, true) => true,
            _ => false
        };

        Pc += inst.Size;
    }

    private void Op_INC(Instruction inst)
    {
        // (operand) <- (operand) + 1
        // (could be either direct or indirect)
        var address = GetOperandAddress(inst.Parameters[0], inst.Arg0);
        var operand = ReadRam(address);
        operand++;
        WriteRam(address, operand);
        Pc += inst.Size;
    }

    private void Op_DEC(Instruction inst)
    {
        // (operand) <- (operand) - 1
        // (could be either direct or indirect)
        var address = GetOperandAddress(inst.Parameters[0], inst.Arg0);
        var operand = ReadRam(address);
        operand--;
        WriteRam(address, operand);
        Pc += inst.Size;
    }

    private void Op_MUL(Instruction inst)
    {
        // TODO: interrupts enabled on the 7th cycle.
        // Have to consider when implementing interrupts.

        // (B) (ACC) (C) <- (ACC) (C) * (B)
        int result = (SFRs.Acc << 0x8 | SFRs.C) * SFRs.B;
        SFRs.B = (byte)(result >> 0x10); // Casting to byte just takes the 8 least significant bits of the expression
        SFRs.Acc = (byte)(result >> 0x8);
        SFRs.C = (byte)result;

        // Overflow cleared indicates the result can fit into 16 bits, i.e. B is 0.
        SFRs.Ov = SFRs.B != 0;
        SFRs.Cy = false;
        Pc += inst.Size;
    }

    private void Op_DIV(Instruction inst)
    {
        // (ACC) (C), mod(B) <- (ACC) (C) / (B)
        if (SFRs.B == 0)
        {
            SFRs.Acc = 0xff;
            SFRs.Ov = true;
        }
        else
        {
            int lhs = SFRs.Acc << 0x8 | SFRs.C;
            int result = lhs / SFRs.B;
            int mod = lhs % SFRs.B;

            SFRs.Acc = (byte)(result >> 0x8);
            SFRs.C = (byte)result;
            SFRs.B = (byte)mod;
            SFRs.Ov = false;
        }
        SFRs.Cy = false;
        Pc += inst.Size;
    }

    private void Op_AND(Instruction inst)
    {
        // ACC <- ACC & operand
        var rhs = FetchOperand(inst.Parameters[0], inst.Arg0);
        SFRs.Acc &= rhs;
        Pc += inst.Size;
    }


    private void Op_OR(Instruction inst)
    {
        // ACC <- ACC | operand
        var rhs = FetchOperand(inst.Parameters[0], inst.Arg0);
        SFRs.Acc |= rhs;
        Pc += inst.Size;
    }
    private void Op_XOR(Instruction inst)
    {
        // ACC <- ACC ^ operand
        var rhs = FetchOperand(inst.Parameters[0], inst.Arg0);
        SFRs.Acc ^= rhs;
        Pc += inst.Size;
    }

    private void Op_ROL(Instruction inst)
    {
        // <-A7<-A6<-A5<-A4<-A3<-A2<-A1<-A0
        int shifted = SFRs.Acc << 1;
        bool bit0 = (shifted & 0x100) != 0;
        SFRs.Acc = (byte)(shifted | (bit0 ? 1 : 0));
        Pc += inst.Size;
    }

    private void Op_ROLC(Instruction inst)
    {
        // <-A7<-A6<-A5<-A4<-A3<-A2<-A1<-A0<-CY<- (A7)
        int shifted = SFRs.Acc << 1 | (SFRs.Cy ? 1 : 0);
        SFRs.Cy = (shifted & 0x100) != 0;
        SFRs.Acc = (byte)shifted;
        Pc += inst.Size;
    }

    private void Op_ROR(Instruction inst)
    {
        // (A0) ->A7->A6->A5->A4->A3->A2->A1->A0
        bool bit7 = (SFRs.Acc & 1) != 0;
        SFRs.Acc = (byte)((SFRs.Acc >> 1) | (bit7 ? 0x80 : 0));
        Pc += inst.Size;
    }


    private void Op_RORC(Instruction inst)
    {
        // (A0) ->CY->A7->A6->A5->A4->A3->A2->A1->A0
        bool newCarry = (SFRs.Acc & 1) != 0;
        SFRs.Acc = (byte)((SFRs.Cy ? 0x80 : 0) | SFRs.Acc >> 1);
        SFRs.Cy = newCarry;
        Pc += inst.Size;
    }

    private void Op_LD(Instruction inst)
    {
        // (ACC) <- (d9)
        SFRs.Acc = FetchOperand(inst.Parameters[0], inst.Arg0);
        Pc += inst.Size;
    }

    private void Op_ST(Instruction inst)
    {
        // (d9) <- (ACC)
        var address = GetOperandAddress(inst.Parameters[0], inst.Arg0);
        WriteRam(address, SFRs.Acc);
        Pc += inst.Size;
    }

    private void Op_MOV(Instruction inst)
    {
        // MOV i8,d9
        // (d9) <- #i8
        // ((Ri)) <- #i8
        var i8 = (byte)inst.Arg0;
        var address = GetOperandAddress(inst.Parameters[1], inst.Arg1);
        WriteRam(address, i8);
        Pc += inst.Size;
    }

    private void Op_LDC(Instruction inst)
    {
        // (ACC) <- (BNK)((TRR) + (ACC)) [ROM]
        // For a program running in ROM, ROM is accessed.
        // For a program running in flash memory, bank 0 of flash memory is accessed.
        // TODO: Cannot access bank 1 of flash memory. System BIOS function must be used instead.
        var address = ((SFRs.Trh << 8) | SFRs.Trl) + SFRs.Acc;
        SFRs.Acc = CurrentROMBank[address];
        Pc += inst.Size;
    }

    private void Op_PUSH(Instruction inst)
    {
        // (SP) <- (SP) + 1, ((SP)) <- (d9)
        Memory.PushStack(FetchOperand(inst.Parameters[0], inst.Arg0));
        Pc += inst.Size;
    }

    private void Op_POP(Instruction inst)
    {
        // (d9) <- ((SP)), (SP) <- (SP) - 1
        var dAddress = GetOperandAddress(inst.Parameters[0], inst.Arg0);
        WriteRam(dAddress, Memory.PopStack());

        Pc += inst.Size;
    }

    private void Op_XCH(Instruction inst)
    {
        // (ACC) <--> (d9)
        // (ACC) <--> ((Rj)) j = 0, 1, 2, 3
        var address = GetOperandAddress(inst.Parameters[0], inst.Arg0);
        var temp = ReadRam(address);
        WriteRam(address, SFRs.Acc);
        SFRs.Acc = temp;

        Pc += inst.Size;
    }

    /// <summary>Jump near absolute address</summary>
    private void Op_JMP(Instruction inst)
    {
        // (PC) <- (PC) + 2, (PC11 to 00) <- a12
        ushort a12 = inst.Arg0;
        Pc += 2;
        Pc &= 0b1111_0000__0000_0000;
        Pc |= a12;
    }

    /// <summary>Jump far absolute address</summary>
    private void Op_JMPF(Instruction inst)
    {
        // (PC) <- a16
        Pc = inst.Arg0;
    }

    /// <summary>Branch near relative address</summary>
    private void Op_BR(Instruction inst)
    {
        // (PC) <- (PC) + 2, (PC) <- (PC) + r8
        var r8 = (sbyte)inst.Arg0;
        Pc = (ushort)(Pc + inst.Size + r8);
    }

    /// <summary>Branch far relative address</summary>
    private void Op_BRF(Instruction inst)
    {
        // (PC) <- (PC) + 3, (PC) <- (PC) - 1 + r16
        var r16 = inst.Arg0;
        Pc = (ushort)(Pc + inst.Size - 1 + r16);
    }

    /// <summary>Branch near relative address if accumulator is zero</summary>
    private void Op_BZ(Instruction inst)
    {
        // (PC) <- (PC) + 2, if (ACC) = 0 then (PC) <- PC + r8
        var r8 = (sbyte)inst.Arg0;
        var z = SFRs.Acc == 0;

        Pc += inst.Size;
        if (z)
            Pc = (ushort)(Pc + r8);
    }

    /// <summary>Branch near relative address if accumulator is not zero</summary>
    private void Op_BNZ(Instruction inst)
    {
        // (PC) <- (PC) + 2, if (ACC) != 0 then (PC) <- PC + r8
        var r8 = (sbyte)inst.Arg0;
        var nz = SFRs.Acc != 0;

        Pc += inst.Size;
        if (nz)
            Pc = (ushort)(Pc + r8);
    }

    /// <summary>Branch near relative address if direct bit is one ("positive")</summary>
    private void Op_BP(Instruction inst)
    {
        // (PC) <- (PC) + 3, if (d9, b3) = 1 then (PC) <- (PC) + r8
        var d9 = inst.Arg0;
        var b3 = (byte)inst.Arg1;
        var r8 = (sbyte)inst.Arg2;

        Pc += 3;
        if (BitHelpers.ReadBit(ReadRam(d9), b3))
            Pc = (ushort)(Pc + r8);
    }

    /// <summary>Branch near relative address if direct bit is one ("positive"), and clear</summary>
    private void Op_BPC(Instruction inst)
    {
        // When applied to port P1 and P3, the latch of each port is selected. The external signal is not selected.
        // When applied to port P7, there is no change in status.

        // (PC) <- (PC) + 3, if (d9, b3) = 1 then (PC) <- (PC) + r8, (d9, b3) = 0
        var d9 = inst.Arg0;
        var b3 = (byte)inst.Arg1;
        var r8 = (sbyte)inst.Arg2;

        var d_value = ReadRam(d9);
        var new_d_value = d_value;
        BitHelpers.WriteBit(ref new_d_value, bit: b3, value: false);

        Pc += inst.Size;
        if (d_value != new_d_value)
            Pc = (ushort)(Pc + r8);

        WriteRam(d9, new_d_value);
    }

    /// <summary>Branch near relative address if direct bit is zero ("negative")</summary>
    private void Op_BN(Instruction inst)
    {
        // (PC) <- (PC) + 3, if (d9, b3) = 0 then (PC) <- (PC) + r8
        var d9 = inst.Arg0;
        var b3 = (byte)inst.Arg1;
        var r8 = (sbyte)inst.Arg2;

        Pc += inst.Size;
        if (!BitHelpers.ReadBit(ReadRam(d9), b3))
            Pc = (ushort)(Pc + r8);
    }

    /// <summary>Decrement direct byte and branch near relative address if direct byte is nonzero</summary>
    private void Op_DBNZ(Instruction inst)
    {
        // (PC) <- (PC) + 3, (d9) = (d9)-1, if (d9) != 0 then (PC) <- (PC) + r8
        // (PC) <- (PC) + 3, ((Ri)) = ((Ri))-1, if ((Ri)) != 0 then (PC) <- (PC) + r8
        var address = GetOperandAddress(inst.Parameters[0], inst.Arg0);
        var value = ReadRam(address);
        var r8 = (sbyte)inst.Arg1;

        --value;
        WriteRam(address, value);

        Pc += inst.Size;
        if (value != 0)
            Pc = (ushort)(Pc + r8);
    }

    /// <summary>
    /// - Compare immediate data or direct byte to accumulator and branch near relative address if equal
    /// - Compare immediate data to indirect byte and branch near relative address if equal
    /// </summary>
    private void Op_BE(Instruction inst)
    {
        // (PC) <- (PC) + 3, if (ACC) == #i8 then (PC) <- (PC) + r8
        // (PC) <- (PC) + 3, if (ACC) == d9 then (PC) <- (PC) + r8
        // (PC) <- (PC) + 3, if ((Ri)) == #i8 then (PC) <- (PC) + r8
        var param0 = inst.Parameters[0];
        var indirectMode = param0.Kind == ParameterKind.Ri;
        var (lhs, rhs, r8) = indirectMode
            ? (lhs: Memory.ReadIndirect(inst.Arg0), rhs: inst.Arg1, r8: (sbyte)inst.Arg2)
            : (lhs: SFRs.Acc, rhs: FetchOperand(param0, inst.Arg0), r8: (sbyte)inst.Arg1);

        Pc += inst.Operation.Size;
        SFRs.Cy = lhs < rhs;
        if (lhs == rhs)
            Pc = (ushort)(Pc + r8);
    }

    /// <summary>
    /// - Compare immediate data or direct byte to accumulator and branch near relative address if not equal
    /// - Compare immediate data to indirect byte and branch near relative address if not equal
    /// </summary>
    private void Op_BNE(Instruction inst)
    {
        // (PC) <- (PC) + 3, if (ACC) != #i8 then (PC) <- (PC) + r8
        // (PC) <- (PC) + 3, if (ACC) != d9 then (PC) <- (PC) + r8
        // (PC) <- (PC) + 3, if ((Ri)) != #i8 then (PC) <- (PC) + r8
        var param0 = inst.Parameters[0];
        var indirectMode = param0.Kind == ParameterKind.Ri;
        var (lhs, rhs, r8) = indirectMode
            ? (lhs: Memory.ReadIndirect(inst.Arg0), rhs: inst.Arg1, r8: (sbyte)inst.Arg2)
            : (lhs: SFRs.Acc, rhs: FetchOperand(param0, inst.Arg0), r8: (sbyte)inst.Arg1);

        Pc += inst.Operation.Size;
        SFRs.Cy = lhs < rhs;
        if (lhs != rhs)
            Pc = (ushort)(Pc + r8);
    }

    /// <summary>Near absolute subroutine call</summary>
    private void Op_CALL(Instruction inst)
    {
        // similar to OP_JMP
        // (PC) <- (PC) + 2, (SP) <- (SP) + 1, ((SP)) <- (PC7 to 0), (SP) <- (SP) + 1, ((SP)) <- (PC15 to 8), (PC11 to 00) <- a12
        // 000a_1aaa aaaa_aaaa

        ushort a12 = inst.Arg0;

        Pc += inst.Size;
        Memory.PushStack((byte)Pc);
        Memory.PushStack((byte)(Pc >> 8));

        Pc &= 0b1111_0000__0000_0000;
        Pc |= a12;
    }

    /// <summary>Far absolute subroutine call</summary>
    private void Op_CALLF(Instruction inst)
    {
        // Similar to Op_JMPF
        // (PC) <- (PC) + 3, (SP) <- (SP) + 1, ((SP)) <- (PC7 to 0),
        // (SP) <- (SP) + 1, ((SP)) <- (PC15 to 8), (PC) <- a16
        var a16 = inst.Arg0;
        Pc += 3;
        Memory.PushStack((byte)Pc);
        Memory.PushStack((byte)(Pc >> 8));
        Pc = a16;
    }

    /// <summary>Far relative subroutine call</summary>
    private void Op_CALLR(Instruction inst)
    {
        // (PC) <- (PC) + 3, (SP) <- (SP) + 1, ((SP)) <- (PC7 to 0),
        // (SP) <- (SP) + 1, ((SP)) <- (PC15 to 8), (PC) <- (PC) - 1 + r16
        var r16 = inst.Arg0;
        Pc += inst.Size;
        Memory.PushStack((byte)Pc);
        Memory.PushStack((byte)(Pc >> 8));
        Pc = (ushort)(Pc - 1 + r16);
    }

    /// <summary>Return from subroutine</summary>
    private void Op_RET(Instruction inst)
    {
        // (PC15 to 8) <- ((SP)), (SP) <- (SP) - 1, (PC7 to 0) <- ((SP)), (SP) <- (SP) -1
        var Pc15_8 = Memory.PopStack();
        var Pc7_0 = Memory.PopStack();
        Pc = (ushort)(Pc15_8 << 8 | Pc7_0);
    }

    /// <summary>Return from interrupt</summary>
    private void Op_RETI(Instruction inst)
    {
        // (PC15 to 8) <- ((SP)), (SP) <- (SP) - 1, (PC7 to 0) <- ((SP)), (SP) <- (SP) -1
        if (InterruptServicingState != InterruptServicingState.Servicing)
            Logger.LogDebug($"Expected 'Servicing' state when returning from interrupt, but got '{InterruptServicingState}'");

        InterruptServicingState = InterruptServicingState.Returned;
        var Pc15_8 = Memory.PopStack();
        var Pc7_0 = Memory.PopStack();
        Pc = (ushort)(Pc15_8 << 8 | Pc7_0);
    }

    /// <summary>Clear direct bit</summary>
    private void Op_CLR1(Instruction inst)
    {
        // (d9, b3) <- 0
        var d9 = inst.Arg0;
        var b3 = (byte)inst.Arg1;
        var memory = ReadRam(d9);
        BitHelpers.WriteBit(ref memory, bit: b3, value: false);
        WriteRam(d9, memory);
        Pc += inst.Size;
    }

    /// <summary>Set direct bit</summary>
    private void Op_SET1(Instruction inst)
    {
        // (d9, b3) <- 1
        var d9 = inst.Arg0;
        var b3 = (byte)inst.Arg1;
        var memory = ReadRam(d9);
        BitHelpers.WriteBit(ref memory, bit: b3, value: true);
        WriteRam(d9, memory);
        Pc += inst.Size;
    }

    /// <summary>Not direct bit</summary>
    private void Op_NOT1(Instruction inst)
    {
        // (d9, b3) <- !(d9, b3)
        var d9 = inst.Arg0;
        var b3 = (byte)inst.Arg1;
        var memory = ReadRam(d9);
        var bit = BitHelpers.ReadBit(memory, b3);
        BitHelpers.WriteBit(ref memory, bit: b3, value: !bit);
        WriteRam(d9, memory);
        Pc += inst.Size;
    }

    /// <summary>Load a value from flash memory into accumulator. Undocumented.</summary>
    private void Op_LDF(Instruction inst)
    {
        var a16 = SFRs.Trl | (SFRs.Trh << 8);
        var bank = SFRs.FPR0 ? FlashBank1 : FlashBank0;
        SFRs.Acc = bank[a16];
        Pc += inst.Size;
    }

    // OP_STF

    /// <summary>No operation</summary>
    private void Op_NOP(Instruction inst)
    {
        Pc += inst.Size;
    }
}
