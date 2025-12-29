; From https://github.com/jvsTSX/VMU-MISC-CODE
;
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
    clr1 P3INT, 1
    mov #$FF, HaltCnt
  reti

    .org $1F0
goodbye:
    not1 EXT, 0   ; 
  jmpf goodbye    ; return to BIOS

;    /////////////////////////////////////////////////////////////
;   ///                    DREAMCAST HEADER                   ///
;  /////////////////////////////////////////////////////////////
    .org $200
    .byte "hello, world!   " ; ................... 16-byte Title
    .byte "by https://github.com/jvsTSX    " ; ... 32-byte Description

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
temp1 = $10
temp2 = $11
temp3 = $12
chptr = $13
HaltCnt = $14

Start:
    mov #00000101, P3INT ; enable joypad interrupt
    mov #0, T1CNT
    clr1 BTCR, 6

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

    ; print hello world into the screen
    set1 VSEL, 4 ; WRAM autoincrement on
    mov #0, XBNK
    mov #$80, 2
    mov #12, temp1        ; message character count
    mov #<String, temp2   ; low part of message character address
    mov #>String, temp3   ; high part of message character address
  call PrintStringFlash

Main: ; wait untill MODE is pressed
    ld P3
  bp ACC, 6, .NoMode
    set1 BTCR, 6      ; enable base timer count
    jmp goodbye       ;
.NoMode:
  dbnz HaltCnt, Main  ; dec HaltCnt and if not zero go back to start of Main
    set1 PCON, 0      ; HaltCnt was 0 time to enable halt mode (which waits for p3 interrupt)
  br Main             ; when we return from p3 interrupt go to start of main anyways to handle it



;    /////////////////////////////////////////////////////////////
;   ///                      SUBROUTINES                      ///
;  /////////////////////////////////////////////////////////////
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