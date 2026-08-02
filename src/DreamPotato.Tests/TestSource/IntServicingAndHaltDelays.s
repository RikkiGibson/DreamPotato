; Program for determining how much delay is introduced
; when servicing an interrupt and when waking from halt.
;
; REAL HW OUTPUT:
; 008009
; 008017
; 000009
; 009009

;    /////////////////////////////////////////////////////////////
;   ///                        VECTORS                        ///
;  /////////////////////////////////////////////////////////////
.org 0   ; entry point
  jmpf main

.org $03 ; External int. (INTO)
  reti
.org $0B ; External int. (INT1)
  reti
.org $13 ; External int. (INT2) and Timer 0 low
  reti
.org $1B ; External int. (INT3) and base timer
  jmp int3_bt
.org $23 ; Timer 0 high
  jmp int_t0h
.org $2B ; Timer 1 Low and High
  jmp int_t1
.org $33 ; Serial IO 0
  reti
.org $3B ; Serial IO 1
  reti
.org $43 ; VMU to VMU comms
  reti
.org $4B ; Port 3 interrupt
  jmp int_p3

;    /////////////////////////////////////////////////////////////
;   ///                    INTERRUPT HANDLERS                 ///
;  /////////////////////////////////////////////////////////////
int3_bt:
  clr1 BTCR,1 ; Clears the base timer interrupt source
  clr1 BTCR,3 ; Clears the base timer interrupt source
  reti ; User interrupt processing end

int_t0h:
  reti

int_t1:
  push acc
  ld T1L
  st T1LIntValue
  ld T1CNT
  st T1CntIntValue
  clr1 t1cnt, 1
  pop acc
  reti

int_p3:
  clr1 p3int,1 ; interrupt flag clear
  reti

;    /////////////////////////////////////////////////////////////
;   ///                EXTERNAL PROGRAMS                      ///
;  /////////////////////////////////////////////////////////////
.org $1F0
game_end:
    not1 EXT, 0   ;
  jmpf game_end ; return to BIOS


;    /////////////////////////////////////////////////////////////
;   ///                    DREAMCAST HEADER                   ///
;  /////////////////////////////////////////////////////////////
.org $200
.byte "TimerPreemption " ; ................... 16-byte Title
.byte "Timer Preemption Test 1         " ; ... 32-byte Descr1tion

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
r3 = $3      ; Indirect address register 3

; variables
;
flag = $11
xpos = $12 ; x tile position to draw flag
ypos = $13 ; y tile position to draw flag
T1LIntValue = $14
T1CntIntValue = $15

;
; *-------------------------------------------------------------------------*
; * User program *
; *-------------------------------------------------------------------------*
;
;
;
main:
  call cls ; Clears the LCD display
  call ClearVariables

  call CheckServicingDelay1
  call CheckServicingDelay2

  mov #0, xpos
  inc ypos
  call CheckServicingDelay3
  call CheckServicingDelay4

  mov #0, xpos
  inc ypos
  call CheckServicingDelay5
  call CheckServicingDelay6

  mov #0, xpos
  inc ypos
  call CheckServicingDelay7
  call CheckServicingDelay8

  call WaitForExit

ClearVariables:
  xor acc ; zero out acc
  st flag ; init variables
  st xpos
  st ypos
  st T1LIntValue
  st T1CntIntValue
  st BTCR
  st T1LC ; ensure no sound output
  ret

; Real HW output: 008
CheckServicingDelay1:
  ; Wait for an interrupt then print the T1L value observed by the interrupt handler.
  ; Use halt mode to wait.
  mov #0, T1L
  mov #%01000001, T1CNT ; T1LRUN | T1LIE
  set1 pcon, 0
  mov #0, T1CNT
  ld T1LIntValue
  call print_decimal
  ret

; Real HW output: 009
CheckServicingDelay2:
  ; Wait for an interrupt then print the T1L value observed by the interrupt handler.
  ; Use bn/bp (2 cycle instructions) to wait.
  mov #0, T1L
  mov #%01000001, T1CNT ; T1LRUN | T1LIE
.BusyWait1:
  bn T1L, 7, .BusyWait1
.BusyWait2:
  bp T1L, 7, .BusyWait2
  mov #0, T1CNT
  ld T1LIntValue
  call print_decimal
  ret

; Real HW output: 008
CheckServicingDelay3:
  ; Wait for an interrupt at which point T1L value is instantly read.
  ; Use loop followed by nop sequence (1 cycle instruction) to wait.
  mov #0, T1L
  mov #%01000001, T1CNT ; T1LRUN | T1LIE
  mov #125, acc
.BusyWait:
  dbnz acc, .BusyWait
  nop
  nop
  nop
  nop
  nop
  nop

  mov #0, T1CNT
  ld T1LIntValue
  call print_decimal
  ret

; Real HW output: 017
CheckServicingDelay4:
  ; Wait for an interrupt, wake, then read T1L value ourselves.
  mov #0, T1L
  mov #%01000001, T1CNT ; T1LRUN | T1LIE
  set1 pcon, 0
  ld T1L
  mov #0, T1CNT
  call print_decimal
  ret

; Real HW output: 000
CheckServicingDelay5:
  ; Manually set T1LOVF, then print T1L value observed by interrupt handler.
  mov #0, T1L
  mov #0, T1LIntValue
  mov #%01000001, T1CNT ; T1LRUN | T1LIE
  set1 T1CNT, 1 ; T1LOVF
  ld T1LIntValue
  mov #0, T1CNT
  call print_decimal
  ret

; Real HW output: 009
CheckServicingDelay6:
  ; Manually set T1LOVF, wait one instruction, then print T1L value observed by interrupt handler.
  mov #0, T1L
  mov #0, T1LIntValue
  mov #%01000001, T1CNT ; T1LRUN | T1LIE
  set1 T1CNT, 1 ; T1LOVF
  nop
  ld T1LIntValue
  mov #0, T1CNT
  call print_decimal
  ret

; Real HW output: 009
CheckServicingDelay7:
  ; Set then clear T1LOVF, then observe whether an interrupt handler was executed.
  mov #0, T1L
  mov #0, T1LIntValue
  mov #%01000001, T1CNT ; T1LRUN | T1LIE
  set1 T1CNT, 1 ; T1LOVF
  clr1 T1CNT, 1 ; T1LOVF
  ld T1LIntValue
  mov #0, T1CNT
  call print_decimal
  ret

; Real HW output: 067 (64 + 2 + 1) / T1LRUN + T1LOVF + T1LIE
CheckServicingDelay8:
  ; Wait for an interrupt then print the T1CNT value observed by the interrupt handler.
  ; Use bn/bp (2 cycle instructions) to wait.
  mov #0, T1L
  mov #0, T1CntIntValue
  mov #%01000001, T1CNT ; T1LRUN | T1LIE
.BusyWait1:
  bn T1L, 7, .BusyWait1
.BusyWait2:
  bp T1L, 7, .BusyWait2
  mov #0, T1CNT
  ld T1CntIntValue
  call print_decimal
  ret

; Done working. Wait for mode button (to exit)
WaitForExit: ; ** [M] (mode) Button Check **
  set1 pcon, 0
  ld P3
  bn acc,6,finish ; If the [M] button is pressed, the application ends

  jmp WaitForExit ; Repeat

finish: ; ** Application End Processing **
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

; Print the value in 'acc' as a 3-digit decimal number
; at positions (xpos, ypos)-(xpos+2, ypos)
;
print_decimal:
  ; 2,0: hundreds digit
  st c        ; divisor(7-0)
  xor acc     ; divisor(15-8)
  mov #100, b ; dividend
  div
  ld c        ; result(7-0)
  st flag
  push b ; Save remainder (dec 0-99) for next step
  call putch_xy

  ; 2,1: tens digit
  inc xpos
  pop c       ; use remainder as new divisor(7-0)
  xor acc     ; divisor(15-8)
  mov #10, b  ; dividend
  div
  ld c        ; result(7-0)
  st flag
  push b      ; Save remainder (dec 0-9) for next step
  call putch_xy

  ; 2,2: ones digit
  inc xpos
  pop flag
  call putch_xy

  inc xpos
  ret

; Call putch, transferring values from stable to non-stable variables
;
putch_xy:
  ld xpos
  st c
  ld ypos
  st b
  ld flag ; Draw the flag value to screen
  call putch
  ret

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
