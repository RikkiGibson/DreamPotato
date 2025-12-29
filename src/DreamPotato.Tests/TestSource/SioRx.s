;    /////////////////////////////////////////////////////////////
;   ///                        VECTORS                        ///
;  /////////////////////////////////////////////////////////////
    .org 0   ; entry point
  jmpf main
    .org $03 ; External int. (INTO)
  jmp int_03
    .org $0B ; External int. (INT1)
  jmp int_0b
    .org $13 ; External int. (INT2) and Timer 0 low
  jmp int_13
    .org $1B ; External int. (INT3) and base timer
  jmp int_1b
    .org $23 ; Timer 0 high
  jmp int_23
    .org $2B ; Timer 1 Low and High
  jmp int_2b
    .org $33 ; Serial IO 1
  jmp int_33
    .org $3B ; Serial IO 2
  jmp int_3b
    .org $43 ; VMU to VMU comms
  jmp int_43
    .org $4B ; Port 3 interrupt
  jmp int_4b

;    /////////////////////////////////////////////////////////////
;   ///                    INTERRUPT HANDLERS                 ///
;  /////////////////////////////////////////////////////////////
int_03:
  reti
int_0b:
  reti
int_13:
  reti
int_23:
  reti
int_2b:
  reti
int_33:
  reti
int_3b:
  reti
int_43:
  reti
int_4b:
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
    .byte "SioRx           " ; ................... 16-byte Title
    .byte "Serial Reception test           " ; ... 32-byte Description

;    /////////////////////////////////////////////////////////////
;   ///                       GAME ICON                       ///
;  /////////////////////////////////////////////////////////////
    .org $240 ; >>> ICON HEADER
    .org $260 ; >>> PALETTE TABLE
    .org $280 ; >>> ICON DATA

;    //////////////////////////////////////////////////////////////
;   ///                 SERIAL COMMUNICATIONS SAMPLE           ///
;  //////////////////////////////////////////////////////////////

;----------------------------------------------------------------------------
; ** Serial Communications Sample 2 (Data Reception) **
;
; ·Displays a numeric value that was received from the serial communications port on the LCD
;----------------------------------------------------------------------------

; **** Definition of System Constants ***************************************
    .include "sfr.i"
; OCR (Oscillation Control Register) settings
osc_rc = $4d ; Specifies internal RC oscillation for the system clock
osc_xt = $ef ; Specifies crystal oscillation for the system clock
r2 = $2      ; Indirect address register 2

; variables
counter = $10 ; Counter
work1 = $11 ; Work (used in put2digit)
LowBattChk = $6e ; Low battery detection flag (RAM bank 0)

; *-------------------------------------------------------------------------*
; * User program *
; *-------------------------------------------------------------------------*
main:
  call cls ; Clears the LCD display
  call BattChkOff ; Turns off the low battery automatic detection function

cwait:
  call SioInit ; Serial communications initialization
  bz start ; Starts if VM is connected

  ld P3 ; [M] button check
  bn acc,6,finish ; If the [M] button is pressed, the application ends

  jmp cwait ; Waits until VM is connected
start:

loop0:
  call SioRecv1 ; Receives one byte
  bnz next4 ; If there is no received data, then goes to next4

  ld b ; Loads the received data into acc
  mov #2,c ; Display coordinates (horizontal)
  mov #1,b ; Display coordinates (vertical)
  call put2digit ; Displays the two-digit value on the LCD

next4: ; ** [M] (mode) Button Check **
  ld P3
  bn acc,6,finish ; If the [M] button is pressed, the application ends

  jmp loop0 ; Repeat

finish: ; ** Application End Processing **
  call SioEnd ; Serial communications end processing
  call BattChkOn ; Turns on the low battery automatic detection function
  jmp game_end ; Application end

; *-------------------------------------------------------------------------*
; * Serial Communications Initialization *
; * Outputs:acc = 0 : Normal end *
; * acc = 0ffh: VM not connected *
; *-------------------------------------------------------------------------*
; Serial communications initialization
; This sample assumes that the system clock is in crystal mode.
SioInit:
  ; **** VM Connection Check ****
  ld P7 ; Checks the connection status
  and #%00001101 ; Checks P70, P72, P73
  sub #%00001000 ; P70 = 0, P72 = 0, P73 = 1
  bz next3 ; To next3 if connected

  mov #$ff,acc ; If not connected, abnormal end with acc = 0ffh
  ret ; SioInit end
next3:

  ; **** Serial Communications Initialization ****
  mov #0,SCON0 ; Specifies output as 'LSB first'
  mov #0,SCON1 ; Specifies input as 'LSB first'
  mov #$88,SBR ; Sets the transfer rate
  clr1 P1,0 ; Clears the P10 latch (P10/S00)
  clr1 P1,2 ; Clears the P12 latch (P12/SCK0)
  clr1 P1,3 ; Clears the P13 latch (P13/S01)

  mov #%00000101,P1FCR ; Sets the pin functions
  mov #%00000101,P1DDR ; Sets the pin functions

  mov #0,SBUF0 ; Clears the transfer buffer
  mov #0,SBUF1 ; Clears the transfer buffer

  ret ; SioInit end

; *-------------------------------------------------------------------------*
; * Serial Communications End *
; *-------------------------------------------------------------------------*
SioEnd: ; **** Serial Communications End Processing ****

  mov #0,SCON0 ; SCON0 = 0
  mov #0,SCON1 ; SCON1 = 0
  mov #$bf,P1FCR ; P1FCR = 0bfh
  mov #$a4,P1DDR ; P1DDR = 0a4h

  ret ; SioEnd end

; *-------------------------------------------------------------------------*
; * Receiving 1 Byte from a Serial port *
; * Outputs: b: Received data *
; * acc = 0 : Received data found *
; * acc = 0ffh: Received data not found*
; *-------------------------------------------------------------------------*
SioRecv1: ; **** Receiving 1 Byte ****
  ld SCON1
  bp acc,1,next5 ; If received data is found, then go to next5
  bp acc,3,next6 ; If transfer is currently in progress, then go to next6

  set1 SCON1,3 ; Starts transfer
next6:
  mov #$ff,acc ; Returns with acc = 0ffh (received data not found)
  ret ; SioRecv1 end
next5:

  ld SBUF1 ; Loads the received data
  st b ; Copies the data into b

  clr1 SCON1,1 ; Resets the transfer end flag

  mov #0,acc ; Returns with acc = 0 (received data found)
  ret ; SioRecv1 end

; *-------------------------------------------------------------------------*
; * Displaying a two-digit value *
; * Inputs: acc: Numeric value *
; * c: Horizontal position of character*
; * b: Vertical position of character*
; *-------------------------------------------------------------------------*
put2digit:
  push b ; Pushes the coordinate data onto the stack
  push c ;
  st c ; Calculates the tens digit and the ones digit
  xor acc ; ( acc = acc/10, work1 = acc mod 10 )
  mov #10,b ;
  div ;
  ld b ;
  st work1 ; Stores the ones digit in work1
  ld c ;
  pop c ; Pops the coordinate values into (c, b)
  pop b ;
  push b ; Pushes the coordinates onto the stack again
  push c ;
  call putch ; Displays the tens digit
  ld work1 ; Loads the ones digit
  pop c ; Pops the coordinate values into (c, b)
  pop b ;
  inc c ; Moves the display coordinates to the right
  call putch ; Displays the ones digit

  ret ; put2digit end

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
  push PSW ; Pushes the PSW value onto the stack
  push acc

  set1 PSW,1 ; Selects data RAM bank 1

  inc counter ; Increments the counter

  ld counter ; If the counter value is:
  bne #100,next_bt ; not 100, then next_bt
  mov #0,counter ; 100, then reset to '0'
next_bt:
  pop acc
  pop PSW ; Pops the PSW value off of the stack

  clr1 BTCR,1 ; Clears the base timer interrupt source
  ret ; User interrupt processing end
