;    /////////////////////////////////////////////////////////////
;   ///                        VECTORS                        ///
;  /////////////////////////////////////////////////////////////
    .org 0   ; entry point
  jmpf Start
    .org $03 ; External int. (INTO)                 - IO1CR
  reti
    .org $0B ; External int. (INT1)                 - IO1CR
  reti
    .org $13 ; External int. (INT2) and Timer 0 low - I23CR and T0CNT
  reti
    .org $1B ; External int. (INT3) and base timer  - I23CR and BTCR
    not1 countModeFlag,0  ; switch between counting and reporting mode
    clr1 BTCR,1 ; clear base timer interrupt 0 source
  reti
    .org $23 ; Timer 0 high                         - T0CNT
  reti
    .org $2B ; Timer 1 Low and High                 - T1CNT
  reti
    .org $33 ; Serial IO 1                          - SCON0
  reti
    .org $3B ; Serial IO 2                          - SCON1
  reti
    .org $43 ; VMU to VMU comms                     - not listed? (160h/161h)
  reti
    .org $4B ; Port 3 interrupt                     - P3INT
  reti

    .org $1F0
goodbye:
    not1 EXT, 0   ;
  jmpf goodbye    ; return to BIOS

    .org $130
BiosClockTick:
    not1 EXT,0
  jmpf BiosClockTick  ; call BIOS tick function
    ; TODO: where does the BIOS return us to when it gives control back?

;    /////////////////////////////////////////////////////////////
;   ///                    DREAMCAST HEADER                   ///
;  /////////////////////////////////////////////////////////////
    .org $200
    .byte "RcOscTest       " ; ................... 16-byte Title
    .byte "Test the RC Oscillator speed    " ; ... 32-byte Description

;    /////////////////////////////////////////////////////////////
;   ///                       GAME ICON                       ///
;  /////////////////////////////////////////////////////////////
    .org $240 ; >>> ICON HEADER
    .org $260 ; >>> PALETTE TABLE
    .org $280 ; >>> ICON DATA



;    /////////////////////////////////////////////////////////////
;   ///                       GAME CODE                       ///
;  /////////////////////////////////////////////////////////////

    .include "sfr.i"
temp1 = $10   ; message character count
temp2 = $11   ; low part of message character address
temp3 = $12   ; high part of message character address
chptr = $13   ; work RAM pointer (for building character graphics)
HaltCnt = $14

ticks0 = $15        ; low byte of counter
ticks1 = $16        ; mid byte of counter
ticks2 = $17        ; high byte of counter
countModeFlag = $18 ; counting mode: 0 to count, 1 to report

; store ASCII codes for each of the hex digits we will print
chars0 = $1A
chars1 = $1B
chars2 = $1C
chars3 = $1D
chars4 = $1E
chars5 = $1F

Start:
    mov #0, P3INT         ; disable joypad interrupt
    mov #%01000001, BTCR  ; enable base timer count and int0
    ; NB: DO NOT use set1 and similar instrs for write-only regs like VCCR.
    ; Only MOV, ST and POP are suitable.
	  mov #%10000001, OCR ; select RC clock /6

    ; initialize screen
    mov #$80, 2
    mov #0, XBNK
.Loop:
    xor ACC
    st @r2 ; line 1
    inc 2
    st @r2
    inc 2
    st @r2
    inc 2
    st @r2
    inc 2
    st @r2
    inc 2
    st @r2
    inc 2
    st @r2 ; line 2
    inc 2
    st @r2
    inc 2
    st @r2
    inc 2
    st @r2
    inc 2
    st @r2
    inc 2
    st @r2
    ld 2    ; load R2 into ACC
    add #5  ; add 5 to ACC
    st 2    ; store in R2. This skips the unused XRAM addresses
  bnz .Loop
  bp XBNK, 0, .LoopDone ; If second pass with XBNK1 was done
    inc XBNK
    mov #80, 2
  br .Loop
.LoopDone:

CountMode:
    ; setup counting mode
    mov #0, ticks0
    mov #0, ticks1
    mov #0, ticks2
    mov #ticks0, 0     ; R0 points to ticks0
    mov #ticks1, 1     ; R1 points to ticks1
    mov #1, countModeFlag
.CountOnce:
    inc ticks0                ; inc LSB
    bne @R0, #0, .NoOverflow  ; if no overflow, skip ahead
    inc ticks1                ; LSB overflowed, inc ticks1
    bne @R1, #0, .NoOverflow  ; if no ticks1 overflow, skip ahead
    inc ticks2                ; middle byte overflowed, inc ticks2
.NoOverflow:
    ld P3               ; check if MODE pressed
    bp ACC, 6, .NoMode
    jmpf goodbye        ; mode was pressed, exit.
.NoMode:
    bp countModeFlag, 0, .CountOnce   ; if still in count mode, keep counting

ReportMode:
    ; setup reporting
    set1 VSEL, 4 ; WRAM autoincrement on
    mov #0, XBNK          ; start at XRAM (video RAM) bank 0
    mov #$80, 2           ; start of XRAM address space
    mov #6, temp1         ; message character count

    ; Get most significant ascii hex value
    ; note: we could probably make this easier by laying out the 0-9, A-F graphics in order instead.
    ld ticks2          ; load ticks2 to ACC
    and #$f0              ; clear lower 4 bits
    ror                   ; place in lower pos
    ror
    ror
    ror
  call ConvertToAscii
    st chars0

    ; get smaller nybble of ticks2
    ld ticks2
    and #$f
  call ConvertToAscii
    st chars1

    ; note: we could probably make this easier by laying out the 0-9, A-F graphics in order instead.
    ld ticks1          ; load ticks1 to ACC
    and #$f0              ; clear lower 4 bits
    ror                   ; place in lower pos
    ror
    ror
    ror
  call ConvertToAscii
    st chars2

    ; get smaller nybble of ticks1
    ld ticks1
    and #$f
  call ConvertToAscii
    st chars3

    ; get bigger nybble of ticks0
    ld ticks0
    and #$f0
    ror
    ror
    ror
    ror
  call ConvertToAscii
    st chars4

    ; get smaller nybble of ticks0
    ld ticks0
    and #$f
  call ConvertToAscii
    st chars5

  ; ascii data setup now. Print the characters
  mov #chars0, 0  ; setup pointer to ascii char data
  call PrintStringRAM
.Halt:
    set1 PCON, 0            ; halt until a timer interrupt goes off
    bp countModeFlag, 0, CountMode  ; return to count mode if set
    br .Halt                ; not in count mode? wait to go into count mode

;    /////////////////////////////////////////////////////////////
;   ///                      SUBROUTINES                      ///
;  /////////////////////////////////////////////////////////////

ConvertToAscii:
    ; ACC = value between $0-$f.
    ; After returning, ACC contains the ASCII code for the value.
    be #$a, .CompareOnly
.CompareOnly:
    bp PSW, CY, .WriteDigit   ; CY is set when ACC < $a (0-9 digit case).
    br .WriteLetter           ; otherwise ACC >= $a
.WriteDigit:
    or #$30                   ; Put $3 in the bigger nybble and you have an ascii digit.
    ret
.WriteLetter:                 ; ACC contains a value $a-$f. Add $37 to move into range $41-$46.
    add #$37
    ret

PrintStringRAM:
    ; unchanged:
    ; temp1 = char count
    ;
    ; modifies:
    ; r0 = char pointer (ascii data)
    ; r1 (scratch)
    ; r2 and XBNK = XRAM location (make sure even line)

    ; initialize WRAM
    ld temp1      ; load temp1 into ACC
    clr1 PSW, 7   ; clear carry flag
    rorc          ;
    addc #0       ;
    st 1          ; divide temp1 in half, rounding up, store result
  call ClearCharCellsWRAM

    ; render text
    ld temp1      ;
    st 1          ; put char count in r1
    mov #0, chptr ; set chptr to 0 (work ram pointer)
.StringLoop:
    ld @r0        ; read current ascii character
  call DrawChar   ; draw character
    inc 0         ; inc char pointer
  dbnz 1, .StringLoop

    ; copy result
    ld temp1
    clr1 PSW, 7
    rorc
    addc #0       ; take char count, divide in half rounding up
    st 1
  call PrintCharCells
  ret

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
; uses regs: B, C, TRL, TRH, 5
  be #$20, .here0
.here0:
  bp PSW, 7, .BlankChar     ; draw blank char if ACC < $20 (non-printable ASCII chars)

  be #$80, .here1
.here1:
  bp PSW, 7, .EnglishChar   ; draw english char if ACC < $80 (printable ASCII chars)

  be #$A0, .here2
.here2:
  bp PSW, 7, .BlankChar     ; special case for non-breaking space $A0

  be #$E0, .here3
.here3:
  bp PSW, 7, .JapaneseChar

    ; fail condition = blank char
.BlankChar: ; don't render anything
    inc chptr
  ret

.EnglishChar:
    mov #<En_Chars, TRL
    mov #>En_Chars, TRH     ; load address of english character graphics to TR
    sub #$20                ; subtract $20 from ACC (since the table starts with printable chars)
  br .Continue

.JapaneseChar:
    mov #<Jp_Chars, TRL
    mov #>Jp_Chars, TRH
    sub #$A0

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
    mov #6, 5
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
  dbnz 5, .MaskLoop
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
  dbnz 5, .MaskLoop
    inc chptr
  ret



;    /////////////////////////////////////////////////////////////
;   ///                      DATA SPACE                       ///
;  /////////////////////////////////////////////////////////////
String:
    .byte "Hello,World!"

En_Chars:
    .include sprite "JIS_EN.png"  header="no"
    .include sprite "JIS_EN2.png" header="no"
    .include sprite "JIS_EN3.png" header="no"

Jp_Chars:
    .cnop 0, $200