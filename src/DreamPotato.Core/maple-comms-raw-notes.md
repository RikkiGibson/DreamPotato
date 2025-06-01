# Maple Comms Raw Notes

Investigating what the BIOS is doing in order to send/receive Maple messages.

## Involved Registers

Mplsw: Maple Status Word = 0x60
- Enables bit 0 and 1 of Vsel, then enables bit 2 of Mplsw, then returns..
- ROM@[1014]: SET1 Mplsw, 2
- ROM@[02A4]: CLR1 Mplsw, 2

Mplsta: Maple Start Transfer = 0x61

Mplrst: Maple Reset = 0x62
- ROM 1007 likely a maple-related function.
- ROM@[1007]: MOV #80H, Mplrst
- NOPs twice, then..
- ROM@[100C]: MOV #0, Mplrst

Vsel: Controls Work RAM.
- Documented to reset with value 0b1111_1100.
- Bit 4: Ince: auto-increments Vramad on access. Used in userspace.
- Bits 1 and 0: ???. Possibly controls data direction?
- Bit 1: SIOSEL. when true, P1 is used for dedicated Maple interface. Otherwise, used for serial.
- Bit 0: ASEL. when true, it means DC to WRAM transfer in progress, and WRAM not accessible by VMU.
- ROM@[02A0]: CLR1 Vsel, 0
- ROM@[02A2]: CLR1 Vsel, 1