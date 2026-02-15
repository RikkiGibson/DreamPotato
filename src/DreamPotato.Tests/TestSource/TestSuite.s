; General test suite program for verifying emulator accuracy.
; Intended for running both on original hardware and on emulators.
.include "sfr.i"

.org 0   ; entry point
  jmpf main

; *-------------------------------------------------------------------------*
; * Interrupt Vectors *
; *-------------------------------------------------------------------------*
Interrupts:
.org $03
  jmp .int0
.org $0B
  jmp .int1
.org $13
  jmp .int1_t0l
.org $1B
  jmp int3_bt
.org $23
  jmp .int_t0h
.org $2B
  jmp .int_t1
.org $33
  jmp .int_sio0
.org $3B
  jmp .int_sio1
.org $43
  jmp .int_maple
.org $4B
  jmp .int_p3

.int0:
  reti
.int1:
  reti
.int1_t0l:
  reti
.int_t0h:
  reti
.int_t1:
  reti
.int_sio0:
  reti
.int_sio1:
  reti
.int_maple:
  reti
.int_p3:
  clr1 p3int,1 ; interrupt flag clear
  reti

; *-------------------------------------------------------------------------*
; * External Programs *
; *-------------------------------------------------------------------------*
.org $100
fm_wrt_ex:
  not1 EXT, 0
  jmpf fm_wrt_ex  ; return to BIOS
  ret

.org $110
fm_vrf_ex:
  not1 EXT, 0
  jmpf fm_vrf_ex  ; return to BIOS
  ret

.org $120
fm_prd_ex:
  not1 EXT, 0
  jmpf fm_prd_ex  ; return to BIOS
  ret

.org $130
int3_bt:
timer_ex:
  push ie
  clr1 ie,7 ; disable interrupts
  not1 EXT, 0
  jmpf timer_ex   ; return to BIOS
  call int_BaseTimer ; User interrupt processing
  pop ie
  reti

.org $1F0
game_end:
  not1 EXT, 0   ;
  jmpf game_end ; return to BIOS


; *-------------------------------------------------------------------------*
; * Dreamcast Header *
; *-------------------------------------------------------------------------*
.org $200
  .byte "Test Suite      " ; ................... 16-byte Title
  .byte "VMU Hardware Test Suite         " ; ... 32-byte Descr1tion

; *-------------------------------------------------------------------------*
; * Game Icon *
; *-------------------------------------------------------------------------*
.org $240 ; >>> ICON HEADER
.org $260 ; >>> PALETTE TABLE
.org $280 ; >>> ICON DATA

; *-------------------------------------------------------------------------*
; * Constants *
; *-------------------------------------------------------------------------*
; OCR (Oscillation Control Register) settings
osc_rc = $4d ; Specifies internal RC oscillation for the system clock
osc_xt = $ef ; Specifies crystal oscillation for the system clock
r2 = $2      ; Indirect address register 2
TestCaseCount = $01

; variables (bank 0)
LowBattChk = $6e ; Low battery detection flag (RAM bank 0)

; variables (bank 1)
testCaseIdLow = $00
testCaseIdHigh = $01
xpos = $12 ; x tile position to draw flag
ypos = $13 ; y tile position to draw flag
temp1 = $20 ; TODO: can't we make these "subroutine-local" somehow?
temp2 = $21
temp3 = $22
chptr = $23

; *-------------------------------------------------------------------------*
; * User program *
; *-------------------------------------------------------------------------*
main:
  call cls ; Clears the LCD display
  call BattChkOff ; Turns off the low battery automatic detection function

start:
  set1 psw,1 ; Use ram bank 1
  xor acc
  st BTCR
  st testCaseIdLow
  st testCaseIdHigh

  ; Run test case 0
  call Test0_IEWriteIntDelay_01

  ; Done working. Wait for mode button (to exit)
  mov #0, BTCR
.checkMode: ; ** [M] (mode) Button Check **
  ld P3
  bn acc,6,.finish ; If the [M] button is pressed, the application ends

  jmp .checkMode ; Repeat

.finish: ; ** Application End Processing **
  call BattChkOn ; Turns on the low battery automatic detection function
  jmp game_end ; Application end

Test0_IEWriteIntDelay_01:
  ; Verify that writing IE causes an extra instruction delay before servicing an interrupt.
  xor acc

  clr1 IE,7 ; master interrupt disable
  mov #%00001100, BTCR ; set btint1 enable and source
  ld IE
  set1 IE, 7 ; enable interrupts
  st IE ; write to IE. Note we 'double store' to make this uniform with other checks
  mov #$f0, acc ; btint1 should run *after* this inst

  ; btint1 will xor the value of acc. So, we expect opposite bit pattern of $f0, when we get here.
  bne #$0f, .Fail

  set1 VSEL, 4 ; WRAM autoincrement on
  mov #0, XBNK
  mov #$80, 2
  mov #8, temp1        ; message character count
  ; Success case. Write this to screen. Eventually just record the result somewhere or similar..
  mov #<str_Passed, temp2   ; low part of message character address
  mov #>str_Passed, temp3   ; high part of message character address
  call PrintStringFlash
  ret

.Fail:
  ; Fail case. Write expected and actual values to screen.
  
  ; print hello world into the screen
  mov #<str_Failed, temp2   ; low part of message character address
  mov #>str_Failed, temp3   ; high part of message character address
  call PrintStringFlash

  ret

; *-------------------------------------------------------------------------*
; * Clearing the LCD Display Image *
; *-------------------------------------------------------------------------*
cls:
  push OCR ; Pushes the OCR value onto the stack
  mov #osc_rc,OCR ; Specifies the system clock

  mov #0,XBNK ; Specifies the display RAM bank address (BANK0)
  call .cls_s ; Clears the data in that bank

  mov #1,XBNK ; Specifies the display RAM bank address (BANK1)
  call .cls_s ; Clears the data in that bank
  pop OCR ; Pops the OCR value off of the stack

  ret ; cls end

.cls_s: ; *** Clearing One Bank of Display RAM ***
  mov #$80,r2 ; Points the indirect addressing register at the start of display RAM
  mov #$80,b ; Sets the number of loops in loop counter b
.loop3:
  mov #0,@r2 ; Writes "0" while incrementing the address
  inc r2 ;
  dbnz b,.loop3 ; Repeats until b is "0"

  ret ; cls_s end

PrintStringFlash:
    ; r2 and XBNK = XRAM location (make sure even line)
    ; temp1 = char count
    ; temp2 = flash address low
    ; temp3 = flash address high

    ; initialize WRAM
    ld temp1      ; load temp1 into ACC
    clr1 PSW, 7   ; clear carry flag
    rorc          ;
    addc #0       ;
    st 1          ; divide temp1 in half, rounding up, store result to r1
  call ClearCharCellsWRAM

    ; render text
    ld temp1      ; load temp1 into ACC
    st 1          ; store ACC to r1
    mov #0, chptr ; set chtptr to 0 (work ram pointer)
.StringLoop:
    ld temp2      ; load flash address low into ACC
    st TRL        ; store ACC to table reference register low
    ld temp3      ; load flash address high into ACC
    st TRH        ; store ACC to table reference register high
    ldf           ; read a character from flash
  call DrawChar
    inc temp2
    ld temp2
  bnz .NoCarry
    inc temp3
.NoCarry:
  dbnz 1, .StringLoop

    ; copy result
    ld temp1
    clr1 PSW, 7
    rorc
    addc #0
    st 1
  call PrintCharCells
  ret

ClearCharCellsWRAM: ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; initializes work RAM char cells because the rendering process involves OR masking
    ; r1 = char cell count (1 cell = 2 char)
    mov #0, VRMAD1  ;
    set1 VRMAD2, 0  ; start at address 0x100 of WRAM
.CleanLoop:
    xor ACC         ; store 0 to ACC
    st VTRBF        ; store ACC to WRAM and increment
    st VTRBF
    st VTRBF
    st VTRBF
    st VTRBF
    st VTRBF
  dbnz 1, .CleanLoop  ; decrement r1, repeat loop if r1 != 0
  ret



PrintCharCells: ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; renders char cells from WRAM into XRAM
    ; XBNK and r2 = position
    ; 1 = cell count

    ; C is "is even line" flag used to skip 4 empty bytes of XRAM after even lines
    mov #0, C           ;
    ld 2                ; load R2 into ACC
    and #%00001111      ; mask out 4 lower bits of ACC
    be #6, .here        ; check if ACC < #6, store into CY
.here
  bn PSW, 7, .evenline  ; handle xram 4-byte gaps after even lines
    inc C               ; if ACC was < 6, inc C (set C0)
.evenline:

    mov #0, VRMAD1      ; clear VRMAD 0-7 bits
    set1 VRMAD2, 0      ; set VRMAD high bit
.CopyLoop:
    mov #6, B           ; write #6 to B
.SubLoop:               ; copy a single byte of WRAM to XRAM
    ld VTRBF            ; read WRAM into ACC and auto-inc VRMAD
    st @r2              ; store ACC to address in r2
    ld 2                ; read r2 into ACC
  bp C, 0, .even        ; check C0
    add #4              ; if C0 is cleared: add 4 to ACC
.even:
    add #6              ; add 6 to ACC
    st 2                ; store ACC to r2
    not1 C, 0           ; flip C so that even/odd add behaviors are used for next line
  dbnz B, .SubLoop      ; 
    ld 2
    sub #$2F
    st 2
  dbnz 1, .CopyLoop
  ret



DrawChar: ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; renders one char to Work RAM
    ; chptr = location in WRAM (100 - 180)
    ; ACC = char to draw
  be #$20, .here0
.here0:
  bp PSW, 7, .BlankChar     ; draw blank char if ACC < $20 (non-printable ASCII chars)

  be #$80, .here1
.here1:
  bp PSW, 7, .EnglishChar   ; draw english char if ACC < $80 (printable ASCII chars)

  be #$A0, .here2
.here2:
  bp PSW, 7, .BlankChar     ; special case for non-breaking space $A0

    ; fail condition = blank char
.BlankChar: ; don't render anything
    inc chptr
  ret

.EnglishChar:
    mov #<En_Chars, TRL
    mov #>En_Chars, TRH     ; load address of english character graphics to TR
    sub #$20                ; subtract $20 from ACC (since the table starts with printable chars)
  br .Continue

.Continue: ; multiply index by 6 and add char table offset to TR
    st C
    mov #6, B
    xor ACC
    mul
    st B
    ld C
    add TRL
    st TRL
    ld B
    addc TRH
    st TRH

    set1 VRMAD2, 0 ; get current cell
    ld chptr
    clr1 PSW, 7
    rorc
    st C
    xor ACC
    mov #6, B

; mask new char data into the cell and store new result
    mov #6, 0
  bp PSW, 7, MaskRight ; check odd/even
MaskLeft: ; mask regular
    mul
    ld C
    st VRMAD1
.MaskLoop:
    ldf
    or VTRBF      ; read-in already filled pixels from VTRBF
    dec VRMAD1    ; dec to undo auto-increment
    st VTRBF      ; store combo of flash and WRAM pixels
    inc TRL
    ld TRL
  bnz .NoCarry
    inc TRH
.NoCarry
  dbnz 0, .MaskLoop
    inc chptr
  ret

MaskRight: ; mask with ROR
    mul
    ld C
    st VRMAD1
.MaskLoop:
    ldf
    ror
    ror
    ror
    ror
    or VTRBF
    dec VRMAD1
    st VTRBF
    inc TRL
    ld TRL
  bnz .NoCarry
    inc TRH
.NoCarry
  dbnz 0, .MaskLoop
    inc chptr
  ret


; *-------------------------------------------------------------------------*
; * Strings *
; *-------------------------------------------------------------------------*
str_Passed:
  .byte "Passed"

str_Failed:
  .byte "Failed"

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

En_Chars:
    .include sprite "JIS_EN.png"  header="no"
    .include sprite "JIS_EN2.png" header="no"
    .include sprite "JIS_EN3.png" header="no"

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
  xor #$ff ; Bitwise NOT the acc value
  mov #0, BTCR ; Disable base timer interrupt
  ret

; int_BaseTimer_Backup: ; old impl to review later
;   push PSW ; Pushes the PSW value onto the stack
;   push acc
;   push b
;   push c

;   set1 PSW,1 ; Selects data RAM bank 1

;   ; Draw the digit for 'flag' at position (b,c)
;   ld xpos
;   st c
;   ld ypos
;   st b
;   ld flag ; Draw the flag value to screen
;   call putch

;   inc flag ; Inc'ing the flag demonstrates we do not handle the interrupt again until flag is reset to 0
;   ; i.e. writing a 2 using this routine indicates a bug.

;   pop c
;   pop b
;   pop acc
;   pop PSW ; Pops the PSW value off of the stack

;   clr1 BTCR,1 ; Clears the base timer interrupt source
;   clr1 BTCR,3 ; Clears the base timer interrupt source
;   ret ; User interrupt processing end
