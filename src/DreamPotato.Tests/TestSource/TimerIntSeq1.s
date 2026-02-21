; Program for determining the sequence in which timers induce interrupts and when those interrupts are serviced

;    /////////////////////////////////////////////////////////////
;   ///                        VECTORS                        ///
;  /////////////////////////////////////////////////////////////
    .org 0   ; entry point
  jmpf main
    .org $03 ; External int. (INTO)
InterruptVectors:
  jmp .int_03
    .org $0B ; External int. (INT1)
  jmp .int_0b
    .org $13 ; External int. (INT2) and Timer 0 low
  jmp .int_13
    .org $1B ; External int. (INT3) and base timer
  jmp .int_1b
    .org $23 ; Timer 0 high
  jmp .int_23
    .org $2B ; Timer 1 Low and High
  jmp .int_2b
    .org $33 ; Serial IO 1
  jmp .int_33
    .org $3B ; Serial IO 2
  jmp .int_3b
    .org $43 ; VMU to VMU comms
  jmp .int_43
    .org $4B ; Port 3 interrupt
  jmp .int_4b

;    /////////////////////////////////////////////////////////////
;   ///                    INTERRUPT HANDLERS                 ///
;  /////////////////////////////////////////////////////////////
.int_03:
  reti
.int_0b:
  reti
.int_13:
  reti
.int_23:
  reti
.int_2b:
  call int_T1
  reti
.int_33:
  reti
.int_3b:
  reti
.int_43:
  reti
.int_4b:
  clr1 p3int,1 ; interrupt flag clear
  reti

;    /////////////////////////////////////////////////////////////
;   ///                EXTERNAL PROGRAMS                      ///
;  /////////////////////////////////////////////////////////////
    .org $100
fm_wrt_ex:
    not1 EXT, 0   ;
  jmpf fm_wrt_ex  ; return to BIOS
  ret
    .org $110
fm_vrf_ex:
    not1 EXT, 0   ;
  jmpf fm_vrf_ex  ; return to BIOS
  ret
    .org $120
fm_prd_ex:
    not1 EXT, 0   ;
  jmpf fm_prd_ex  ; return to BIOS
  ret
    .org $130
int_1b:
timer_ex:
  push ie
  clr1 ie,7 ; interrupt prohibition
    not1 EXT, 0   ;
  jmpf timer_ex   ; return to BIOS
  call int_BaseTimer ; User interrupt processing
  pop ie
  reti
    .org $1F0
game_end:
    not1 EXT, 0   ;
  jmpf game_end ; return to BIOS


;    /////////////////////////////////////////////////////////////
;   ///                    DREAMCAST HEADER                   ///
;  /////////////////////////////////////////////////////////////
    .org $200
    .byte "TimerIntSeq1    " ; ................... 16-byte Title
    .byte "Timer Interrupt Sequence Test 1 " ; ... 32-byte Descr1tion

;    /////////////////////////////////////////////////////////////
;   ///                       GAME ICON                       ///
;  /////////////////////////////////////////////////////////////
    .org $240 ; >>> ICON HEADER
    .org $260 ; >>> PALETTE TABLE
    .org $280 ; >>> ICON DATA

;    //////////////////////////////////////////////////////////////
;   ///                 INTERRUPT DELAYS SAMPLE.               ///
;  //////////////////////////////////////////////////////////////

;----------------------------------------------------------------------------
; ** Interrupt Delays Sample **
;----------------------------------------------------------------------------

; **** Definition of System Constants ***************************************
    .include "sfr.i"
; OCR (Oscillation Control Register) settings
osc_rc = $4d ; Specifies internal RC oscillation for the system clock
osc_xt = $ef ; Specifies crystal oscillation for the system clock
r2 = $2      ; Indirect address register 2

; variables

flag = $11
xpos = $12 ; x tile position to draw flag
ypos = $13 ; y tile position to draw flag

LowBattChk = $6e ; Low battery detection flag (RAM bank 0)


;
; *-------------------------------------------------------------------------*
; * User program *
; *-------------------------------------------------------------------------*
;
;
;
;
;
;
main:
  call cls ; Clears the LCD display
  call BattChkOff ; Turns off the low battery automatic detection function
start:

xor acc ; zero out acc
st flag ; init variables
st xpos
st ypos
st BTCR
st T1LC

; 1st: how long from enabling the timer, till interrupt occurring?
; REAL HW: 2
clr1 IE,7 ; master interrupt disable
mov #0, T1CNT
mov #$ff, T1L ; Set T1L reload value
set1 IE,7 ; master interrupt enable
mov #%01000001, T1CNT ; T1LRUN (run T1L) | T1LIE (enable T1L interrupt)
inc flag ; question: what is the value of 'flag' when T1 interrupt actually runs?
inc flag
inc flag
inc flag

; 2nd: try seeing what is in T1L when it gets an extra cycle to run before expiring.
; REAL HW: 3
inc xpos
clr1 IE,7 ; master interrupt disable
mov #0, flag
mov #0, T1CNT
mov #$fe, T1L ; Set T1L reload value
mov #0, T1LC ; Set compare value (disable audio)
set1 IE,7 ; master interrupt enable
mov #%01000001, T1CNT ; T1LRUN (run T1L) | T1LIE (enable T1L interrupt)
inc flag ; question: what is the value of 'flag' when T1 interrupt actually runs?
inc flag
inc flag
inc flag

; 3rd: what do we read from T1L, the cycle after enabling it?
; REAL HW: FE (display 8)
inc xpos
clr1 IE,7 ; master interrupt disable
mov #0, flag
mov #0, T1CNT
mov #$fd, T1L ; Set T1L reload value
mov #0, T1LC ; Set compare value (disable audio)
set1 IE,7 ; master interrupt enable
mov #%01000001, T1CNT ; T1LRUN (run T1L) | T1LIE (enable T1L interrupt)
ld T1L
nop
nop
nop
nop

; 4th: what do we read from T1L, if we set reload value first before enabling it?
; REAL HW: FE (display 8)
inc xpos
clr1 IE,7 ; master interrupt disable
mov #0, flag
mov #$fd, T1L ; Set T1L reload value
mov #0, T1CNT
mov #0, T1LC ; Set compare value (disable audio)
set1 IE,7 ; master interrupt enable
mov #%01000001, T1CNT ; T1LRUN (run T1L) | T1LIE (enable T1L interrupt)
ld T1L
nop
nop
nop
nop

; 5th: what do we read from T1LOVF the cycle after enabling it
; REAL HW: 5 (instantly set)
inc xpos
clr1 IE,7 ; master interrupt disable
xor acc
mov #0, flag
mov #$ff, T1L ; Set T1L reload value
mov #0, T1CNT
set1 IE,7 ; master interrupt enable
mov #%01000001, T1CNT ; T1LRUN (run T1L) | T1LIE (enable T1L interrupt)
ld T1CNT
nop
nop
nop

; 6th: what do we read from T1LOVF the 2nd cycle after enabling it
; REAL HW: 5 (instantly set)
inc xpos
clr1 IE,7 ; master interrupt disable
xor acc
mov #0, flag
mov #$ff, T1L ; Set T1L reload value
mov #0, T1CNT
set1 IE,7 ; master interrupt enable
mov #%01000001, T1CNT ; T1LRUN (run T1L) | T1LIE (enable T1L interrupt)
nop
ld T1CNT
nop
nop
nop

; Done working. Wait for mode button (to exit)
next4: ; ** [M] (mode) Button Check **
  ld P3
  bn acc,6,finish ; If the [M] button is pressed, the application ends

  jmp next4 ; Repeat

finish: ; ** Application End Processing **
  call BattChkOn ; Turns on the low battery automatic detection function
  jmp game_end ; Application end

;
; *-------------------------------------------------------------------------*
; Helper Subroutines
; *-------------------------------------------------------------------------*
;
;
;
;
;
;

; *-------------------------------------------------------------------------*
; * Clearing the LCD Display Image *
; *-------------------------------------------------------------------------*
cls:
  push OCR ; Pushes the OCR value onto the stack
  mov #osc_rc,OCR ; Specifies the system clock

  mov #0,XBNK ; Specifies the display RAM bank address (BANK0)
  call cls_s ; Clears the data in that bank

  mov #1,XBNK ; Specifies the display RAM bank address (BANK1)
  call cls_s ; Clears the data in that bank
  pop OCR ; Pops the OCR value off of the stack

  ret ; cls end

cls_s: ; *** Clearing One Bank of Display RAM ***
  mov #$80,r2 ; Points the indirect addressing register at the start of display RAM
  mov #$80,b ; Sets the number of loops in loop counter b
loop3:
  mov #0,@r2 ; Writes "0" while incrementing the address
  inc r2 ;
  dbnz b,loop3 ; Repeats until b is "0"

  ret ; cls_s end

; *-------------------------------------------------------------------------*
; * Displaying One Character in a Specified Position*
; * Inputs: acc: Character code *
; * c: Horizontal position of character*
; * b: Vertical position of character*
; *-------------------------------------------------------------------------*
putch:
  push XBNK
  push acc
  call locate ; Calculates display RAM address according to coordinates
  pop acc
  call put_chara ; Displays one character
  pop XBNK

  ret ; putch end

locate: ; **** Calculating the Display RAM Address According to the Display Position Specification ****
  ; ** Inputs: c: Horizontal position (0 to 5) b: Vertical position (0 to 3)
  ; ** Outputs: r2: RAM address XBNK: Display RAM bank

  ; *** Determining the Display RAM Bank Address ***
  ld b ; Jump to next1 when b >= 2
  sub #2 ;
  bn PSW,7,next1 ;

  mov #$00,XBNK ; Specifies the display RAM bank address (BANK0)
  br next2
next1:
  st b
  mov #$01,XBNK ; Specifies the display RAM bank address (BANK1)
next2:

  ; *** Calculating the RAM Address for a Specified Position on the Display ***
  ld b ; b * 40h + c + 80h
  rol ;
  rol ;
  rol ;
  rol ;
  rol ;
  rol ;
  add c ;
  add #$80 ;
  st r2 ; Stores the RAM address in r2

  ret ; locate end

put_chara:
  push PSW ; Pushes the PSW value onto the stack
  set1 PSW,1 ; Selects data RAM bank 1

  ; *** Calculating the Character Data Address ***
  rol ; (TRH,TRL) = acc*8 + fontdata
  rol ;
  rol ;
  add #<(fontdata) ;
  st TRL ;
  mov #0,acc ;
  addc #>(fontdata) ;
  st TRH ;

  push OCR ; Pushes the OCR value onto the stack
  mov #osc_rc,OCR ; Specifies the system clock

  mov #0,b ; Offset value for loading the character data
  mov #4,c ; Loop counter
loop1:
  ld b ; Loads the display data for the first line
  ldc ;
  inc b ; Increments the load data offset by 1
  st @r2 ; Transfers the display data to display RAM
  ld r2 ; Adds 6 to the display RAM address
  add #6 ;
  st r2 ;

  ld b ; Loads the display data for the second line
  ldc ;
  inc b ; Increments the load data offset by 1
  st @r2 ; Transfers the display data to display RAM
  ld r2 ; Adds 10 to the display RAM address
  add #10 ;
  st r2 ;

  dec c ; Decrements the loop counter
  ld c ;
  bnz loop1 ; Repeats for 8 lines (four times)

  pop OCR ; Pops the OCR value off of the stack
  pop PSW ; Pops the PSW value off of the stack

  ret ; put_chara end

; *-------------------------------------------------------------------------*
; * Character Bit Image Data *
; *-------------------------------------------------------------------------*
fontdata:
  .byte $7c, $e6, $c6, $c6, $c6, $ce, $7c, $00 ; '0' 00
  .byte $18, $38, $18, $18, $18, $18, $3c, $00 ; '1' 01
  .byte $7c, $c6, $c6, $0c, $38, $60, $fe, $00 ; '2' 02
  .byte $7c, $e6, $06, $1c, $06, $e6, $7c, $00 ; '3' 03
  .byte $0c, $1c, $3c, $6c, $cc, $fe, $0c, $00 ; '4' 04
  .byte $fe, $c0, $fc, $06, $06, $c6, $7c, $00 ; '5' 05
  .byte $1c, $30, $60, $fc, $c6, $c6, $7c, $00 ; '6' 06
  .byte $fe, $c6, $04, $0c, $18, $18, $38, $00 ; '7' 07
  .byte $7c, $c6, $c6, $7c, $c6, $c6, $7c, $00 ; '8' 08
  .byte $7c, $c6, $c6, $7e, $06, $0c, $78, $00 ; '9' 09

; *-------------------------------------------------------------------------*
; * Low Battery Automatic Detection Function ON*
; *-------------------------------------------------------------------------*
BattChkOn:
  push PSW ; Pushes the PSW value onto the stack

  clr1 PSW,1 ; Selects data RAM bank 0
  mov #0,acc ; Detects low battery (0)
  st LowBattChk ; Low battery automatic detection flag (RAM bank 0)

  pop PSW ; Pops the PSW value off of the stack
  ret ; BattChkOn end

; *-------------------------------------------------------------------------*
; * Low Battery Automatic Detection Function OFF*
; *-------------------------------------------------------------------------*
BattChkOff:
  push PSW ; Pushes the PSW value onto the stack

  clr1 PSW,1 ; Selects data RAM bank 0
  mov #$ff,acc ; Does not detect low battery (0ffh)
  st LowBattChk ; Low battery automatic detection flag (RAM bank 0)

  pop PSW ; Pops the PSW value off of the stack
  ret ; BattChkOff end

; *-------------------------------------------------------------------------*
; * Base Timer Interrupt Handler *
; *-------------------------------------------------------------------------*
int_BaseTimer:
  clr1 BTCR,1 ; Clears the base timer interrupt source
  clr1 BTCR,3 ; Clears the base timer interrupt source
  ret ; User interrupt processing end


; *-------------------------------------------------------------------------*
; * T1 Interrupt Handler *
; *-------------------------------------------------------------------------*
int_T1:
  push PSW ; Pushes the PSW value onto the stack
  push acc
  push b
  push c

  set1 PSW,1 ; Selects data RAM bank 1

  be #%01000011, .accSourceFlagSet
  be #%01000001, .accSourceFlagClear
  be #$fd, .accWasFD
  be #$fe, .accWasFE
  be #$ff, .accWasFF
  br .draw

.accSourceFlagSet:
  mov #5, flag
  br .draw
.accSourceFlagClear:
  mov #6, flag
  br .draw
.accWasFD:
  mov #7, flag
  br .draw
.accWasFE:
  mov #8, flag
  br .draw
.accWasFF:
  mov #9, flag
  br .draw

.draw:
  ; Draw the digit for 'flag' at position (b,c)
  ld xpos
  st c
  ld ypos
  st b
  ld flag ; Draw the flag value to screen
  call putch

  ld flag ; Modify the flag so we notice if we end up running the ISR twice
  add #4
  st flag

  pop c
  pop b
  pop acc
  pop PSW ; Pops the PSW value off of the stack

  mov #0, T1CNT
  ret ; User interrupt processing end