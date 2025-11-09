
  ;; Entry point
  .org 0
  jmp   label_0200

  .byte $FF

  ;; Entry point
  .org $0003
  jmp   label_004F

  .byte $FF,$FF,$FF,$FF,$FF,$FF

  ;; Entry point
  .org $000B
  jmp   label_0052

  .byte $FF,$FF,$FF,$FF,$FF,$FF

  ;; Entry point
  .org $0013
  jmp   label_0055

  .byte $FF,$FF,$FF,$FF,$FF,$FF

  ;; Entry point
  .org $001B
  callf label_3DC9
  reti

  .byte $FF,$FF,$FF,$FF

  ;; Entry point
  .org $0023
  jmp   label_005A

  .byte $FF,$FF,$FF,$FF,$FF,$FF

  ;; Entry point
  .org $002B
  jmp   label_005D

  .byte $FF,$FF,$FF,$FF,$FF,$FF

  ;; Entry point
  .org $0033
  callf label_25E4
  reti

  .byte $FF,$FF,$FF,$FF

  ;; Entry point
  .org $003B
  callf label_260E
  reti

  .byte $FF,$FF,$FF,$FF

  ;; Entry point
  .org $0043
  callf label_05E8
  reti

  .byte $FF,$FF,$FF,$FF

  ;; Entry point
  .org $004B
  callf label_3E66
  reti
label_004F:
  clr1  i01cr, $01
  reti
label_0052:
  clr1  i01cr, $05
  reti
label_0055:
  clr1  t0con, $01
  clr1  i23cr, $05
  reti
label_005A:
  clr1  t0con, $03
  reti
label_005D:
  clr1  t1cnt, $01
  clr1  t1cnt, $03
  reti

  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$20,$3C ; .............. <
  .byte $B7,$B8,$0D,$21,$01,$05,$20,$3C,$B7,$B8,$0D,$21,$01,$0B,$20,$E0 ; ..\r!.. <..\r!.. .
  .byte $1B,$B8,$0D,$21,$01,$15,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$20,$E0 ; ..\r!.......... .
  .byte $27,$B8,$0D,$21,$01,$25,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF ; '.\r!.%........

  ;; Entry point
  .org $0130
  callf label_3DC9
  not1  ext, $00
  jmpf  $0139

  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$20,$04,$21,$B8,$0D,$21,$01,$45 ; ........ .!.\r!.E
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$11,$6A,$3A,$FF,$FF,$FF,$FF,$FF ; .........j:.....
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF

label_0200:
  mov   #$A3, ocr
  mov   #$7F, sp
  clr1  psw, rambk0
  clr1  p3int, $00
  mov   #$FF, $6E
  mov   #$FF, $67
  callf label_3BA9
  callf label_338F
  callf label_33E6
  ld    $70
  be    #$F0, label_0220
  br    label_024C
label_0220:
  mov   #$04, b
  mov   #$00, acc
  mov   #$2C, $00
label_0229:
  push  b
  push  acc
  ld    @R0
  inc   $00
  st    b
  pop   acc
  push  acc
  mov   #$C3, trl
  mov   #$05, trh
  ldc
  sub   b
  bnz   label_024C
  pop   acc
  inc   acc
  pop   b
  dbnz  b, label_0229
  br    label_0294
label_024C:
  mov   #$7F, sp
  mov   #$02, c
  clr1  psw, rambk0
  mov   #$00, acc
label_0257:
  mov   #$00, $00
  mov   #$40, b
label_025D:
  st    @R0
  inc   $00
  st    @R0
  inc   $00
  st    @R0
  inc   $00
  st    @R0
  inc   $00
  dbnz  b, label_025D
  mov   #$00, $00
  set1  psw, rambk0
  dbnz  c, label_0257
  clr1  psw, rambk0
  mov   #$2C, $00
  mov   #$00, acc
  mov   #$04, b
label_027F:
  push  acc
  mov   #$C3, trl
  mov   #$05, trh
  ldc
  st    @R0
  pop   acc
  inc   $00
  inc   acc
  dbnz  b, label_027F
  clr1  psw, rambk0
label_0294:
  mov   #$00, $70
  mov   #$00, $67
  callf label_1007
  callf label_1017
  clr1  vsel, $00
  clr1  vsel, $01
  clr1  mplsw, $02
  mov   #$00, $6E
  mov   #$00, $30
  mov   #$00, $35
label_02AF:
  ld    $31
  bnz   label_02EF
  mov   #$00, $6C
  callf label_1253
  ld    p7
  and   #$01
  bz    label_02C2
  jmpf  label_3EA7
label_02C2:
  push  ocr
  mov   #$81, ocr
  mov   #$02, xbnk
  mov   #$00, xram_0181
  mov   #$00, xram_0182
  mov   #$04, xram_0183
  mov   #$00, xram_0184
  mov   #$00, xbnk
  pop   ocr
  mov   #$40, $35
  callf label_129D
  bnz   label_02EF
  mov   #$FF, $31
  mov   #$00, $6E
  mov   #$00, $30
  mov   #$00, $35
label_02EF:
  clr1  psw, rambk0
  clr1  p3int, $00
  ld    p7
  and   #$01
  bnz   label_0337
  ld    $31
  bz    label_02AF
  callf label_3BA9
  mov   #$C7, trl
  mov   #$05, trh
  mov   #$00, $60
  mov   #$00, $61
  callf label_3A05
  set1  p3int, $00
  mov   #$01, $33
  mov   #$00, $34
label_0317:
  set1  pcon, $00
  ld    p7
  and   #$01
  bnz   label_0337
  ld    $33
  bz    label_0333
  ld    $35
  bnz   label_0333
  ld    $30
  bz    label_033A
  be    #$01, label_0360
  be    #$02, label_0387
  br    label_0317
label_0333:
  call  label_0421
  br    label_0317
label_0337:
  jmpf  label_3EA7
label_033A:
  ld    $70
  and   #$10
  bnz   label_035E
  ld    $34
  bz    label_0317
  mov   #$00, $34
  push  ocr
  mov   #$81, ocr
  mov   #$02, xbnk
  not1  xram_0181, $06
  mov   #$00, xram_0182
  mov   #$00, xram_0183
  mov   #$00, xbnk
  pop   ocr
  br    label_0317
label_035E:
  jmp   label_03AE
label_0360:
  ld    $70
  and   #$10
  bnz   label_0384
  ld    $34
  bz    label_0317
  mov   #$00, $34
  push  ocr
  mov   #$81, ocr
  mov   #$02, xbnk
  not1  xram_0182, $04
  mov   #$00, xram_0181
  mov   #$00, xram_0183
  mov   #$00, xbnk
  pop   ocr
  br    label_0317
label_0384:
  jmpf  label_3C12
label_0387:
  ld    $70
  and   #$10
  bnz   label_03AB
  ld    $34
  bz    label_0317
  mov   #$00, $34
  push  ocr
  mov   #$81, ocr
  mov   #$02, xbnk
  not1  xram_0183, $02
  mov   #$00, xram_0181
  mov   #$00, xram_0182
  mov   #$02, xbnk
  pop   ocr
  jmp   label_0317
label_03AB:
  jmpf  label_1034
label_03AE:
  push  ocr
  mov   #$81, ocr
  mov   #$02, xbnk
  mov   #$40, xram_0181
  mov   #$00, xram_0182
  mov   #$00, xram_0183
  mov   #$00, xbnk
  pop   ocr
  mov   #$01, fpr
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$00, trl
  mov   #$10, b
label_03D3:
  ldf
  xor   #$55
  bnz   label_03E1
  dbnz  b, label_03D3
  mov   #$00, $2B
  jmpf  label_196F
label_03E1:
  clr1  p3int, $00
  callf label_3C07
  mov   #$0E, trl
  mov   #$04, trh
  mov   #$00, $60
  mov   #$00, $61
  callf label_3A05
  set1  p3int, $00
  mov   #$01, $33
label_03FA:
  callf label_3696
  be    #$FF, label_0408
  be    #$FD, label_0408
  be    #$FE, label_040A
  br    label_03FA
label_0408:
  jmp   label_02EF
label_040A:
  call  label_0421
  br    label_03FA

  .byte $46,$6F,$72,$6D,$61,$74,$20,$20,$0D,$0A,$20,$20,$6D,$65,$6D,$6F ; Format  \r\n  memo
  .byte $72,$79,$00 ; ry.

label_0421:
  push  acc
  push  psw
  clr1  psw, rambk0
  push  $30
  push  $6E
  mov   #$FF, $6E
  mov   #$00, vccr
  mov   #$00, mcr
  callf label_3675
  set1  p3int, $00
label_0439:
  set1  pcon, $00
  ld    p7
  and   #$01
  bnz   label_045C
  ld    $70
  and   #$80
  bz    label_0439
  mov   #$09, mcr
  mov   #$80, vccr
  mov   #$00, $35
  mov   #$01, $33
  pop   $6E
  pop   $30
  pop   psw
  pop   acc
  ret
label_045C:
  mov   #$09, mcr
  mov   #$80, vccr
  mov   #$00, $35
  mov   #$00, $30
  mov   #$FF, $6E
  clr1  psw, rambk0
  mov   #$7F, sp
  jmpf  label_3EA7

  .byte $7C,$C6,$C6,$C6,$C6,$C6,$7C,$00,$18,$38,$18,$18,$18,$18,$3C,$00 ; |.....|..8....<.
  .byte $7C,$C6,$C6,$1C,$70,$C0,$FE,$00,$7C,$C6,$C6,$1C,$C6,$C6,$7C,$00 ; |...p...|.....|.
  .byte $C0,$D8,$D8,$D8,$FE,$18,$3C,$00,$FE,$C0,$FC,$06,$C6,$C6,$7C,$00 ; ......<.......|.
  .byte $7C,$C6,$C0,$FC,$C6,$C6,$7C,$00,$7E,$C6,$C6,$06,$06,$06,$06,$00 ; |.....|.~.......
  .byte $7C,$C6,$C6,$7C,$C6,$C6,$7C,$00,$7C,$C6,$C6,$7E,$06,$C6,$7C,$00 ; |..|..|.|..~..|.
  .byte $00,$30,$30,$00,$30,$30,$00,$00,$00,$FE,$7C,$38,$10,$00,$00,$00 ; .00.00....|8....
  .byte $00,$00,$10,$38,$7C,$FE,$00,$00,$02,$04,$08,$10,$20,$40,$80,$00 ; ...8|....... @..
  .byte $02,$04,$08,$10,$20,$40,$80,$00,$02,$04,$08,$10,$20,$40,$80,$00 ; .... @...... @..
  .byte $38,$6C,$C6,$C6,$FE,$C6,$C6,$00,$FC,$C6,$C6,$FC,$C6,$C6,$FC,$00 ; 8l..............
  .byte $7C,$C6,$C6,$C0,$C6,$C6,$7C,$00,$FC,$C6,$C6,$C6,$C6,$C6,$FC,$00 ; |.....|.........
  .byte $FC,$C0,$C0,$F8,$C0,$C0,$FC,$00,$FC,$C0,$C0,$F8,$C0,$C0,$C0,$00
  .byte $7C,$C6,$C6,$C0,$DE,$C6,$7E,$00,$C6,$C6,$C6,$FE,$C6,$C6,$C6,$00 ; |.....~.........
  .byte $78,$30,$30,$30,$30,$30,$78,$00,$06,$06,$06,$06,$66,$66,$3C,$00 ; x00000x.....ff<.
  .byte $CC,$CC,$D8,$F0,$D8,$CC,$CC,$00,$C0,$C0,$C0,$C0,$C0,$C0,$FC,$00
  .byte $C6,$EE,$FE,$D6,$C6,$C6,$C6,$00,$C6,$E6,$F6,$DE,$CE,$C6,$C6,$00
  .byte $38,$6C,$C6,$C6,$C6,$6C,$38,$00,$FC,$C6,$C6,$FC,$C0,$C0,$C0,$00 ; 8l...l8.........
  .byte $38,$6C,$C6,$C6,$D6,$6C,$36,$00,$FC,$C6,$C6,$FC,$C6,$C6,$C6,$00 ; 8l...l6.........
  .byte $7C,$C6,$C0,$7C,$06,$C6,$7C,$00,$7E,$18,$18,$18,$18,$18,$18,$00 ; |..|..|.~.......
  .byte $C6,$C6,$C6,$C6,$C6,$C6,$7C,$00,$C6,$C6,$C6,$6C,$6C,$38,$10,$00 ; ......|....ll8..
  .byte $C6,$C6,$D6,$D6,$FE,$EE,$C6,$00,$C6,$C6,$6C,$38,$6C,$C6,$C6,$00 ; ..........l8l...
  .byte $CC,$CC,$CC,$78,$30,$30,$30,$00,$FE,$06,$0C,$38,$60,$C0,$FE,$00 ; ...x000....8`...
  .byte $52,$45,$53,$54,$20,$20,$17,$18,$19,$1A,$0D,$0A,$20,$56,$69,$73 ; REST  ....\r\n Vis
  .byte $75,$61,$6C,$0D,$0A,$20,$4D,$65,$6D,$6F,$72,$79,$0D,$0A,$20,$20 ; ual\r\n Memory\r\n  
  .byte $55,$6E,$69,$74,$00 ; Unit.

label_05E8:
  clr1  vsel, $00
  push  acc
  push  b
  push  c
  push  $00
  push  $01
  push  $02
  push  $03
  ld    mplsta
  and   #$76
  st    b
  bp    acc, $05, label_062F
  bp    acc, $04, label_062F
  bp    acc, $01, label_062F
  ld    $22
  bnz   label_063F
  mov   #$03, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  be    #$01, label_0617
  jmp   label_07D8
label_0617:
  mov   #$02, vrmad1
  ld    vtrbf
  st    c
  and   #$3F
  sub   $20
  bz    label_0626
  jmp   label_07D8
label_0626:
  ld    c
  st    $20
  mov   #$FF, $22
  jmp   label_0646
label_062F:
  ld    $22
  bnz   label_0635
  jmp   label_07D8
label_0635:
  call  label_07EB
  bz    label_063B
  jmp   label_07D8
label_063B:
  call  label_0800
  jmp   label_07D8
label_063F:
  ld    $22
  bne   #$55, label_0646
  jmp   label_07D8
label_0646:
  call  label_07EB
  bz    label_064C
  jmp   label_07D8
label_064C:
  mov   #$03, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  bne   #$01, label_066A
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  bnz   label_0666
  mov   #$01, $24
  call  label_08B9
  jmp   label_07D8
label_0666:
  call  label_0800
  jmp   label_07D8
label_066A:
  bne   #$02, label_0681
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  bnz   label_067D
  mov   #$02, $24
  call  label_090F
  jmp   label_07D8
label_067D:
  call  label_0800
  jmp   label_07D8
label_0681:
  bne   #$03, label_0698
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  bnz   label_0694
  mov   #$FF, $24
  call  label_0962
  jmp   label_07D8
label_0694:
  call  label_0800
  jmp   label_07D8
label_0698:
  bne   #$04, label_06AF
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  bnz   label_06AB
  mov   #$FF, $24
  call  label_0970
  jmp   label_07D8
label_06AB:
  call  label_0800
  jmp   label_07D8
label_06AF:
  bne   #$0A, label_06D7
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  bne   #$02, label_06D3
  mov   #$0A, $24
  call  label_0827
  ld    $29
  be    #$02, label_06CB
  be    #$04, label_06CF
  jmp   label_07D4
label_06CB:
  call  label_09BB
  jmp   label_07D8
label_06CF:
  call  label_0A6A
  jmp   label_07D8
label_06D3:
  call  label_0800
  jmp   label_07D8
label_06D7:
  bne   #$0B, label_0735
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  bne   #$02, label_0731
  mov   #$0B, $24
  call  label_0827
  ld    $29
  be    #$02, label_06F7
  be    #$08, label_06F3
  jmp   label_07D4
label_06F3:
  call  label_0BFE
  jmp   label_07D8
label_06F7:
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  set1  psw, rambk0
  mov   #$70, $00
  ld    vtrbf
  st    @R0
  inc   $00
  mov   #$04, vrmad1
  mov   #$02, b
label_070C:
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  dbnz  b, label_070C
  clr1  psw, rambk0
  mov   #$00, $25
  mov   #$00, $27
  call  label_0858
  call  label_0B7D
  jmp   label_07D8
label_0731:
  call  label_0800
  jmp   label_07D8
label_0735:
  bne   #$0C, label_0756
  mov   #$0C, $24
  call  label_0827
  ld    $29
  be    #$04, label_074C
  be    #$08, label_0750
  bne   #$02, label_0754
  call  label_0C5B
  jmp   label_07D8
label_074C:
  call  label_0D02
  jmp   label_07D8
label_0750:
  call  label_0DBD
  jmp   label_07D8
label_0754:
  jmp   label_07D4
label_0756:
  bne   #$0D, label_0787
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  be    #$02, label_0767
  call  label_0800
  jmp   label_07D8
label_0767:
  mov   #$0D, $24
  call  label_0827
  ld    $29
  bne   #$02, label_0785
  mov   #$0A, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  sub   $25
  bz    label_077E
  set1  $27, $01
label_077E:
  mov   #$00, $25
  call  label_0E5D
  jmp   label_07D8
label_0785:
  jmp   label_07D4
label_0787:
  bne   #$FC, label_078E
  call  label_0E6F
  jmp   label_07D8
label_078E:
  bne   #$09, label_07AF
  mov   #$09, $24
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  bne   #$01, label_07A9
  call  label_0827
  ld    $29
  bne   #$08, label_07AD
  call  label_0E08
  jmp   label_07D8
label_07A9:
  call  label_0800
  jmp   label_07D8
label_07AD:
  jmp   label_07D4
label_07AF:
  bne   #$0E, label_07D0
  mov   #$0E, $24
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  bne   #$02, label_07CA
  call  label_0827
  ld    $29
  bne   #$08, label_07CE
  call  label_0E3A
  jmp   label_07D8
label_07CA:
  call  label_0800
  jmp   label_07D8
label_07CE:
  jmp   label_07D4
label_07D0:
  call  label_097B
  jmp   label_07D8
label_07D4:
  call  label_099B
  jmp   label_07D8
label_07D8:
  clr1  mplsta, $02
  pop   $03
  pop   $02
  pop   $01
  pop   $00
  pop   c
  pop   b
  pop   acc
  set1  vsel, $00
  ret
label_07EB:
  mov   #$02, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  st    b
  mov   #$01, vrmad1
  ld    vtrbf
  st    $21
  ld    $20
  sub   b
  ret
label_0800:
  call  label_081D
  mov   #$00, vtrbf
  ld    $20
  st    vtrbf
  ld    $21
  st    vtrbf
  mov   #$FC, vtrbf
  mov   #$FC, $24
  mov   #$00, vlreg
  set1  vsel, $00
  set1  mplsw, $04
  set1  mplsw, $00
  ret
label_081D:
  clr1  vsel, $00
  clr1  mplsw, $04
  mov   #$00, vrmad1
  set1  vrmad2, $00
  ret
label_0827:
  mov   #$05, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  bnz   label_0854
  ld    vtrbf
  bnz   label_0854
  ld    vtrbf
  bnz   label_0854
  mov   #$04, vrmad1
  ld    vtrbf
  be    #$04, label_084B
  be    #$08, label_0850
  bne   #$02, label_0854
  mov   #$02, $29
  br    label_0853
label_084B:
  mov   #$04, $29
  br    label_0853
label_0850:
  mov   #$08, $29
label_0853:
  ret
label_0854:
  mov   #$FF, $29
  ret
label_0858:
  set1  psw, rambk0
  mov   #$78, $00
  ld    @R0
  bz    label_0864
  clr1  psw, rambk0
  set1  $27, $00
label_0864:
  clr1  psw, rambk0
  ld    $25
  set1  psw, rambk0
  dec   $00
  sub   @R0
  bz    label_0876
  clr1  psw, rambk0
  set1  $27, $01
  mov   #$FE, $25
label_0876:
  clr1  psw, rambk0
  inc   $25
  set1  psw, rambk0
  dec   $00
  ld    @R0
  bz    label_0887
  clr1  psw, rambk0
  set1  $27, $02
  set1  psw, rambk0
label_0887:
  clr1  psw, rambk0
  ld    $27
  bnz   label_08B8
  set1  psw, rambk0
  dec   $00
  ld    @R0
  clr1  psw, cy
  rolc
  rolc
  st    b
  and   #$01
  st    $7D
  ld    b
  and   #$FC
  mov   #$77, $00
  or    @R0
  st    c
  and   #$00
  rolc
  mov   #$80, b
  mul
  st    $7E
  ld    c
  st    $7F
  mov   #$01, $7C
  clr1  psw, rambk0
label_08B8:
  ret
label_08B9:
  call  label_081D
  mov   #$00, $25
  mov   #$1C, vtrbf
  ld    $20
  st    vtrbf
  ld    $21
  st    vtrbf
  mov   #$05, vtrbf
  mov   #$95, trl
  mov   #$0A, trh
  mov   #$00, acc
  mov   #$03, vlreg
  mov   #$03, b
  call  label_0F4A
  set1  vsel, $00
  set1  mplsw, $00
  call  label_0FBF
  mov   #$06, c
label_08E6:
  clr1  vsel, $00
  mov   #$00, vrmad1
  mov   #$04, b
  call  label_0F4A
  set1  vsel, $00
  set1  mplsw, $01
  call  label_0FBF
  dbnz  c, label_08E6
  clr1  vsel, $00
  mov   #$00, vrmad1
  mov   #$00, vlreg
  mov   #$01, b
  call  label_0F4A
  set1  vsel, $00
  set1  mplsw, $04
  set1  mplsw, $01
  call  label_0FBF
  ret
label_090F:
  call  label_081D
  mov   #$00, $25
  mov   #$30, vtrbf
  ld    $20
  st    vtrbf
  ld    $21
  st    vtrbf
  mov   #$06, vtrbf
  mov   #$95, trl
  mov   #$0A, trh
  mov   #$00, acc
  mov   #$03, vlreg
  mov   #$03, b
  call  label_0F4A
  set1  vsel, $00
  set1  mplsw, $00
  call  label_0FBF
  mov   #$0B, c
label_093C:
  clr1  vsel, $00
  mov   #$00, vrmad1
  mov   #$04, b
  call  label_0F4A
  set1  vsel, $00
  set1  mplsw, $01
  call  label_0FBF
  dbnz  c, label_093C
  clr1  vsel, $00
  mov   #$00, vrmad1
  mov   #$01, b
  call  label_0F4A
  set1  vsel, $00
  set1  mplsw, $04
  set1  mplsw, $01
  call  label_0FBF
  ret
label_0962:
  call  label_081D
  mov   #$00, $25
  call  label_0F32
  callf label_1017
  callf label_3C07
  ret
label_0970:
  call  label_081D
  mov   #$00, $25
  call  label_0F32
  mov   #$55, $22
  ret
label_097B:
  call  label_081D
  mov   #$00, $25
  mov   #$00, vtrbf
  ld    $20
  st    vtrbf
  ld    $21
  st    vtrbf
  mov   #$FD, vtrbf
  mov   #$FD, $24
  mov   #$00, vlreg
  set1  vsel, $00
  set1  mplsw, $04
  set1  mplsw, $00
  ret
label_099B:
  call  label_081D
  mov   #$00, $25
  mov   #$00, vtrbf
  ld    $20
  st    vtrbf
  ld    $21
  st    vtrbf
  mov   #$FE, vtrbf
  mov   #$FE, $24
  mov   #$00, vlreg
  set1  vsel, $00
  set1  mplsw, $04
  set1  mplsw, $00
  ret
label_09BB:
  call  label_081D
  mov   #$00, $25
  mov   #$07, vtrbf
  ld    $20
  st    vtrbf
  ld    $21
  st    vtrbf
  mov   #$08, vtrbf
  mov   #$07, vlreg
  mov   #$00, acc
  mov   #$01, fpr
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$00, trl
  mov   #$10, b
label_09E3:
  ldf
  bne   #$55, label_0A38
  dbnz  b, label_09E3
  mov   #$03, vlreg
  mov   #$59, trl
  mov   #$0B, trh
  mov   #$00, acc
  mov   #$02, b
  call  label_0F4A
  mov   #$01, fpr
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$47, trl
  ldf
  st    vtrbf
  dec   trl
  ldf
  st    vtrbf
  mov   #$00, vtrbf
  mov   #$FF, vtrbf
  set1  vsel, $00
  set1  mplsw, $00
  call  label_0FBF
  mov   #$01, fpr
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$48, trl
  clr1  vsel, $00
  mov   #$00, vrmad1
  mov   #$04, b
  call  label_0F95
  set1  vsel, $00
  set1  mplsw, $04
  set1  mplsw, $01
  ret
label_0A38:
  mov   #$59, trl
  mov   #$0B, trh
  mov   #$00, acc
  mov   #$03, vlreg
  mov   #$03, b
  call  label_0F4A
  set1  vsel, $00
  set1  mplsw, $00
  call  label_0FBF
  clr1  vsel, $00
  mov   #$00, vrmad1
  mov   #$03, vlreg
  mov   #$04, b
  call  label_0F4A
  set1  vsel, $00
  set1  mplsw, $04
  set1  mplsw, $01
  call  label_0FBF
  clr1  vsel, $00
  mov   #$00, vrmad1
  ret
label_0A6A:
  call  label_081D
  mov   #$00, $25
  mov   #$02, vtrbf
  ld    $20
  st    vtrbf
  ld    $21
  st    vtrbf
  mov   #$08, vtrbf
  mov   #$02, vlreg
  mov   #$75, trl
  mov   #$0B, trh
  mov   #$00, acc
  mov   #$02, b
  call  label_0F4A
  set1  vsel, $00
  set1  mplsw, $04
  set1  mplsw, $00
  ret

  .byte $00,$00,$00,$0E,$7E,$7E,$3F,$40,$00,$05,$10,$00,$00,$0F,$41,$00 ; ....~~?@......A.
  .byte $FF,$00,$56,$69,$73,$75,$61,$6C,$20,$4D,$65,$6D,$6F,$72,$79,$20 ; ..Visual Memory 
  .byte $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20 ;                 
  .byte $50,$72,$6F,$64,$75,$63,$65,$64,$20,$42,$79,$20,$6F,$72,$20,$55 ; Produced By or U
  .byte $6E,$64,$65,$72,$20,$4C,$69,$63,$65,$6E,$73,$65,$20,$46,$72,$6F ; nder License Fro
  .byte $6D,$20,$53,$45,$47,$41,$20,$45,$4E,$54,$45,$52,$50,$52,$49,$53 ; m SEGA ENTERPRIS
  .byte $45,$53,$2C,$4C,$54,$44,$2E,$20,$20,$20,$20,$20,$7C,$00,$82,$00 ; ES,LTD.     |...
  .byte $56,$65,$72,$73,$69,$6F,$6E,$20,$31,$2E,$30,$30,$35,$2C,$31,$39 ; Version 1.005,19
  .byte $39,$39,$2F,$31,$30,$2F,$32,$36,$2C,$33,$31,$35,$2D,$36,$32,$30 ; 99/10/26,315-620
  .byte $38,$2D,$30,$35,$2C,$53,$45,$47,$41,$20,$56,$69,$73,$75,$61,$6C ; 8-05,SEGA Visual
  .byte $20,$4D,$65,$6D,$6F,$72,$79,$20,$53,$79,$73,$74,$65,$6D,$20,$42 ;  Memory System B
  .byte $49,$4F,$53,$20,$50,$72,$6F,$64,$75,$63,$65,$64,$20,$62,$79,$20 ; IOS Produced by 
  .byte $53,$75,$65,$20,$00,$00,$00,$02,$FF,$00,$00,$00,$FF,$00,$FE,$00 ; Sue ............
  .byte $01,$00,$FD,$00,$0D,$00,$00,$00,$C8,$00,$1F,$00,$00,$00,$80,$00
  .byte $00,$00,$00,$04,$2F,$1F,$10,$02 ; ..../...

label_0B7D:
  ld    $27
  bnz   label_0BFB
  mov   #$00, $25
  call  label_081D
  mov   #$82, vtrbf
  ld    $20
  st    vtrbf
  ld    $21
  st    vtrbf
  mov   #$08, vtrbf
  mov   #$02, vlreg
  set1  psw, rambk0
  mov   #$71, $00
  mov   #$02, b
  clr1  psw, rambk0
  call  label_0F79
  set1  vsel, $00
  set1  mplsw, $00
  call  label_0FBF
  set1  psw, rambk0
  ld    $7D
  clr1  psw, rambk0
  st    fpr
  st    $6F
  set1  psw, rambk0
  ld    $7E
  st    trh
  ld    $7F
  st    trl
  clr1  psw, rambk0
  mov   #$03, vlreg
  mov   #$1F, c
  mov   #$02, xbnk
  mov   #$01, xram_0184
label_0BCB:
  clr1  vsel, $00
  mov   #$00, vrmad1
  mov   #$04, b
  call  label_0F95
  ld    trl
  bnz   label_0BDB
  inc   trh
label_0BDB:
  set1  vsel, $00
  set1  mplsw, $01
  call  label_0FBF
  dbnz  c, label_0BCB
  clr1  vsel, $00
  mov   #$00, vrmad1
  mov   #$04, b
  call  label_0F95
  set1  vsel, $00
  set1  mplsw, $04
  set1  mplsw, $01
  mov   #$00, xram_0184
  mov   #$00, xbnk
  ret
label_0BFB:
  call  label_0EDE
  ret
label_0BFE:
  mov   #$08, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  bnz   label_0C58
  ld    vtrbf
  bnz   label_0C58
  ld    vtrbf
  bnz   label_0C58
  ld    vtrbf
  bnz   label_0C58
  call  label_081D
  mov   #$03, vtrbf
  ld    $20
  st    vtrbf
  ld    $21
  st    vtrbf
  mov   #$08, vtrbf
  mov   #$03, vlreg
  mov   #$08, vtrbf
  mov   #$00, acc
  st    vtrbf
  st    vtrbf
  st    vtrbf
  ld    $1A
  st    vtrbf
  ld    $19
  st    vtrbf
  ld    $17
  st    vtrbf
  ld    $18
  st    vtrbf
  mov   #$00, vtrbf
  ld    $1D
  st    vtrbf
  ld    $1C
  st    vtrbf
  ld    $1B
  st    vtrbf
  set1  vsel, $00
  set1  mplsw, $04
  set1  mplsw, $00
  ret
label_0C58:
  call  label_0800
  ret
label_0C5B:
  set1  psw, rambk0
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  mov   #$70, $00
  mov   #$02, b
  ld    vtrbf
  st    @R0
  inc   $00
  mov   #$04, vrmad1
label_0C70:
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  dbnz  b, label_0C70
  mov   #$80, $00
  mov   #$20, b
label_0C8D:
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  dbnz  b, label_0C8D
  clr1  psw, rambk0
  call  label_081D
  call  label_0F32
  mov   #$02, xbnk
  mov   #$01, xram_0184
  ld    $25
  bnz   label_0CBF
  set1  psw, rambk0
  ld    $75
  clr1  psw, rambk0
  st    $26
  mov   #$00, $27
label_0CBF:
  set1  psw, rambk0
  ld    $75
  clr1  psw, rambk0
  sub   $26
  bz    label_0CCB
  set1  $27, $02
label_0CCB:
  set1  psw, rambk0
  ld    $70
  clr1  psw, rambk0
  be    #$22, label_0CD6
  set1  $27, $04
label_0CD6:
  call  label_0858
  ld    $27
  bnz   label_0D01
  set1  psw, rambk0
  mov   #$80, $00
  mov   #$20, c
  clr1  psw, rambk0
  call  label_0FC9
  set1  psw, rambk0
  clr1  ie, $07
  callf label_E024
  set1  ie, $07
  callf label_E01B
  ld    $00
  bz    label_0CFE
  clr1  psw, rambk0
  set1  $27, $03
  set1  psw, rambk0
label_0CFE:
  clr1  psw, rambk0
  ret
label_0D01:
  ret
label_0D02:
  mov   #$00, $28
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  be    #$32, label_0D15
  ld    $28
  or    #$10
  st    $28
label_0D15:
  mov   #$0B, vrmad1
  ld    vtrbf
  bz    label_0D22
  ld    $28
  or    #$01
  st    $28
label_0D22:
  mov   #$0A, vrmad1
  ld    vtrbf
  bz    label_0D2F
  ld    $28
  or    #$02
  st    $28
label_0D2F:
  mov   #$08, vrmad1
  ld    vtrbf
  bnz   label_0D3A
  ld    vtrbf
  bz    label_0D40
label_0D3A:
  ld    $28
  or    #$04
  st    $28
label_0D40:
  ld    $28
  bz    label_0D48
  call  label_0F08
  jmp   label_0DBA
label_0D48:
  set1  psw, rambk0
  mov   #$40, $00
  mov   #$0C, vrmad1
  mov   #$30, b
label_0D53:
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  dbnz  b, label_0D53
  clr1  psw, rambk0
  call  label_081D
  call  label_0F32
  set1  psw, rambk0
  mov   #$40, $00
  mov   #$30, c
  clr1  psw, rambk0
  call  label_0FC9
  set1  psw, rambk0
  mov   #$40, $00
  mov   #$00, xbnk
  mov   #$02, $01
label_0D87:
  mov   #$08, b
  mov   #$80, $02
label_0D8D:
  mov   #$03, c
label_0D90:
  ld    @R0
  st    @R2
  inc   $00
  inc   $02
  ld    @R0
  st    @R2
  inc   $00
  inc   $02
  ld    @R0
  st    @R2
  inc   $00
  inc   $02
  ld    @R0
  st    @R2
  inc   $00
  inc   $02
  dbnz  c, label_0D90
  ld    $02
  add   #$04
  st    $02
  dbnz  b, label_0D8D
  mov   #$01, xbnk
  dbnz  $01, label_0D87
label_0DBA:
  clr1  psw, rambk0
  ret
label_0DBD:
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  bne   #$04, label_0E05
  mov   #$08, vrmad1
  ld    vtrbf
  bnz   label_0E05
  ld    vtrbf
  bnz   label_0E05
  ld    vtrbf
  bnz   label_0E05
  ld    vtrbf
  bnz   label_0E05
  ld    vtrbf
  st    $1A
  ld    vtrbf
  st    $19
  ld    vtrbf
  st    $17
  ld    vtrbf
  st    $18
  ld    vtrbf
  ld    vtrbf
  st    $1D
  ld    vtrbf
  st    $1C
  ld    vtrbf
  st    $1B
  call  label_081D
  call  label_0F32
  callf label_E02D
  clr1  psw, rambk0
  mov   #$FF, $31
  ret
label_0E05:
  call  label_0800
  ret
label_0E08:
  call  label_081D
  mov   #$02, vtrbf
  ld    $20
  st    vtrbf
  ld    $21
  st    vtrbf
  mov   #$08, vtrbf
  mov   #$02, vlreg
  mov   #$08, vtrbf
  mov   #$00, acc
  st    vtrbf
  st    vtrbf
  st    vtrbf
  st    vtrbf
  st    vtrbf
  st    vtrbf
  ld    p3
  or    #$C0
  st    vtrbf
  set1  vsel, $00
  set1  mplsw, $04
  set1  mplsw, $00
  ret
label_0E3A:
  mov   #$0A, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  st    b
  ld    vtrbf
  st    c
  mov   #$00, t1cnt
  mov   #$FF, acc
  sub   c
  st    t1lr
  add   b
  st    t1lc
  mov   #$50, t1cnt
  call  label_081D
  call  label_0F32
  ret
label_0E5D:
  mov   #$02, xbnk
  mov   #$00, xram_0184
  ld    $27
  bnz   label_0E6C
  call  label_081D
  call  label_0F32
  ret
label_0E6C:
  call  label_0EDE
  ret
label_0E6F:
  ld    $24
  bnz   label_0E75
  br    label_0EDD
label_0E75:
  be    #$01, label_0E9B
  be    #$02, label_0E9F
  be    #$0A, label_0EA3
  be    #$0B, label_0EB0
  be    #$0C, label_0EBD
  be    #$0D, label_0EC3
  be    #$09, label_0EC7
  be    #$0E, label_0ECB
  be    #$FD, label_0ECF
  be    #$FC, label_0ED3
  be    #$FB, label_0ED7
  be    #$FA, label_0EDB
  br    label_0EDD
label_0E9B:
  call  label_08B9
  br    label_0EDD
label_0E9F:
  call  label_090F
  br    label_0EDD
label_0EA3:
  ld    $29
  be    #$04, label_0EAC
  call  label_09BB
  br    label_0EDD
label_0EAC:
  call  label_0A6A
  br    label_0EDD
label_0EB0:
  ld    $29
  be    #$08, label_0EB9
  call  label_0B7D
  br    label_0EDD
label_0EB9:
  call  label_0BFE
  br    label_0EDD
label_0EBD:
  call  label_081D
  call  label_0F32
  br    label_0EDD
label_0EC3:
  call  label_0E5D
  br    label_0EDD
label_0EC7:
  call  label_0E08
  br    label_0EDD
label_0ECB:
  call  label_0E3A
  br    label_0EDD
label_0ECF:
  call  label_097B
  br    label_0EDD
label_0ED3:
  call  label_0800
  br    label_0EDD
label_0ED7:
  call  label_0EDE
  br    label_0EDD
label_0EDB:
  call  label_0F08
label_0EDD:
  ret
label_0EDE:
  call  label_081D
  mov   #$01, vtrbf
  ld    $20
  st    vtrbf
  ld    $21
  st    vtrbf
  mov   #$FB, vtrbf
  mov   #$FB, $24
  mov   #$01, vlreg
  ld    $27
  st    vtrbf
  mov   #$00, acc
  st    vtrbf
  st    vtrbf
  st    vtrbf
  set1  vsel, $00
  set1  mplsw, $04
  set1  mplsw, $00
  ret
label_0F08:
  call  label_081D
  mov   #$01, vtrbf
  ld    $20
  st    vtrbf
  ld    $21
  st    vtrbf
  mov   #$FA, vtrbf
  mov   #$FA, $24
  mov   #$01, vlreg
  ld    $28
  st    vtrbf
  mov   #$00, acc
  st    vtrbf
  st    vtrbf
  st    vtrbf
  set1  vsel, $00
  set1  mplsw, $04
  set1  mplsw, $00
  ret
label_0F32:
  mov   #$00, vtrbf
  ld    $20
  st    vtrbf
  ld    $21
  st    vtrbf
  mov   #$07, vtrbf
  mov   #$00, vlreg
  set1  vsel, $00
  set1  mplsw, $04
  set1  mplsw, $00
  ret
label_0F4A:
  add   #$03
  mov   #$00, fpr
label_0F4F:
  push  acc
  ldc
  st    vtrbf
  pop   acc
  dec   acc
  push  acc
  ldc
  st    vtrbf
  pop   acc
  dec   acc
  push  acc
  ldc
  st    vtrbf
  pop   acc
  dec   acc
  push  acc
  ldc
  st    vtrbf
  pop   acc
  add   #$07
  dbnz  b, label_0F4F
  sub   #$03
  ret
label_0F79:
  set1  psw, rambk0
label_0F7B:
  ld    @R0
  st    vtrbf
  inc   $00
  ld    @R0
  st    vtrbf
  inc   $00
  ld    @R0
  st    vtrbf
  inc   $00
  ld    @R0
  st    vtrbf
  inc   $00
  dbnz  b, label_0F7B
  clr1  psw, rambk0
  ret
label_0F95:
  mov   #$02, xbnk
  mov   #$01, xram_0184
  ld    trl
  add   #$03
label_0F9F:
  st    trl
  ldf
  st    vtrbf
  dec   trl
  ldf
  st    vtrbf
  dec   trl
  ldf
  st    vtrbf
  dec   trl
  ldf
  st    vtrbf
  ld    trl
  add   #$07
  dbnz  b, label_0F9F
  sub   #$03
  st    trl
  ret
label_0FBF:
  push  acc
label_0FC1:
  ld    mplsta
  bn    acc, $00, label_0FC1
  pop   acc
  ret
label_0FC9:
  push  acc
  push  b
  push  c
  set1  psw, rambk0
  ld    $00
  and   #$FC
  st    $00
  add   #$03
  st    $01
label_0FDB:
  ld    @R1
  st    b
  ld    @R0
  st    @R1
  ld    b
  st    @R0
  dec   $01
  inc   $00
  ld    @R1
  st    b
  ld    @R0
  st    @R1
  ld    b
  st    @R0
  ld    $01
  add   #$05
  st    $01
  ld    $00
  add   #$03
  st    $00
  dbnz  c, label_0FDB
  clr1  psw, rambk0
  pop   c
  pop   b
  pop   acc
  ret
label_1007:
  mov   #$80, mplrst
  nop
  nop
  mov   #$00, mplrst
  nop
  nop
  mov   #$13, vsel
  set1  mplsw, $02
  ret
label_1017:
  mov   #$00, acc
  st    $20
  st    $22
  ld    p7
  and   #$0C
  st    $23
  ror
  ror
  mov   #$30, trl
  mov   #$10, trh
  ldc
  st    $20
  ret

  .byte $01,$02,$04,$08

label_1034:
  push  ocr
  mov   #$81, ocr
  mov   #$02, xbnk
  mov   #$00, xram_0181
  mov   #$00, xram_0182
  mov   #$04, xram_0183
  mov   #$00, xbnk
  pop   ocr
  mov   #$00, $6E
label_104D:
  call  label_10CA
  mov   #$01, $33
  mov   #$00, $34
  mov   #$80, $35
label_1058:
  callf label_3696
  be    #$FF, label_10C4
  be    #$FE, label_1088
  be    #$FD, label_10C4
  be    #$08, label_1099
  ld    $70
  be    #$14, label_108D
  be    #$10, label_1099
  ld    $1D
  xor   b
  bz    label_1077
  call  label_1805
label_1077:
  ld    $1D
  st    b
  bnz   label_1058
  push  ocr
  mov   #$81, ocr
  call  label_1102
  pop   ocr
  br    label_1058
label_1088:
  callf label_0421
  br    label_104D
label_108D:
  call  label_1167
  bnz   label_10C4
  mov   #$00, $35
  mov   #$01, $33
  jmp   label_104D
label_1099:
  call  label_112E
  mov   #$00, $34
  mov   #$01, $33
  mov   #$80, $35
label_10A4:
  callf label_3696
  be    #$FF, label_10C4
  be    #$FE, label_1088
  be    #$FD, label_10C4
  be    #$04, label_104D
  ld    $70
  and   #$38
  be    #$20, label_104D
  ld    $33
  be    #$08, label_10A4
  bp    psw, cy, label_10A4
  br    label_104D
label_10C4:
  mov   #$00, $35
  jmpf  label_02EF
label_10CA:
  push  acc
  push  b
  push  $60
  push  $61
  push  psw
  clr1  psw, rambk0
  clr1  p3int, $00
  ld    $32
  and   #$C0
  st    $32
  callf label_3C07
  mov   #$00, $61
  call  label_164E
  st    $14
  call  label_1671
  st    $15
  mov   #$03, $60
  mov   #$0A, acc
  callf label_38F0
  set1  p3int, $00
  pop   psw
  pop   $61
  pop   $60
  pop   b
  pop   acc
  ret
label_1102:
  push  acc
  push  b
  push  c
  push  $60
  push  $61
  clr1  p3int, $00
  mov   #$00, $61
  ld    $1D
  bnz   label_1121
  call  label_1671
  st    $15
  ld    $1C
  bnz   label_1121
  call  label_164E
  st    $14
label_1121:
  set1  p3int, $00
  pop   $61
  pop   $60
  pop   c
  pop   b
  pop   acc
  ret
label_112E:
  push  acc
  push  $00
  push  $60
  push  $61
  clr1  p3int, $00
  callf label_3C07
  mov   #$02, $61
  call  label_15AB
  clr1  p3int, $00
  mov   #$52, $00
  ld    @R0
  st    $10
  inc   $00
  ld    @R0
  st    $11
  mov   #$00, $61
  call  label_15BF
  clr1  p3int, $00
  st    $12
  call  label_162B
  clr1  p3int, $00
  st    $13
  set1  p3int, $00
  pop   $61
  pop   $60
  pop   $00
  pop   acc
  ret
label_1167:
  clr1  p3int, $00
  callf label_3C07
  call  label_1240
  mov   #$00, acc
  callf label_1F47
  mov   #$40, $35
  set1  p3int, $00
  st    b
  callf label_3675
label_117E:
  callf label_3696
  be    #$10, label_1192
  be    #$20, label_11F2
  be    #$04, label_119D
  be    #$08, label_119D
  be    #$FF, label_11FB
  br    label_117E
label_1192:
  ld    b
  bz    label_11AB
  call  label_129D
  be    #$FF, label_11FB
  br    label_11AB
label_119D:
  ld    b
  xor   #$FF
  st    b
  callf label_1F47
  callf label_3662
  br    label_117E
label_11AB:
  callf label_3C07
  call  label_11FF
  mov   #$40, $35
  callf label_3675
label_11B6:
  callf label_3696
  be    #$10, label_11F2
  be    #$20, label_1167
  be    #$01, label_11CA
  be    #$02, label_11E1
  be    #$FF, label_11FB
  br    label_11B6
label_11CA:
  ld    $32
  and   #$C0
  be    #$40, label_11D9
  bp    psw, cy, label_11D9
  mov   #$00, $32
  br    label_11DD
label_11D9:
  add   #$40
  st    $32
label_11DD:
  call  label_1213
  br    label_11B6
label_11E1:
  ld    $32
  and   #$C0
  bnz   label_11EC
  mov   #$80, $32
  br    label_11DD
label_11EC:
  sub   #$40
  st    $32
  br    label_11DD
label_11F2:
  set1  p3int, $00
  mov   #$00, acc
  mov   #$02, $30
  ret
label_11FB:
  mov   #$FF, acc
  ret
label_11FF:
  clr1  p3int, $00
  mov   #$1C, trl
  mov   #$19, trh
  mov   #$00, $67
  mov   #$00, $60
  mov   #$00, $61
  callf label_3A05
label_1213:
  clr1  p3int, $00
  ld    $32
  and   #$C0
  bz    label_1226
  be    #$40, label_122E
  mov   #$2F, trl
  mov   #$19, trh
  br    label_1234
label_1226:
  mov   #$38, trl
  mov   #$19, trh
  br    label_1234
label_122E:
  mov   #$41, trl
  mov   #$19, trh
label_1234:
  mov   #$00, $60
  mov   #$03, $61
  callf label_3A05
  set1  p3int, $00
  ret
label_1240:
  mov   #$4A, trl
  mov   #$19, trh
  mov   #$00, $67
  mov   #$00, $60
  mov   #$00, $61
  callf label_3A05
  ret
label_1253:
  mov   #$20, $10
  mov   #$00, $11
  mov   #$10, $00
  callf label_3470
  ld    $52
  st    $17
  ld    $53
  st    $18
  mov   #$01, $12
  ld    $12
  callf label_344B
  st    $19
  mov   #$01, $13
  ld    $13
  callf label_344B
  st    $1A
  mov   #$00, $14
  ld    $14
  callf label_344B
  st    $1B
  mov   #$00, $15
  ld    $15
  callf label_344B
  st    $1C
  mov   #$00, $16
  ld    $16
  callf label_344B
  st    $1D
  mov   #$00, $1E
  ret
label_129D:
  push  psw
  clr1  psw, rambk0
  ld    $1B
  callf label_34FA
  st    $14
  ld    $1C
  callf label_34FA
  st    $15
  mov   #$00, $1D
  ld    p7
  and   #$01
  bnz   label_1329
  mov   #$40, $35
label_12BB:
  callf label_3C07
  ld    p7
  and   #$01
  bnz   label_1329
  mov   #$00, $67
  call  label_1681
  call  label_132E
  callf label_E02D
label_12CE:
  ld    p7
  and   #$01
  bnz   label_1329
  call  label_16AF
  call  label_1395
  be    #$20, label_12BB
label_12DB:
  ld    p7
  and   #$01
  bnz   label_1329
  call  label_16F2
  call  label_140D
  be    #$20, label_12CE
label_12E8:
  ld    p7
  and   #$01
  bnz   label_1329
  ld    $1B
  callf label_34FA
  st    $14
  ld    $1C
  callf label_34FA
  st    $15
  call  label_173B
  call  label_14F4
  be    #$20, label_12DB
label_1303:
  ld    p7
  and   #$01
  bnz   label_1329
  call  label_179E
  call  label_1532
  be    #$20, label_12E8
  ld    p7
  and   #$01
  bnz   label_1329
  call  label_17DC
  call  label_1570
  be    #$20, label_1303
  mov   #$00, $1D
  mov   #$00, acc
label_1323:
  mov   #$00, $35
  pop   psw
  ret
label_1329:
  mov   #$FF, acc
  br    label_1323
label_132E:
  callf label_3675
  mov   #$00, $34
label_1334:
  callf label_3718
  mov   #$00, $1D
  be    #$10, label_1394
  be    #$01, label_1348
  be    #$02, label_136F
  be    #$FF, label_1394
  br    label_1334
label_1348:
  ld    $18
  clr1  psw, cy
  add   #$01
  st    $18
  mov   #$00, acc
  addc  $17
  st    $17
  ld    $17
  be    #$26, label_138D
  bp    psw, cy, label_138D
  ld    $18
  be    #$0F, label_138D
  bp    psw, cy, label_138D
  mov   #$00, $17
  mov   #$00, $18
  br    label_138D
label_136F:
  ld    $18
  clr1  psw, cy
  sub   #$01
  st    $18
  ld    $17
  subc  #$00
  st    $17
  ld    $17
  bne   #$FF, label_138D
  ld    $18
  bne   #$FF, label_138D
  mov   #$27, $17
  mov   #$0F, $18
label_138D:
  mov   #$01, $61
  call  label_15AB
  br    label_1334
label_1394:
  ret
label_1395:
  callf label_3675
  mov   #$00, $34
label_139B:
  callf label_3718
  mov   #$00, $1D
  be    #$10, label_13DB
  be    #$20, label_13DB
  be    #$01, label_13B2
  be    #$02, label_13C1
  be    #$FF, label_13DB
  br    label_139B
label_13B2:
  inc   $19
  ld    $19
  be    #$0C, label_13CA
  bp    psw, cy, label_13CA
  mov   #$01, $19
  br    label_13CA
label_13C1:
  dec   $19
  ld    $19
  bnz   label_13CA
  mov   #$0C, $19
label_13CA:
  call  label_15BF
  ld    $19
  be    #$02, label_13F3
  ld    $1A
  be    #$1E, label_13DC
  bn    psw, cy, label_13DC
  br    label_139B
label_13DB:
  ret
label_13DC:
  ld    $19
  be    #$04, label_13EC
  be    #$06, label_13EC
  be    #$09, label_13EC
  be    #$0B, label_13EC
  br    label_139B
label_13EC:
  mov   #$1E, $1A
  call  label_162B
  br    label_139B
label_13F3:
  ld    $1A
  be    #$1D, label_13FD
  bn    psw, cy, label_13FD
  br    label_139B
label_13FD:
  ld    $1F
  bz    label_1406
  mov   #$1D, $1A
  br    label_1409
label_1406:
  mov   #$1C, $1A
label_1409:
  call  label_162B
  br    label_139B
label_140D:
  callf label_3675
  mov   #$00, $34
label_1413:
  callf label_3718
  mov   #$00, $1D
  be    #$10, label_142A
  be    #$20, label_142A
  be    #$01, label_146C
  be    #$FF, label_142A
  bne   #$02, label_1413
  jmp   label_14B7
label_142A:
  push  acc
  ld    $19
  be    #$02, label_1440
  be    #$04, label_145E
  be    #$06, label_145E
  be    #$09, label_145E
  be    #$0B, label_145E
  pop   acc
  ret
label_1440:
  ld    $1F
  bnz   label_1451
  ld    $1A
  be    #$1C, label_1469
  bp    psw, cy, label_1469
  mov   #$1C, $1A
  br    label_1469
label_1451:
  ld    $1A
  be    #$1D, label_1469
  bp    psw, cy, label_1469
  mov   #$1D, $1A
  br    label_1469
label_145E:
  ld    $1A
  be    #$1E, label_1469
  bp    psw, cy, label_1469
  mov   #$1E, $1A
label_1469:
  pop   acc
  ret
label_146C:
  inc   $1A
  ld    $19
  be    #$02, label_1499
  be    #$04, label_148C
  be    #$06, label_148C
  be    #$09, label_148C
  be    #$0B, label_148C
  ld    $1A
  be    #$1F, label_14F0
  bp    psw, cy, label_14F0
  mov   #$01, $1A
  br    label_14F0
label_148C:
  ld    $1A
  be    #$1E, label_14F0
  bp    psw, cy, label_14F0
  mov   #$01, $1A
  br    label_14F0
label_1499:
  ld    $1F
  bnz   label_14AA
  ld    $1A
  be    #$1C, label_14F0
  bp    psw, cy, label_14F0
  mov   #$01, $1A
  br    label_14F0
label_14AA:
  ld    $1A
  be    #$1D, label_14F0
  bp    psw, cy, label_14F0
  mov   #$01, $1A
  br    label_14F0
label_14B7:
  dec   $1A
  ld    $19
  be    #$02, label_14DC
  be    #$04, label_14D3
  be    #$06, label_14D3
  be    #$09, label_14D3
  be    #$0B, label_14D3
  ld    $1A
  bnz   label_14F0
  mov   #$1F, $1A
  br    label_14F0
label_14D3:
  ld    $1A
  bnz   label_14F0
  mov   #$1E, $1A
  br    label_14F0
label_14DC:
  ld    $1F
  bnz   label_14E9
  ld    $1A
  bnz   label_14F0
  mov   #$1C, $1A
  br    label_14F0
label_14E9:
  ld    $1A
  bnz   label_14F0
  mov   #$1D, $1A
label_14F0:
  call  label_162B
  jmp   label_1413
label_14F4:
  callf label_3675
  mov   #$00, $34
label_14FA:
  callf label_3718
  mov   #$00, $1D
  be    #$10, label_1531
  be    #$20, label_1531
  be    #$01, label_1511
  be    #$02, label_1520
  be    #$FF, label_1531
  br    label_14FA
label_1511:
  inc   $1B
  ld    $1B
  be    #$17, label_152A
  bp    psw, cy, label_152A
  mov   #$00, $1B
  br    label_152A
label_1520:
  dec   $1B
  ld    $1B
  bne   #$FF, label_152A
  mov   #$17, $1B
label_152A:
  mov   #$01, $61
  call  label_164E
  br    label_14FA
label_1531:
  ret
label_1532:
  callf label_3675
  mov   #$00, $34
label_1538:
  callf label_3718
  mov   #$00, $1D
  be    #$10, label_156F
  be    #$20, label_156F
  be    #$01, label_154F
  be    #$02, label_155E
  be    #$FF, label_156F
  br    label_1538
label_154F:
  inc   $1C
  ld    $1C
  be    #$3B, label_1568
  bp    psw, cy, label_1568
  mov   #$00, $1C
  br    label_1568
label_155E:
  dec   $1C
  ld    $1C
  bne   #$FF, label_1568
  mov   #$3B, $1C
label_1568:
  mov   #$01, $61
  call  label_1671
  br    label_1538
label_156F:
  ret
label_1570:
  callf label_3675
  mov   #$00, b
  mov   #$00, $34
label_1579:
  callf label_3696
  mov   #$00, $1D
  be    #$10, label_159E
  be    #$20, label_15AA
  be    #$04, label_1590
  be    #$08, label_1590
  be    #$FF, label_15AA
  br    label_1579
label_1590:
  ld    b
  xor   #$FF
  st    b
  callf label_1F47
  callf label_3662
  br    label_1579
label_159E:
  ld    b
  bz    label_15A7
  mov   #$00, acc
  br    label_15AA
label_15A7:
  mov   #$20, acc
label_15AA:
  ret
label_15AB:
  clr1  p3int, $00
  mov   #$17, $00
  callf label_351A
  mov   #$52, $00
  mov   #$02, $60
  callf label_3B30
  set1  p3int, $00
  ret
label_15BF:
  push  b
  push  c
  push  trh
  push  trl
  clr1  p3int, $00
  mov   #$F7, trl
  mov   #$15, trh
  ld    $19
  and   #$0F
  rol
  rol
  st    b
  mov   #$03, c
  mov   #$03, $60
label_15DD:
  ld    b
  inc   b
  ldc
  sub   #$31
  callf label_38F0
  inc   $60
  dbnz  c, label_15DD
  set1  p3int, $00
  pop   trl
  pop   trh
  pop   c
  pop   b
  ret

  .byte $20,$20,$20,$20,$4A,$41,$4E,$00,$46,$45,$42,$00,$4D,$41,$52,$00 ;     JAN.FEB.MAR.
  .byte $41,$50,$52,$00,$4D,$41,$59,$00,$4A,$55,$4E,$00,$4A,$55,$4C,$00 ; APR.MAY.JUN.JUL.
  .byte $41,$55,$47,$00,$53,$45,$50,$00,$4F,$43,$54,$00,$4E,$4F,$56,$00 ; AUG.SEP.OCT.NOV.
  .byte $44,$45,$43,$00 ; DEC.

label_162B:
  clr1  p3int, $00
  ld    $1A
  be    #$09, label_1640
  bp    psw, cy, label_1640
  callf label_34FA
  mov   #$00, $60
  callf label_3B05
  br    label_164B
label_1640:
  mov   #$00, $60
  callf label_39A1
  inc   $60
  callf label_38F0
label_164B:
  set1  p3int, $00
  ret
label_164E:
  clr1  p3int, $00
  ld    $1B
  be    #$09, label_1663
  bp    psw, cy, label_1663
  callf label_34FA
  mov   #$01, $60
  callf label_3B05
  br    label_166E
label_1663:
  mov   #$01, $60
  callf label_39A1
  inc   $60
  callf label_38F0
label_166E:
  set1  p3int, $00
  ret
label_1671:
  clr1  p3int, $00
  ld    $1C
  callf label_34FA
  mov   #$04, $60
  callf label_3B05
  set1  p3int, $00
  ret
label_1681:
  clr1  p3int, $00
  mov   #$05, $60
  mov   #$00, $61
  mov   #$0C, acc
  callf label_38F0
  mov   #$02, $61
  mov   #$0B, acc
  callf label_38F0
  mov   #$01, $61
  call  label_15AB
  mov   #$5D, trl
  mov   #$19, trh
  mov   #$00, $60
  mov   #$03, $61
  callf label_3A05
  set1  p3int, $00
  ret
label_16AF:
  push  b
  clr1  p3int, $00
  mov   #$00, $61
  mov   #$00, $67
  mov   #$02, b
label_16BC:
  mov   #$01, $60
  callf label_39A1
  mov   #$02, $61
  dbnz  b, label_16BC
  mov   #$02, $60
  mov   #$01, $61
  callf label_39A1
  mov   #$05, $60
  mov   #$00, $61
  mov   #$0C, acc
  callf label_38F0
  mov   #$02, $61
  mov   #$0B, acc
  callf label_38F0
  mov   #$01, $61
  call  label_15BF
  call  label_162B
  set1  p3int, $00
  pop   b
  ret
label_16F2:
  push  b
  clr1  p3int, $00
  mov   #$00, $61
  mov   #$00, $67
  mov   #$02, b
label_16FF:
  mov   #$02, $60
  callf label_39A1
  mov   #$05, $60
  callf label_39A1
  mov   #$02, $61
  dbnz  b, label_16FF
  mov   #$02, $60
  mov   #$01, $61
  callf label_39A1
  mov   #$01, $60
  mov   #$00, $61
  mov   #$0C, acc
  callf label_38F0
  mov   #$02, $61
  mov   #$0B, acc
  callf label_38F0
  mov   #$01, $61
  call  label_15BF
  call  label_162B
  set1  p3int, $00
  pop   b
  ret
label_173B:
  push  b
  mov   #$00, $1D
  clr1  p3int, $00
  mov   #$00, $61
  mov   #$00, $67
  mov   #$02, b
label_174B:
  mov   #$01, $60
  callf label_39A1
  mov   #$05, $60
  callf label_39A1
  mov   #$02, $61
  dbnz  b, label_174B
  mov   #$00, $60
  mov   #$01, $61
  callf label_39A1
  mov   #$02, $60
  mov   #$00, $61
  mov   #$0C, acc
  callf label_38F0
  mov   #$02, $61
  mov   #$0B, acc
  callf label_38F0
  mov   #$03, $60
  mov   #$01, $61
  mov   #$0A, acc
  callf label_38F0
  ld    $14
  callf label_344B
  st    $1B
  call  label_164E
  ld    $15
  callf label_344B
  st    $1C
  call  label_1671
  set1  p3int, $00
  pop   b
  ret
label_179E:
  mov   #$00, $1D
  clr1  p3int, $00
  mov   #$00, $67
  mov   #$00, $61
  mov   #$02, $60
  callf label_39A1
  mov   #$01, xbnk
  callf label_3BCD
  mov   #$5D, trl
  mov   #$19, trh
  mov   #$00, $60
  mov   #$03, $61
  callf label_3A05
  mov   #$05, $60
  mov   #$00, $61
  mov   #$0C, acc
  callf label_38F0
  mov   #$02, $61
  mov   #$0B, acc
  callf label_38F0
  set1  p3int, $00
  ret
label_17DC:
  mov   #$00, $1D
  clr1  p3int, $00
  mov   #$00, $67
  mov   #$00, $61
  mov   #$05, $60
  callf label_39A1
  mov   #$66, trl
  mov   #$19, trh
  mov   #$00, $60
  mov   #$02, $61
  callf label_3A05
  mov   #$00, acc
  callf label_1F47
  set1  p3int, $00
  ret
label_1805:
  push  acc
  push  trh
  push  trl
  push  b
  push  c
  push  $02
  mov   #$01, xbnk
  mov   #$07, trl
  mov   #$19, trh
  inc   $32
  ld    $32
  and   #$C0
  rol
  rol
  st    b
  bz    label_1838
label_1826:
  mov   #$00, acc
  ldc
  add   trl
  st    trl
  mov   #$00, acc
  addc  trh
  st    trh
  dbnz  b, label_1826
label_1838:
  mov   #$00, acc
  ldc
  st    b
  ld    $32
  and   #$0F
  xor   b
  bz    label_188B
  ld    $32
  bp    acc, $04, label_187A
  and   #$0F
  ldc
  or    #$80
  st    $02
  mov   #$CF, trl
  mov   #$18, trh
  ld    $32
  and   #$E0
  ror
  ror
  add   trl
  st    trl
  mov   #$00, acc
  addc  trh
  st    trh
  call  label_1897
  not1  $32, $05
label_186D:
  pop   $02
  pop   c
  pop   b
  pop   trl
  pop   trh
  pop   acc
  ret
label_187A:
  and   #$0F
  ldc
  or    #$80
  st    $02
  mov   #$FF, trl
  mov   #$18, trh
  call  label_1897
  br    label_186D
label_188B:
  ld    $32
  and   #$D0
  st    $32
  not1  $32, $04
  inc   $32
  br    label_1838
label_1897:
  push  acc
  push  b
  push  $02
  mov   #$00, acc
  mov   #$08, b
  push  acc
label_18A5:
  ldc
  st    @R2
  ld    $02
  and   #$0F
  be    #$05, label_18B7
  bp    psw, cy, label_18B7
  ld    $02
  add   #$04
  st    $02
label_18B7:
  ld    $02
  add   #$06
  st    $02
  pop   acc
  inc   acc
  push  acc
  dbnz  b, label_18A5
  pop   acc
  pop   $02
  pop   b
  pop   acc
  ret

  .byte $00,$08,$8E,$8E,$78,$78,$C8,$84,$00,$08,$8E,$8E,$78,$78,$48,$50 ; ....xx......xxHP
  .byte $30,$18,$BC,$FA,$FE,$BC,$30,$60,$30,$18,$BC,$FA,$FE,$BC,$30,$60 ; 0.....0`0.....0`
  .byte $E0,$76,$3D,$FE,$7C,$38,$40,$00,$06,$1D,$FE,$7C,$3C,$4E,$02,$00 ; .v=.|8@....|<N..
  .byte $00,$00,$00,$00,$00,$00,$00,$00,$07,$86,$91,$98,$A3,$AA,$B5,$07
  .byte $90,$A1,$92,$A3,$94,$A5,$07,$B0,$A1,$A2,$93,$94,$85,$41,$6E,$69 ; .............Ani
  .byte $6D,$61,$74,$65,$64,$0D,$0A,$20,$64,$69,$73,$70,$6C,$61,$79,$00 ; mated\r\n display.
  .byte $42,$69,$72,$64,$20,$20,$20,$20,$00,$44,$6F,$67,$20,$20,$20,$20 ; Bird    .Dog    
  .byte $20,$00,$46,$69,$73,$68,$20,$20,$20,$20,$00,$53,$65,$74,$20,$20 ;  .Fish    .Set  
  .byte $20,$20,$20,$0D,$0A,$20,$20,$20,$63,$6C,$6F,$63,$6B,$00,$1A,$3A ;    \r\n   clock..:
  .byte $53,$65,$6C,$65,$63,$74,$00,$4F,$4B,$20,$3F,$20,$20,$20,$20,$00 ; Select.OK ?    .

label_196F:
  mov   #$01, fpr
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$44, trl
  mov   #$00, $5C
  ldf
  bne   #$FF, label_1987
  inc   trl
  ldf
  bz    label_198D
label_1987:
  ld    $5C
  or    #$40
  st    $5C
label_198D:
  inc   trl
  ldf
  inc   trl
  be    #$FE, label_19A1
  be    #$C7, label_19A6
  bp    psw, cy, label_19A6
  be    #$E7, label_19A6
  bn    psw, cy, label_19A6
label_19A1:
  ldf
  bz    label_19AC
  br    label_19A6
label_19A6:
  ld    $5C
  or    #$20
  st    $5C
label_19AC:
  mov   #$4A, trl
  ldf
  inc   trl
  be    #$C7, label_19D5
  bp    psw, cy, label_19D5
  be    #$FE, label_19D5
  bn    psw, cy, label_19D5
  be    #$E6, label_19CC
  bp    psw, cy, label_19CC
  be    #$F1, label_19CC
  bn    psw, cy, label_19CC
  br    label_19DB
label_19CC:
  ldf
  bnz   label_19D5
  ld    $5C
  bnz   label_19DB
  br    label_1A08
label_19D5:
  ld    $5C
  or    #$10
  st    $5C
label_19DB:
  clr1  p3int, $00
  mov   #$E5, trl
  mov   #$20, trh
  mov   #$00, $60
  mov   #$00, $61
  callf label_3A05
  mov   #$01, xbnk
  callf label_3BCD
  ld    $5C
  mov   #$06, $60
  mov   #$03, $61
  callf label_3AD6
label_19FD:
  callf label_3696
  be    #$FF, label_1A06
  bne   #$FD, label_19FD
label_1A06:
  jmp   label_1B68
label_1A08:
  clr1  p3int, $00
  call  label_1BC7
  set1  p3int, $00
  mov   #$00, $3C
  mov   #$01, $3D
  mov   #$01, $33
label_1A17:
  set1  pcon, $00
  ld    $30
  bz    label_1A1F
  jmp   label_1B68
label_1A1F:
  ld    $35
  bnz   label_1A47
  ld    p7
  and   #$01
  bnz   label_1A4C
  ld    $33
  be    #$0D, label_1A34
  ld    $70
  and   #$18
  bz    label_1A17
label_1A34:
  clr1  p3int, $00
  callf label_3C07
  call  label_1C01
  set1  p3int, $00
  ld    $5C
  bnz   label_19DB
  ld    $3B
  bnz   label_1A4E
  jmp   label_1B33
label_1A47:
  callf label_0421
  br    label_1A17
label_1A4C:
  jmp   label_1B68
label_1A4E:
  clr1  p3int, $00
  call  label_1CED
  call  label_1D10
  mov   #$01, $33
  ld    $2B
  bnz   label_1A5E
  mov   #$01, $2B
label_1A5E:
  callf label_3718
  be    #$FC, label_1A7B
  be    #$FF, label_1A7E
  be    #$FD, label_1A7E
  be    #$01, label_1A80
  be    #$02, label_1A94
  be    #$08, label_1AA6
  be    #$20, label_1A08
  be    #$04, label_1A08
  br    label_1A5E
label_1A7B:
  jmpf  label_27AE
label_1A7E:
  jmp   label_1B68
label_1A80:
  inc   $3D
  ld    $3D
  sub   $3B
  bne   #$01, label_1A8C
  mov   #$01, $3D
label_1A8C:
  clr1  p3int, $00
  call  label_1D10
  set1  p3int, $00
  br    label_1A5E
label_1A94:
  dec   $3D
  ld    $3D
  bnz   label_1A9E
  ld    $3B
  st    $3D
label_1A9E:
  clr1  p3int, $00
  call  label_1D10
  set1  p3int, $00
  br    label_1A5E
label_1AA6:
  clr1  p3int, $00
  callf label_3C07
  call  label_1DB7
  ld    $5C
  bnz   label_1AC0
  call  label_1E61
  call  label_1E98
  mov   #$01, $33
  ld    $2B
  be    #$FF, label_1AC0
  mov   #$00, $2B
label_1AC0:
  callf label_3718
  be    #$FF, label_1A7E
  be    #$FD, label_1A7E
  be    #$01, label_1ADA
  be    #$02, label_1ADA
  be    #$08, label_1AEB
  be    #$20, label_1ADA
  be    #$04, label_1ADA
  br    label_1AC0
label_1ADA:
  clr1  p3int, $00
  callf label_3C07
  call  label_1CED
  be    #$02, label_1A94
  be    #$01, label_1A80
  call  label_1D10
  jmp   label_1A5E
label_1AEB:
  call  label_1EC9
label_1AED:
  call  label_1EEB
  mov   #$01, $33
label_1AF2:
  callf label_3718
  be    #$FF, label_1B68
  be    #$FD, label_1B68
  be    #$01, label_1B0F
  be    #$02, label_1B0F
  be    #$10, label_1B0C
  be    #$20, label_1AA6
  be    #$04, label_1AA6
  br    label_1AF2
label_1B0C:
  jmpf  label_2162
label_1B0F:
  call  label_1F19
  mov   #$01, $33
label_1B14:
  callf label_3718
  be    #$FF, label_1B68
  be    #$FD, label_1B68
  be    #$01, label_1AED
  be    #$02, label_1AED
  be    #$10, label_1B30
  be    #$20, label_1B2E
  be    #$04, label_1B2E
  br    label_1B14
label_1B2E:
  jmp   label_1AA6
label_1B30:
  jmpf  label_2DA5
label_1B33:
  clr1  p3int, $00
  mov   #$F8, trl
  mov   #$20, trh
  mov   #$00, $60
  mov   #$00, $61
  callf label_3A05
  mov   #$01, xbnk
  callf label_3BCD
  mov   #$01, $33
  mov   #$01, $2B
label_1B50:
  callf label_3696
  be    #$FC, label_1B66
  be    #$FF, label_1B68
  be    #$FD, label_1B68
  be    #$20, label_1B64
  be    #$04, label_1B64
  br    label_1B50
label_1B64:
  jmp   label_1A08
label_1B66:
  jmp   label_1A7B
label_1B68:
  jmpf  label_02EF
label_1B6B:
  mov   #$00, $36
  mov   #$00, $37
  mov   #$01, fpr
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$46, trl
  call  label_1CC2
  mov   #$C8, c
label_1B82:
  ldf
  inc   trl
  be    #$FC, label_1B8C
  inc   trl
  br    label_1B94
label_1B8C:
  ldf
  inc   trl
  bne   #$FF, label_1B94
  inc   $37
label_1B94:
  ld    trl
  bnz   label_1B9A
  inc   trh
label_1B9A:
  dbnz  c, label_1B82
  ret
label_1B9E:
  mov   #$00, $38
  mov   #$00, $39
  mov   #$01, fpr
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$46, trl
  call  label_1CC2
  mov   #$80, c
label_1BB5:
  ldf
  bne   #$FC, label_1BC6
  inc   trl
  ldf
  bne   #$FF, label_1BC6
  inc   trl
  inc   $39
  dbnz  c, label_1BB5
label_1BC6:
  ret
label_1BC7:
  mov   #$00, $60
  mov   #$00, $61
  callf label_3C07
  mov   #$C5, trl
  mov   #$20, trh
  callf label_3A05
  call  label_1B6B
  call  label_1B9E
  call  label_1C90
  mov   #$36, $00
  callf label_351A
  mov   #$52, $00
  mov   #$05, $60
  mov   #$00, $61
  callf label_3A97
  mov   #$38, $00
  callf label_351A
  mov   #$52, $00
  mov   #$01, $61
  callf label_3A97
  ret
label_1C01:
  mov   #$01, fpr
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$46, trl
  call  label_1CC2
  st    $54
  ld    trh
  st    $55
  ld    trl
  st    $56
  mov   #$FE, trh
  mov   #$4A, trl
  call  label_1CC2
  ld    $52
  st    $58
  mov   #$00, $3A
  mov   #$00, $3B
label_1C2B:
  mov   #$10, c
label_1C2E:
  ldf
  bz    label_1C33
  inc   $3B
label_1C33:
  ld    trl
  add   #$20
  st    trl
  ld    trh
  addc  #$00
  st    trh
  dbnz  c, label_1C2E
  ld    $54
  st    fpr
  st    $6F
  ld    $55
  st    trh
  ld    $56
  st    trl
  ld    $58
  clr1  psw, cy
  rolc
  st    c
  mov   #$00, acc
  rolc
  st    $52
  ld    trl
  add   c
  st    trl
  ld    trh
  add   $52
  st    trh
  ldf
  bne   #$FA, label_1C79
  inc   trl
  ldf
  dec   trl
  be    #$FF, label_1C88
  bz    label_1C79
  br    label_1C89
label_1C79:
  inc   trl
  ldf
  bnz   label_1C89
  dec   trl
  call  label_1CC2
  ld    $52
  st    $58
  br    label_1C2B
label_1C88:
  ret
label_1C89:
  ld    $5C
  or    #$04
  st    $5C
  ret
label_1C90:
  mov   #$01, fpr
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$46, trl
  call  label_1CC2
  mov   #$90, trl
  inc   trh
  mov   #$00, $3E
  mov   #$00, $3F
  mov   #$1F, b
label_1CAC:
  ldf
  bne   #$FC, label_1CBA
  inc   trl
  ldf
  dec   trl
  bne   #$FF, label_1CBA
  inc   $3F
label_1CBA:
  inc   trl
  inc   trl
  dbnz  b, label_1CAC
  ret
label_1CC2:
  push  b
  push  c
  push  psw
  clr1  psw, rambk0
  ldf
  st    b
  st    $52
  mov   #$02, acc
  mov   #$00, c
  mul
  st    trh
  ld    b
  and   #$01
  st    fpr
  st    $6F
  ld    c
  st    trl
  ld    $6F
  pop   psw
  pop   c
  pop   b
  ret
label_1CED:
  push  acc
  mov   #$D0, trl
  mov   #$20, trh
  mov   #$00, $60
  mov   #$00, $61
  callf label_3A05
  mov   #$3A, $00
  callf label_351A
  mov   #$52, $00
  mov   #$05, $60
  callf label_3A97
  pop   acc
  ret
label_1D10:
  push  $60
  push  $61
  mov   #$3C, $00
  callf label_351A
  mov   #$52, $00
  mov   #$01, $60
  callf label_3A97
  mov   #$00, $60
  mov   #$01, $61
  mov   #$00, $67
  call  label_1F7B
  ld    $5C
  bnz   label_1D97
  inc   trl
  ldf
  bne   #$FF, label_1D3B
  mov   #$FF, $67
label_1D3B:
  inc   trl
  inc   trl
  mov   #$08, c
label_1D42:
  inc   trl
  ldf
  callf label_37D2
  inc   $60
  dbnz  c, label_1D42
  inc   $61
  mov   #$00, $60
  mov   #$04, c
label_1D55:
  inc   trl
  ldf
  callf label_37D2
  inc   $60
  dbnz  c, label_1D55
  mov   #$00, $67
  inc   trl
  inc   $61
  mov   #$00, $60
  inc   trl
  inc   trl
  ldf
  callf label_3AD6
  inc   $60
  inc   $60
  inc   $60
  inc   trl
  ldf
  callf label_3AD6
  inc   $60
  inc   $60
  inc   $60
  dec   trl
  dec   trl
  ldf
  callf label_3AD6
  ld    trl
  sub   #$11
  st    trl
label_1D92:
  pop   $61
  pop   $60
  ret
label_1D97:
  mov   #$00, $60
  mov   #$01, $61
  mov   #$F8, trl
  mov   #$20, trh
  callf label_3A05
  ld    $5C
  or    #$02
  st    $5C
  mov   #$06, $60
  mov   #$03, $61
  callf label_3AD6
  br    label_1D92
label_1DB7:
  push  $60
  push  $61
  push  $6F
  push  trh
  push  trl
  mov   #$00, $60
  mov   #$01, $61
  mov   #$00, $67
  ld    trl
  add   #$1B
  st    trl
  ldf
  bnz   label_1E41
  dec   trl
  ldf
  bz    label_1E06
  st    b
  ld    trl
  sub   #$18
  st    trl
label_1DE0:
  inc   trl
  ldf
  bnz   label_1E41
  dec   trl
  ldf
  clr1  psw, cy
  rolc
  st    c
  mov   #$00, acc
  rolc
  add   $55
  st    trh
  ld    c
  add   $56
  st    trl
  ld    $54
  st    fpr
  st    $6F
  dbnz  b, label_1DE0
  br    label_1E0C
label_1E06:
  ld    trl
  sub   #$18
  st    trl
label_1E0C:
  inc   trl
  ldf
  bnz   label_1E41
  dec   trl
  call  label_1CC2
  mov   #$02, b
  mov   #$00, $61
label_1E1B:
  mov   #$08, c
  mov   #$00, $60
label_1E21:
  ldf
  callf label_37D2
  inc   trl
  inc   $60
  dbnz  c, label_1E21
  mov   #$01, $61
  dbnz  b, label_1E1B
label_1E32:
  pop   trl
  pop   trh
  pop   $6F
  ld    $6F
  st    fpr
  pop   $61
  pop   $60
  ret
label_1E41:
  mov   #$00, $60
  mov   #$00, $61
  mov   #$0B, trl
  mov   #$21, trh
  callf label_3A05
  ld    $5C
  or    #$01
  st    $5C
  mov   #$06, $60
  mov   #$03, $61
  callf label_3AD6
  br    label_1E32
label_1E61:
  push  $6F
  push  trh
  push  trl
  ldf
  be    #$33, label_1E76
  be    #$CC, label_1E7E
  mov   #$59, trl
  mov   #$21, trh
  br    label_1E84
label_1E76:
  mov   #$47, trl
  mov   #$21, trh
  br    label_1E84
label_1E7E:
  mov   #$50, trl
  mov   #$21, trh
label_1E84:
  mov   #$00, $60
  mov   #$03, $61
  callf label_3A05
  pop   trl
  pop   trh
  pop   $6F
  ld    $6F
  st    fpr
  ret
label_1E98:
  push  $6F
  push  trh
  push  trl
  ld    trl
  add   #$19
  st    trl
  ldf
  st    $52
  dec   trl
  ldf
  st    $53
  mov   #$52, $00
  callf label_351A
  mov   #$52, $00
  mov   #$05, $60
  mov   #$02, $61
  callf label_3A97
  pop   trl
  pop   trh
  pop   $6F
  ld    $6F
  st    fpr
  ret
label_1EC9:
  push  trh
  push  trl
  clr1  p3int, $00
  mov   #$01, xbnk
  callf label_3BCD
  mov   #$00, $60
  mov   #$02, $61
  mov   #$30, trl
  mov   #$21, trh
  callf label_3A05
  set1  p3int, $00
  pop   trl
  pop   trh
  ret
label_1EEB:
  push  trh
  push  trl
  clr1  p3int, $00
  mov   #$FF, $67
  mov   #$1E, trl
  mov   #$21, trh
  mov   #$00, $60
  mov   #$00, $61
  callf label_3A05
  mov   #$00, $67
  mov   #$27, trl
  mov   #$21, trh
  mov   #$01, $61
  callf label_3A05
  set1  p3int, $00
  pop   trl
  pop   trh
  ret
label_1F19:
  push  trh
  push  trl
  clr1  p3int, $00
  mov   #$1E, trl
  mov   #$21, trh
  mov   #$00, $60
  mov   #$00, $61
  callf label_3A05
  mov   #$27, trl
  mov   #$21, trh
  mov   #$FF, $67
  mov   #$01, $61
  callf label_3A05
  mov   #$00, $67
  set1  p3int, $00
  pop   trl
  pop   trh
  ret
label_1F47:
  push  trh
  push  trl
  bz    label_1F50
  mov   #$FF, $67
label_1F50:
  mov   #$3D, trl
  mov   #$21, trh
  mov   #$00, $60
  mov   #$03, $61
  callf label_3A05
  mov   #$00, $67
  bnz   label_1F67
  mov   #$FF, $67
label_1F67:
  mov   #$42, trl
  mov   #$21, trh
  mov   #$04, $60
  callf label_3A05
  mov   #$00, $67
  pop   trl
  pop   trh
  ret
label_1F7B:
  push  psw
  clr1  psw, rambk0
  mov   #$01, fpr
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$46, trl
  call  label_1CC2
  st    $54
  ld    trh
  st    $55
  ld    trl
  st    $56
  mov   #$FE, trh
  mov   #$4A, trl
  call  label_1CC2
  ld    $52
  st    $58
  mov   #$00, b
label_1FA6:
  mov   #$10, c
label_1FA9:
  ldf
  be    #$33, label_1FB2
  be    #$CC, label_1FB2
  br    label_1FBA
label_1FB2:
  inc   b
  ld    $3D
  sub   b
  bz    label_1FD2
label_1FBA:
  ld    trl
  add   #$20
  st    trl
  ld    trh
  addc  #$00
  st    trh
  dbnz  c, label_1FA9
  callf label_2043
  ld    $5C
  bnz   label_1FD5
  br    label_1FA6
label_1FD2:
  pop   psw
  ret
label_1FD5:
  ld    $5C
  or    #$80
  st    $5C
  br    label_1FD2
label_1FDD:
  push  psw
  clr1  psw, rambk0
  mov   #$01, fpr
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$46, trl
  call  label_1CC2
  st    $54
  ld    trh
  st    $55
  ld    trl
  st    $56
  mov   #$FE, trh
  mov   #$4A, trl
  callf label_1CC2
  ld    $52
  st    $58
label_2006:
  mov   #$10, c
label_2009:
  ldf
  be    #$33, label_200F
  br    label_2014
label_200F:
  callf label_2097
  bz    label_2038
label_2014:
  ld    trl
  add   #$20
  st    trl
  ld    trh
  addc  #$00
  st    trh
  dbnz  c, label_2009
  callf label_2043
  ld    $5C
  bnz   label_202C
  br    label_2006
label_202C:
  ld    $5C
  and   #$7F
  st    $5C
  mov   #$00, acc
label_2035:
  pop   psw
  ret
label_2038:
  ld    $5C
  and   #$7F
  st    $5C
  mov   #$FF, acc
  br    label_2035
label_2043:
  push  psw
  clr1  psw, rambk0
  ld    $54
  st    fpr
  st    $6F
  ld    $55
  st    trh
  ld    $56
  st    trl
  ld    $58
  clr1  psw, cy
  rolc
  st    c
  mov   #$00, acc
  rolc
  st    $52
  ld    trl
  add   c
  st    trl
  ld    trh
  add   $52
  st    trh
  ldf
  bne   #$FA, label_207E
  inc   trl
  ldf
  dec   trl
  be    #$FF, label_208F
  bz    label_207E
  br    label_208F
label_207E:
  inc   trl
  ldf
  bnz   label_208F
  dec   trl
  callf label_1CC2
  ld    $52
  st    $58
label_208C:
  pop   psw
  ret
label_208F:
  ld    $5C
  or    #$80
  st    $5C
  br    label_208C
label_2097:
  push  psw
  set1  psw, rambk0
  push  trh
  push  trl
  push  b
  push  $00
  ld    trl
  add   #$04
  st    trl
  mov   #$29, $00
  mov   #$0C, b
label_20AF:
  ldf
  xor   @R0
  inc   $00
  inc   trl
  bnz   label_20BA
  dbnz  b, label_20AF
label_20BA:
  pop   $00
  pop   b
  pop   trl
  pop   trh
  pop   psw
  ret

  .byte $46,$72,$65,$65,$0D,$0A,$47,$61,$6D,$65,$00,$85,$20,$20,$20,$2F ; Free\r\nGame..   /
  .byte $20,$20,$20,$0D,$0D,$0D,$0A,$20,$20,$2F,$20,$20,$2F,$20,$20,$00 ;    \r\r\r\n  /  /  .
  .byte $53,$79,$73,$74,$65,$6D,$20,$20,$0D,$0A,$20,$20,$20,$65,$72,$72 ; System  \r\n   err
  .byte $6F,$72,$00,$4E,$6F,$20,$73,$61,$76,$65,$64,$0D,$0A,$20,$20,$20 ; or.No saved\r\n   
  .byte $66,$69,$6C,$65,$73,$00,$46,$69,$6C,$65,$20,$69,$73,$20,$0D,$0A ; files.File is \r\n
  .byte $20,$64,$61,$6D,$61,$67,$65,$64,$00,$EF,$43,$6F,$70,$79,$20,$20 ;  damaged..Copy  
  .byte $20,$00,$EF,$44,$65,$6C,$65,$74,$65,$20,$00,$1A,$3A,$59,$65,$73 ;  ..Delete ..:Yes
  .byte $0D,$0A,$1B,$3A,$4E,$6F,$20,$00,$59,$65,$73,$20,$00,$20,$4E,$6F ; \r\n.:No .Yes . No
  .byte $20,$00,$44,$61,$74,$61,$20,$20,$20,$82,$00,$47,$61,$6D,$65,$20 ;  .Data   ..Game 
  .byte $20,$20,$82,$00,$4F,$74,$68,$65,$72,$20,$20,$82,$00 ;   ..Other  ..

label_2162:
  inc   trl
  ldf
  dec   trl
  bne   #$FF, label_216C
  jmp   label_21F3
label_216C:
  call  label_263B
  mov   #$00, c
  callf label_3675
  mov   #$01, $33
  mov   #$00, $35
label_217A:
  callf label_3696
  be    #$FF, label_21F0
  be    #$FD, label_21F0
  be    #$20, label_21A3
  be    #$04, label_2191
  be    #$08, label_2191
  be    #$10, label_219F
  br    label_217A
label_2191:
  ld    c
  xor   #$FF
  st    c
  callf label_1F47
  callf label_3662
  br    label_217A
label_219F:
  ld    c
  bnz   label_21A6
label_21A3:
  jmpf  label_1AEB
label_21A6:
  mov   #$00, $34
  ld    p7
  and   #$09
  bne   #$08, label_21B2
  jmp   label_222C
label_21B2:
  clr1  p3int, $00
  call  label_26F8
  mov   #$01, $33
  mov   #$F0, c
  mov   #$01, $2B
label_21BF:
  callf label_36D8
  be    #$FF, label_21F0
  be    #$FD, label_21F0
  be    #$FC, label_21EB
  be    #$20, label_21A3
  ld    c
  and   #$FC
  be    #$F4, label_21DE
  ld    c
  bne   #$F8, label_21DE
  and   #$F0
  st    c
label_21DE:
  mov   #$07, $60
  mov   #$03, $61
  callf label_37D2
  inc   c
  br    label_21BF
label_21EB:
  mov   #$FF, $2B
  jmp   label_2162
label_21F0:
  jmpf  label_1B68
label_21F3:
  push  trh
  push  trl
  clr1  p3int, $00
  mov   #$01, xbnk
  callf label_3BCD
  mov   #$88, trl
  mov   #$27, trh
  mov   #$00, $60
  mov   #$00, $61
  callf label_3A05
  pop   trl
  pop   trh
  callf label_3675
  mov   #$01, $33
label_2218:
  callf label_3696
  be    #$FF, label_21F0
  be    #$FD, label_21F0
  be    #$20, label_2229
  be    #$04, label_2229
  br    label_2218
label_2229:
  jmpf  label_1AEB
label_222C:
  mov   #$81, ocr
  mov   #$FF, $2B
  call  label_25A7
  mov   #$00, $60
  mov   #$02, $61
  clr1  p3int, $00
  mov   #$01, xbnk
  callf label_3BCD
  push  trh
  push  trl
  mov   #$1F, trl
  mov   #$27, trh
  callf label_3A05
  pop   trl
  pop   trh
  mov   #$00, sbuf1
  set1  psw, rambk0
  mov   #$10, $00
  mov   #$28, $01
  mov   #$11, @R0
  inc   $00
  mov   #$02, @R0
  inc   $00
  ldf
  st    @R0
  inc   $00
  push  trl
  ld    trl
  add   #$18
  st    trl
  ldf
  pop   trl
  st    @R0
  st    $24
  inc   $00
  mov   #$FF, @R0
  mov   #$04, b
  mov   #$10, $00
  mov   #$28, $01
  call  label_24E2
  bnz   label_2307
  call  label_254E
  bnz   label_2307
  call  label_2579
  bne   #$0A, label_2294
  jmp   label_2332
label_2294:
  mov   #$10, $00
  mov   #$1B, @R0
  inc   $00
  mov   #$06, @R0
  inc   $00
  push  trh
  push  trl
  ld    trl
  add   #$04
  st    trl
  mov   #$0C, b
label_22AC:
  ldf
  st    @R0
  inc   trl
  inc   $00
  dbnz  b, label_22AC
  pop   trl
  pop   trh
  mov   #$FF, @R0
  mov   #$0E, b
  mov   #$10, $00
  mov   #$28, $01
  call  label_24E2
  bnz   label_2307
  call  label_254E
  bnz   label_2307
  call  label_2579
  set1  scon1, $01
  be    #$0A, label_2338
  call  label_233E
  bnz   label_2307
  call  label_2426
  bnz   label_2307
  clr1  psw, rambk0
  call  label_2688
label_22DF:
  call  label_25C3
  mov   #$A3, ocr
  callf label_3675
  mov   #$01, $33
  mov   #$00, $35
  mov   #$00, $30
label_22F0:
  callf label_3696
  be    #$FF, label_22FE
  be    #$FD, label_22FE
  be    #$10, label_2301
  br    label_22F0
label_22FE:
  jmpf  label_1B68
label_2301:
  mov   #$00, $2B
  jmpf  label_196F
label_2307:
  ld    p7
  and   #$09
  bne   #$08, label_232C
  call  label_2579
  be    #$0A, label_232C
  mov   #$10, $00
  mov   #$10, @R0
  inc   $00
  mov   #$0B, @R0
  inc   $00
  mov   #$00, @R0
  inc   $00
  mov   #$FF, @R0
  mov   #$10, $00
  mov   #$28, $01
  call  label_24E2
label_232C:
  clr1  psw, rambk0
  call  label_26A4
  br    label_22DF
label_2332:
  clr1  psw, rambk0
  call  label_26C0
  br    label_22DF
label_2338:
  clr1  psw, rambk0
  call  label_26DC
  br    label_22DF
label_233E:
  push  b
  push  c
  clr1  psw, rambk0
  push  $6F
  set1  psw, rambk0
  push  trh
  push  trl
  mov   #$01, fpr
  clr1  psw, rambk0
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$46, trl
  callf label_1CC2
  set1  psw, rambk0
  st    $54
  ld    trh
  st    $55
  ld    trl
  st    $56
  pop   trl
  pop   trh
  clr1  psw, rambk0
  pop   $6F
  ld    $6F
  st    fpr
  push  $6F
  push  trh
  push  trl
  ld    trl
  add   #$02
  st    trl
label_2381:
  clr1  psw, rambk0
  callf label_1CC2
  set1  psw, rambk0
  st    $7D
  ld    trh
  st    $7E
  ld    trl
  st    $7F
  mov   #$04, c
label_2395:
  push  c
  call  label_24AB
  bnz   label_240E
  callf label_E01B
  mov   #$10, $00
  mov   #$10, @R0
  inc   $00
  mov   #$08, @R0
  inc   $00
  mov   #$00, @R0
  inc   $00
  mov   #$FF, @R0
  mov   #$03, b
  call  label_2579
  be    #$0A, label_240E
  mov   #$10, $00
  mov   #$28, $01
  call  label_24E2
  bnz   label_240E
  call  label_254E
  bnz   label_240E
  ld    $7F
  add   #$80
  st    $7F
  mov   #$00, acc
  addc  $7E
  st    $7E
  pop   c
  dbnz  c, label_2395
  ld    $54
  st    fpr
  clr1  psw, rambk0
  st    $6F
  set1  psw, rambk0
  ld    $7D
  and   #$01
  add   $55
  st    trh
  ld    $7E
  and   #$FE
  add   $56
  dec   acc
  dec   acc
  st    trl
  dbnz  $24, label_2381
  pop   trl
  pop   trh
  clr1  psw, rambk0
  pop   $6F
  ld    $6F
  st    fpr
  set1  psw, rambk0
  pop   c
  pop   b
  mov   #$00, acc
  ret
label_240E:
  pop   c
  pop   trl
  pop   trh
  clr1  psw, rambk0
  pop   $6F
  ld    $6F
  st    fpr
  set1  psw, rambk0
  pop   c
  pop   b
  mov   #$FF, acc
  ret
label_2426:
  push  b
  clr1  psw, rambk0
  push  $6F
  set1  psw, rambk0
  push  trh
  push  trl
  mov   #$10, $00
  mov   #$1B, @R0
  inc   $00
  mov   #$03, @R0
  inc   $00
  ld    trl
  add   #$10
  st    trl
  mov   #$0C, b
label_2446:
  ldf
  st    @R0
  inc   trl
  inc   $00
  dbnz  b, label_2446
  mov   #$FF, @R0
  mov   #$0E, b
  call  label_2579
  be    #$0A, label_24A6
  mov   #$10, $00
  mov   #$28, $01
  call  label_24E2
  bnz   label_24A6
  call  label_254E
  bnz   label_24A6
  mov   #$10, $00
  mov   #$10, @R0
  inc   $00
  mov   #$08, @R0
  inc   $00
  mov   #$00, @R0
  inc   $00
  mov   #$FF, @R0
  mov   #$03, b
  call  label_2579
  be    #$0A, label_24A6
  mov   #$10, $00
  mov   #$28, $01
  call  label_24E2
  bnz   label_24A6
  call  label_254E
  bnz   label_24A6
  mov   #$00, acc
label_2491:
  pop   trl
  pop   trh
  clr1  psw, rambk0
  pop   $6F
  push  acc
  ld    $6F
  st    fpr
  pop   acc
  set1  psw, rambk0
  pop   b
  ret
label_24A6:
  mov   #$FF, acc
  br    label_2491
label_24AB:
  callf label_E027
  push  $7D
  push  $7E
  push  $7F
  mov   #$7E, $00
  mov   #$50, @R0
  inc   $00
  mov   #$04, @R0
  mov   #$82, b
  call  label_2579
  be    #$0A, label_24DD
  mov   #$7E, $00
  mov   #$7E, $01
  call  label_24E2
  bnz   label_24DD
  call  label_2560
  bnz   label_24DD
  mov   #$00, acc
label_24D6:
  pop   $7F
  pop   $7E
  pop   $7D
  ret
label_24DD:
  mov   #$FF, acc
  br    label_24D6
label_24E2:
  mov   #$01, $26
  clr1  psw, rambk0
  mov   #$00, $34
  set1  psw, rambk0
  set1  scon1, $03
  call  label_25D3
label_24F0:
  set1  pcon, $00
  push  psw
  clr1  psw, rambk0
  ld    $34
  pop   psw
  be    #$06, label_254A
  bn    psw, cy, label_254A
  ld    p7
  and   #$09
  bne   #$08, label_254A
  bn    scon1, $01, label_24F0
  clr1  scon1, $01
  ld    sbuf1
  bne   #$E0, label_254A
  inc   $00
  ld    b
  dec   $01
  st    $26
  st    $25
  mov   #$00, sbuf1
  clr1  scon1, $01
  set1  scon1, $03
  call  label_25D3
  inc   $00
  clr1  psw, rambk0
  mov   #$00, $34
  set1  psw, rambk0
label_252D:
  set1  pcon, $00
  clr1  psw, rambk0
  ld    $34
  set1  psw, rambk0
  be    #$06, label_254A
  bn    psw, cy, label_254A
  ld    p7
  and   #$09
  bne   #$08, label_254A
  ld    $26
  bnz   label_252D
  mov   #$00, acc
  ret
label_254A:
  mov   #$FF, acc
  ret
label_254E:
  mov   #$10, $00
  mov   #$28, $01
label_2554:
  ld    @R0
  xor   @R1
  bnz   label_255F
  inc   $00
  inc   $01
  dbnz  $25, label_2554
label_255F:
  ret
label_2560:
  mov   #$7E, $00
  mov   #$50, acc
  xor   @R0
  bnz   label_2575
  mov   #$04, acc
  inc   $00
  xor   @R0
  bnz   label_2575
  mov   #$00, acc
  ret
label_2575:
  mov   #$FF, acc
  ret
label_2579:
  push  psw
  clr1  psw, rambk0
  mov   #$00, $34
label_2580:
  ld    $34
  be    #$06, label_259A
  bn    psw, cy, label_259A
  bn    scon1, $01, label_2580
  clr1  scon1, $01
  ld    sbuf1
  bz    label_2580
  bne   #$0C, label_259A
  pop   psw
  mov   #$00, acc
  ret
label_259A:
  pop   psw
  mov   #$0A, acc
  ret
label_25A0:
  set1  pcon, $00
  ld    $26
  bnz   label_25A0
  ret
label_25A7:
  mov   #$F8, sbr
  mov   #$40, p1
  mov   #$8D, p1fcr
  mov   #$8D, p1ddr
  mov   #$04, scon0
  mov   #$00, sbuf0
  mov   #$04, scon1
  mov   #$00, sbuf1
  mov   #$FF, $6E
  ret
label_25C3:
  mov   #$40, p1
  mov   #$80, p1ddr
  mov   #$BF, p1fcr
  mov   #$00, ip
  mov   #$00, $6E
  ret
label_25D3:
  clr1  scon0, $01
  ld    @R0
  st    sbuf0
  set1  scon0, $00
  mov   #$16, acc
label_25DD:
  nop
  dbnz  acc, label_25DD
  set1  scon0, $03
  ret
label_25E4:
  push  acc
  clr1  scon0, $01
  ld    sbuf0
  push  psw
  set1  psw, rambk0
  st    @R1
  inc   $01
  dbnz  $26, label_25FB
  pop   psw
  clr1  scon0, $00
  pop   acc
  ret
label_25FB:
  mov   #$0E, acc
label_25FE:
  nop
  dbnz  acc, label_25FE
  ld    @R0
  st    sbuf0
  inc   $00
  set1  scon0, $03
  pop   psw
  pop   acc
  ret
label_260E:
  push  acc
  clr1  scon1, $01
  ld    sbuf1
  set1  scon1, $03
  push  psw
  set1  psw, rambk0
  st    @R1
  inc   $01
  dbnz  $3E, label_2623
  mov   #$04, scon1
label_2623:
  pop   psw
  pop   acc
  ret
label_2628:
  mov   #$81, ocr
  ld    ocr
  and   #$FC
  st    ocr
  mov   #$64, acc
label_2634:
  dbnz  acc, label_2634
  mov   #$12, ocr
  ret
label_263B:
  push  trh
  push  trl
  clr1  p3int, $00
  callf label_3C07
  mov   #$00, $60
  mov   #$00, $61
  ld    trl
  add   #$04
  st    trl
  mov   #$08, c
  mov   #$02, b
label_2656:
  ldf
  callf label_37D2
  inc   trl
  inc   $60
  dbnz  c, label_2656
  inc   $61
  mov   #$00, $60
  mov   #$04, c
  dbnz  b, label_2656
  mov   #$16, trl
  mov   #$27, trh
  mov   #$00, $60
  mov   #$02, $61
  callf label_3A05
  mov   #$00, acc
  callf label_1F47
  set1  p3int, $00
  pop   trl
  pop   trh
  ret
label_2688:
  push  trh
  push  trl
  clr1  p3int, $00
  mov   #$00, $60
  mov   #$02, $61
  mov   #$28, trl
  mov   #$27, trh
  callf label_3A05
  set1  p3int, $00
  pop   trl
  pop   trh
  ret
label_26A4:
  push  trh
  push  trl
  clr1  p3int, $00
  mov   #$00, $60
  mov   #$02, $61
  mov   #$31, trl
  mov   #$27, trh
  callf label_3A05
  set1  p3int, $00
  pop   trl
  pop   trh
  ret
label_26C0:
  push  trh
  push  trl
  clr1  p3int, $00
  mov   #$00, $60
  mov   #$00, $61
  mov   #$44, trl
  mov   #$27, trh
  callf label_3A05
  set1  p3int, $00
  pop   trl
  pop   trh
  ret
label_26DC:
  push  trh
  push  trl
  clr1  p3int, $00
  mov   #$00, $60
  mov   #$01, $61
  mov   #$6B, trl
  mov   #$27, trh
  callf label_3A05
  set1  p3int, $00
  pop   trl
  pop   trh
  ret
label_26F8:
  push  trh
  push  trl
  mov   #$01, xbnk
  callf label_3BCD
  mov   #$9B, trl
  mov   #$27, trh
  mov   #$00, $60
  mov   #$02, $61
  callf label_3A05
  pop   trl
  pop   trh
  ret

  .byte $43,$6F,$70,$79,$20,$3F,$20,$20,$00,$43,$6F,$70,$79,$69,$6E,$67 ; Copy ?  .Copying
  .byte $21,$00,$43,$6F,$70,$69,$65,$64,$20,$20,$00,$43,$6F,$70,$79,$20 ; !.Copied  .Copy 
  .byte $20,$20,$20,$0D,$0A,$20,$20,$66,$61,$69,$6C,$65,$64,$00,$4E,$6F ;    \r\n  failed.No
  .byte $74,$20,$20,$20,$20,$20,$0D,$0A,$20,$65,$6E,$6F,$75,$67,$68,$20 ; t     \r\n enough 
  .byte $0D,$0A,$20,$20,$66,$72,$65,$65,$20,$20,$0D,$0A,$20,$20,$6D,$65 ; \r\n  free  \r\n  me
  .byte $6D,$6F,$72,$79,$00,$53,$61,$6D,$65,$20,$20,$20,$20,$0D,$0A,$20 ; mory.Same    \r\n 
  .byte $66,$69,$6C,$65,$20,$20,$20,$0D,$0A,$20,$20,$65,$78,$69,$73,$74 ; file   \r\n  exist
  .byte $73,$00,$55,$6E,$61,$62,$6C,$65,$20,$20,$0D,$0A,$20,$74,$6F,$20 ; s.Unable  \r\n to 
  .byte $63,$6F,$70,$79,$00,$43,$6F,$6E,$6E,$65,$63,$74,$20,$0D,$0A,$56 ; copy.Connect \r\nV
  .byte $4D,$20,$20,$20,$20,$20,$20,$00 ; M      .

label_27AE:
  mov   #$81, ocr
  call  label_2628
  call  label_2D32
  callf label_1B6B
  callf label_1B9E
  call  label_25A7
  mov   #$E0, sbr
  clr1  psw, rambk0
  mov   #$40, $35
  set1  psw, rambk0
  call  label_2F7A
  mov   #$28, $01
  mov   #$20, ip
  mov   #$00, sbuf1
  set1  scon1, $03
label_27D4:
  callf label_36D8
  be    #$FD, label_2800
  push  psw
  clr1  psw, rambk0
  ld    $70
  pop   psw
  be    #$30, label_27F9
  ld    p7
  and   #$09
  bne   #$08, label_27FE
  ld    sbuf1
  bz    label_27D4
  and   #$F0
  be    #$10, label_2802
  set1  scon1, $03
  br    label_27D4
label_27F9:
  clr1  psw, rambk0
  mov   #$FF, $2B
label_27FE:
  jmp   label_2938
label_2800:
  jmp   label_2923
label_2802:
  clr1  p3int, $00
  ld    sbuf1
  and   #$0F
  clr1  scon1, $01
  inc   acc
  inc   acc
  st    $3F
  inc   acc
  st    $3E
  call  label_2D54
  mov   #$10, $00
  mov   #$E0, @R0
  mov   #$01, $26
  callf label_25D3
  callf label_25A0
  mov   #$28, $01
  mov   #$0D, scon1
label_282A:
  set1  pcon, $00
  ld    p7
  and   #$09
  bne   #$08, label_27FE
  ld    $3E
  bnz   label_282A
  mov   #$28, $01
  ld    @R1
  bne   #$02, label_2862
  inc   $01
  ld    @R1
  st    $60
  inc   $01
  ld    @R1
  st    $3D
  st    $78
  mov   #$00, $79
  ld    $60
  clr1  psw, rambk0
  bne   #$CC, label_2858
  ld    $39
  br    label_285A
label_2858:
  ld    $37
label_285A:
  set1  psw, rambk0
  be    $3D, label_286B
  bn    psw, cy, label_286B
label_2862:
  mov   #$0A, acc
  call  label_2CB8
  clr1  psw, rambk0
  jmp   label_27AE
label_286B:
  call  label_2CCE
  call  label_2953
  bz    label_2873
  jmp   label_2938
label_2873:
  mov   #$28, $01
  ld    @R1
  be    #$06, label_287C
  br    label_2862
label_287C:
  call  label_2D02
  call  label_2A01
  push  acc
  ld    $60
  bne   #$CC, label_288D
  mov   #$00, $62
  mov   #$00, $63
label_288D:
  pop   acc
  bz    label_2893
  br    label_28C3
label_2893:
  call  label_295B
  bnz   label_28D3
  call  label_2953
  bz    label_289D
  jmp   label_2938
label_289D:
  mov   #$28, $01
  ld    @R1
  be    #$03, label_28A6
  jmp   label_28C3
label_28A6:
  call  label_2D02
  call  label_2A29
  call  label_2953
  bz    label_28B0
  jmp   label_2938
label_28B0:
  mov   #$28, $01
  ld    @R1
  be    #$08, label_28B9
  jmp   label_28C3
label_28B9:
  call  label_28E0
  bnz   label_28C3
  call  label_2D02
  clr1  psw, rambk0
  jmp   label_27AE
label_28C3:
  ld    p7
  and   #$09
  bne   #$08, label_28DE
  mov   #$0A, acc
  call  label_2CB8
  clr1  psw, rambk0
  jmp   label_27AE
label_28D3:
  ld    p7
  and   #$09
  bne   #$08, label_28DE
  clr1  psw, rambk0
  jmp   label_27AE
label_28DE:
  jmp   label_2938
label_28E0:
  clr1  ie, $07
  call  label_2FCF
  clr1  psw, rambk0
  ld    $5C
  set1  psw, rambk0
  bz    label_28EE
  br    label_2918
label_28EE:
  call  label_2B1E
  bnz   label_2918
  call  label_2BB3
  bnz   label_2918
  mov   #$03, b
label_28F9:
  push  b
  callf label_E024
  pop   b
  callf label_E01B
  bz    label_290B
  dbnz  b, label_28F9
  callf label_30E8
label_290B:
  set1  ie, $07
  mov   #$0C, acc
  call  label_2CB8
  clr1  psw, rambk0
  mov   #$00, acc
  ret
label_2918:
  set1  ie, $07
  mov   #$0A, acc
  call  label_2CB8
  mov   #$FF, acc
  ret
label_2923:
  clr1  psw, rambk0
  set1  p3int, $00
  call  label_25C3
  mov   #$A3, ocr
  mov   #$00, $2B
  mov   #$00, $35
  mov   #$01, $33
  jmpf  label_1B68
label_2938:
  clr1  psw, rambk0
  set1  p3int, $00
  call  label_25C3
  mov   #$02, xbnk
  mov   #$00, xram_0184
  mov   #$A3, ocr
  mov   #$01, $33
  mov   #$00, $35
  mov   #$00, $30
  jmpf  label_196F
label_2953:
  mov   #$0C, acc
  call  label_2CB8
  call  label_2C3C
  ret
label_295B:
  push  b
  push  c
  ld    $60
  bne   #$CC, label_2991
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  bne   #$FC, label_2973
  ld    vtrbf
  be    #$FF, label_2975
label_2973:
  jmp   label_29B2
label_2975:
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  mov   #$FA, vtrbf
  mov   #$FF, vtrbf
  mov   #$00, $7D
  mov   #$00, $7E
  mov   #$00, $7F
  br    label_2997
label_298B:
  call  label_2AD0
  bz    label_2997
  br    label_29B2
label_2991:
  call  label_2A3C
  bz    label_2997
  br    label_29B2
label_2997:
  call  label_29BA
  bnz   label_29B2
  call  label_2D02
  ld    $60
  be    #$CC, label_29A7
  dbnz  $3D, label_2991
  br    label_29AA
label_29A7:
  dbnz  $3D, label_298B
label_29AA:
  mov   #$00, acc
  pop   c
  pop   b
  ret
label_29B2:
  mov   #$FF, acc
  pop   c
  pop   b
  ret
label_29BA:
  push  b
  push  c
  mov   #$04, c
label_29C1:
  push  $7D
  push  $7E
  push  $7F
  call  label_2953
  mov   #$7F, $01
  ld    @R1
  be    #$04, label_29D2
  br    label_29F4
label_29D2:
  pop   $7F
  pop   $7E
  pop   $7D
  call  label_2953
  bnz   label_29F9
  mov   #$28, $01
  ld    @R1
  be    #$08, label_29E5
  br    label_29B2
label_29E5:
  call  label_2BE8
  bnz   label_29F4
  dbnz  c, label_29C1
  pop   c
  pop   b
  mov   #$00, acc
  ret
label_29F4:
  mov   #$0A, acc
  call  label_2CB8
label_29F9:
  pop   c
  pop   b
  mov   #$FF, acc
  ret
label_2A01:
  callf label_1FDD
  bnz   label_2A25
  mov   #$29, $01
  mov   #$64, $00
  mov   #$0C, b
label_2A0F:
  ld    @R1
  st    @R0
  inc   $01
  inc   $00
  dbnz  b, label_2A0F
  mov   #$00, $61
  mov   #$FF, $62
  mov   #$FF, $63
  mov   #$00, acc
  ret
label_2A25:
  mov   #$FF, acc
  ret
label_2A29:
  mov   #$29, $01
  mov   #$70, $00
  mov   #$0C, b
label_2A32:
  ld    @R1
  st    @R0
  inc   $01
  inc   $00
  dbnz  b, label_2A32
  ret
label_2A3C:
  push  c
  mov   #$8E, vrmad1
  set1  vrmad2, $00
  mov   #$C8, c
label_2A46:
  ld    vtrbf
  bne   #$FC, label_2A52
  ld    vtrbf
  be    #$FF, label_2A67
  br    label_2A54
label_2A52:
  ld    vtrbf
label_2A54:
  ld    vrmad1
  sub   #$04
  st    vrmad1
  ld    vrmad2
  subc  #$00
  and   #$01
  st    vrmad2
  dbnz  c, label_2A46
  jmp   label_2ACA
label_2A67:
  ld    vrmad1
  sub   #$02
  st    vrmad1
  ld    vrmad2
  subc  #$00
  and   #$01
  st    vrmad2
  clr1  psw, cy
  rorc
  ld    vrmad1
  rorc
  st    $44
  mov   #$00, $45
  ld    $62
  bne   #$FF, label_2A96
  ld    $63
  be    #$FF, label_2A8C
  jmp   label_2ACA
label_2A8C:
  ld    $44
  st    $62
  ld    $45
  st    $63
  br    label_2AA6
label_2A96:
  ld    $70
  st    vrmad2
  ld    $71
  st    vrmad1
  ld    $44
  st    vtrbf
  ld    $45
  st    vtrbf
label_2AA6:
  ld    $44
  clr1  psw, cy
  rolc
  st    $71
  st    $7E
  st    vrmad1
  mov   #$00, acc
  rolc
  st    $70
  st    $7D
  st    vrmad2
  mov   #$00, $7F
  mov   #$FA, vtrbf
  mov   #$FF, vtrbf
  pop   c
  mov   #$00, acc
  ret
label_2ACA:
  pop   c
  mov   #$FF, acc
  ret
label_2AD0:
  push  b
  ld    vtrbf
  bne   #$FC, label_2B18
  ld    vtrbf
  bne   #$FF, label_2B18
  ld    vrmad1
  sub   #$02
  st    vrmad1
  st    $7E
  ld    vrmad2
  subc  #$00
  and   #$01
  st    vrmad2
  st    $7D
  mov   #$00, $7F
  clr1  psw, cy
  rorc
  ld    vrmad1
  rorc
  st    b
  ld    vrmad1
  sub   #$02
  st    vrmad1
  ld    vrmad2
  subc  #$00
  st    vrmad2
  ld    b
  st    vtrbf
  mov   #$00, vtrbf
  mov   #$FA, vtrbf
  mov   #$FF, vtrbf
  pop   b
  mov   #$00, acc
  ret
label_2B18:
  pop   b
  mov   #$FF, acc
  ret
label_2B1E:
  mov   #$01, fpr
  clr1  psw, rambk0
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$46, trl
  callf label_1CC2
  set1  psw, rambk0
  st    $54
  ld    trh
  st    $55
  ld    trl
  st    $56
  mov   #$01, fpr
  clr1  psw, rambk0
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$4A, trl
  callf label_1CC2
  set1  psw, rambk0
label_2B4E:
  mov   #$10, b
label_2B51:
  ldf
  bz    label_2B92
  ld    trl
  add   #$20
  st    trl
  mov   #$00, acc
  addc  trh
  st    trh
  dbnz  b, label_2B51
  ld    trh
  and   #$FE
  st    vrmad1
  clr1  psw, rambk0
  ld    $6F
  set1  psw, rambk0
  st    vrmad2
  ld    vtrbf
  bne   #$FA, label_2B7D
  ld    vtrbf
  bnz   label_2BAF
  br    label_2B81
label_2B7D:
  ld    vtrbf
  bnz   label_2BAF
label_2B81:
  ld    vrmad1
  sub   #$02
  st    $7E
  ld    vrmad2
  subc  #$00
  st    $7D
  mov   #$00, $7F
  br    label_2B4E
label_2B92:
  push  trl
  ld    trl
  and   #$80
  st    $7F
  ld    trh
  st    $7E
  clr1  psw, rambk0
  ld    $6F
  set1  psw, rambk0
  st    $7D
  callf label_E027
  pop   trl
  mov   #$00, acc
  ret
label_2BAF:
  mov   #$FF, acc
  ret
label_2BB3:
  mov   #$04, b
  mov   #$80, $00
label_2BB9:
  ld    @R0
  bz    label_2BC7
  ld    $00
  add   #$20
  st    $00
  dbnz  b, label_2BB9
  br    label_2BE4
label_2BC7:
  mov   #$60, $01
  mov   #$1C, b
label_2BCD:
  ld    @R1
  st    @R0
  inc   $01
  inc   $00
  dbnz  b, label_2BCD
  mov   #$00, acc
  st    @R0
  inc   $00
  st    @R0
  inc   $00
  st    @R0
  inc   $00
  st    @R0
  ret
label_2BE4:
  mov   #$FF, acc
  ret
label_2BE8:
  push  b
  mov   #$03, b
  clr1  ie, $07
label_2BEF:
  push  b
  push  c
  callf label_E024
  pop   c
  pop   b
  callf label_E01B
  bz    label_2C04
  dbnz  b, label_2BEF
  br    label_2C19
label_2C04:
  ld    $7F
  add   #$80
  st    $7F
  mov   #$00, acc
  addc  $7E
  st    $7E
  mov   #$00, acc
label_2C14:
  set1  ie, $07
  pop   b
  ret
label_2C19:
  ld    $7D
  and   #$01
  push  acc
  ld    $7E
  and   #$FE
  push  acc
  call  label_2F7A
  pop   acc
  st    vrmad1
  pop   acc
  st    vrmad2
  mov   #$FF, vtrbf
  mov   #$FF, vtrbf
  call  label_2FCF
  mov   #$FF, acc
  br    label_2C14
label_2C3C:
  set1  pcon, $00
  ld    p7
  and   #$09
  bne   #$08, label_2C8C
  ld    sbuf1
  bz    label_2C3C
  and   #$F0
  be    #$10, label_2C57
  bne   #$50, label_2C53
  jmp   label_2C90
label_2C53:
  set1  scon1, $03
  br    label_2C3C
label_2C57:
  ld    sbuf1
  and   #$0F
  clr1  scon1, $01
  inc   acc
  inc   acc
  st    $3F
  inc   acc
  st    $3E
  mov   #$10, $00
  mov   #$E0, @R0
  mov   #$01, $26
  callf label_25D3
  callf label_25A0
  mov   #$28, $01
  mov   #$0D, scon1
label_2C7B:
  set1  pcon, $00
  ld    p7
  and   #$09
  bne   #$08, label_2C8C
  ld    $3E
  bnz   label_2C7B
  mov   #$00, acc
  ret
label_2C8C:
  mov   #$FF, acc
  ret
label_2C90:
  mov   #$82, $3E
  mov   #$81, $3F
  mov   #$10, $00
  mov   #$E0, @R0
  mov   #$01, $26
  callf label_25D3
  callf label_25A0
  mov   #$7F, $01
  mov   #$0D, scon1
label_2CAA:
  set1  pcon, $00
  ld    p7
  and   #$09
  bne   #$08, label_2C8C
  ld    $3E
  bnz   label_2CAA
  ret
label_2CB8:
  mov   #$10, $00
  mov   #$28, $01
  st    @R0
  mov   #$01, $26
  callf label_25D3
  callf label_25A0
  mov   #$28, $01
  set1  scon1, $03
  ret
label_2CCE:
  push  b
  mov   #$01, xbnk
  mov   #$C0, $02
  ld    $3D
  add   #$03
  st    b
label_2CDC:
  ld    @R2
  be    #$FF, label_2CE9
  set1  psw, cy
  rorc
  st    @R2
  dbnz  b, label_2CDC
  br    label_2CFF
label_2CE9:
  inc   $02
  ld    $02
  and   #$0F
  bne   #$0C, label_2CFA
  ld    $02
  add   #$04
  or    #$C0
  st    $02
label_2CFA:
  mov   #$80, @R2
  dbnz  b, label_2CDC
label_2CFF:
  pop   b
  ret
label_2D02:
  push  b
  push  acc
  ld    @R2
  st    b
  clr1  psw, cy
  rolc
  and   b
  st    @R2
  bnz   label_2D22
  dec   $02
  ld    $02
  and   #$0F
  bne   #$0F, label_2D22
  ld    $02
  sub   #$04
  or    #$C0
  st    $02
label_2D22:
  pop   acc
  pop   b
  ret

  .byte $23,$00,$FF,$20,$1F,$47,$71,$04,$71,$05,$A0 ; #.. .Gq.q..

label_2D32:
  push  trh
  push  trl
  clr1  p3int, $00
  callf label_3C07
  mov   #$00, $60
  mov   #$00, $61
  mov   #$6B, trl
  mov   #$2D, trh
  callf label_3A05
  callf label_3B95
  set1  p3int, $00
  pop   trl
  pop   trh
  ret
label_2D54:
  clr1  psw, rambk0
  mov   #$00, $60
  mov   #$00, $61
  mov   #$88, trl
  mov   #$2D, trh
  callf label_3A05
  callf label_3B79
  set1  psw, rambk0
  ret

  .byte $57,$61,$69,$74,$69,$6E,$67,$20,$0D,$0A,$66,$6F,$72,$20,$64,$61 ; Waiting \r\nfor da
  .byte $74,$61,$0D,$0A,$20,$20,$20,$20,$20,$20,$20,$20,$00,$52,$65,$63 ; ta\r\n        .Rec
  .byte $69,$76,$69,$6E,$67,$0D,$0A,$20,$44,$6F,$20,$4E,$6F,$74,$20,$0D ; iving\r\n Do Not \r
  .byte $0A,$20,$20,$55,$6E,$70,$6C,$75,$67,$00 ; \n  Unplug.

label_2DA5:
  call  label_2E04
  callf label_3675
  mov   #$00, c
  mov   #$01, $33
  mov   #$00, $35
label_2DB3:
  callf label_3696
  be    #$FF, label_2DFF
  be    #$FD, label_2DFF
  be    #$10, label_2DCF
  be    #$20, label_2DCA
  be    #$04, label_2DD5
  be    #$08, label_2DD5
  br    label_2DB3
label_2DCA:
  set1  p3int, $00
  jmpf  label_1AEB
label_2DCF:
  ld    c
  bz    label_2DCA
  br    label_2DE3
label_2DD5:
  ld    c
  xor   #$FF
  st    c
  callf label_1F47
  callf label_3662
  br    label_2DB3
label_2DE3:
  call  label_2E51
  mov   #$FF, $6E
  callf label_3B79
  call  label_2ED8
  call  label_2E73
  mov   #$00, $6E
  callf label_3B95
  ld    $3D
  be    #$01, label_2DFC
  dec   $3D
label_2DFC:
  jmpf  label_1A34
label_2DFF:
  set1  p3int, $00
  jmpf  label_1B68
label_2E04:
  push  trh
  push  trl
  clr1  p3int, $00
  callf label_3C07
  mov   #$00, $60
  mov   #$00, $61
  ld    trl
  add   #$04
  st    trl
  mov   #$08, c
  mov   #$02, b
label_2E1F:
  ldf
  callf label_37D2
  inc   trl
  inc   $60
  dbnz  c, label_2E1F
  inc   $61
  mov   #$00, $60
  mov   #$04, c
  dbnz  b, label_2E1F
  mov   #$7D, trl
  mov   #$33, trh
  mov   #$00, $60
  mov   #$02, $61
  callf label_3A05
  mov   #$00, acc
  callf label_1F47
  set1  p3int, $00
  pop   trl
  pop   trh
  ret
label_2E51:
  push  trh
  push  trl
  clr1  p3int, $00
  mov   #$01, xbnk
  callf label_3BCD
  mov   #$86, trl
  mov   #$33, trh
  mov   #$00, $60
  mov   #$02, $61
  callf label_3A05
  set1  p3int, $00
  pop   trl
  pop   trh
  ret
label_2E73:
  mov   #$81, ocr
  clr1  p3int, $00
  set1  psw, rambk0
  clr1  ie, $07
  mov   #$01, $7D
  ld    trh
  st    $7E
  ld    trl
  and   #$80
  st    $7F
  push  trh
  push  trl
  callf label_E027
  pop   trl
  pop   trh
  mov   #$03, c
label_2E97:
  ld    trl
  and   #$7F
  or    #$80
  st    $00
  mov   #$00, acc
  mov   #$08, b
label_2EA5:
  st    @R0
  inc   $00
  st    @R0
  inc   $00
  st    @R0
  inc   $00
  st    @R0
  inc   $00
  dbnz  b, label_2EA5
  push  trh
  push  trl
  push  c
  callf label_E024
  pop   c
  callf label_E01B
  pop   trl
  pop   trh
  bz    label_2ECE
  dbnz  c, label_2E97
  callf label_30E8
label_2ECE:
  set1  ie, $07
  clr1  psw, rambk0
  set1  p3int, $00
  mov   #$A3, ocr
  ret
label_2ED8:
  push  trh
  push  trl
  clr1  p3int, $00
  clr1  ie, $07
  mov   #$81, ocr
  set1  psw, rambk0
  call  label_2F7A
  mov   #$01, fpr
  clr1  psw, rambk0
  mov   #$01, $6F
  set1  psw, rambk0
  mov   #$03, acc
  add   trl
  st    trl
  ldf
  bz    label_2EFE
  brf   label_2F68
label_2EFE:
  dec   trl
  ldf
label_2F01:
  clr1  psw, cy
  rolc
  push  acc
  st    vrmad1
  mov   #$00, acc
  rolc
  push  acc
  st    vrmad2
  ld    vtrbf
  bne   #$FA, label_2F25
  ld    vtrbf
  dec   vrmad1
  bz    label_2F25
  be    #$FF, label_2F44
  pop   acc
  pop   acc
  brf   label_2F70
label_2F25:
  st    b
  ld    vtrbf
  bz    label_2F32
  pop   acc
  pop   acc
  brf   label_2F70
label_2F32:
  pop   acc
  st    vrmad2
  pop   acc
  st    vrmad1
  mov   #$FC, vtrbf
  mov   #$FF, vtrbf
  ld    b
  br    label_2F01
label_2F44:
  ld    vtrbf
  pop   acc
  st    vrmad2
  pop   acc
  st    vrmad1
  mov   #$FC, vtrbf
  mov   #$FF, vtrbf
  call  label_2FCF
  ld    $5C
  bnz   label_2F70
label_2F5A:
  mov   #$A3, ocr
  set1  ie, $07
  clr1  psw, rambk0
  set1  p3int, $00
  pop   trl
  pop   trh
  ret
label_2F68:
  clr1  psw, rambk0
  ld    $5C
  or    #$80
  st    $5C
label_2F70:
  clr1  psw, rambk0
  ld    $5C
  or    #$08
  st    $5C
  br    label_2F5A
label_2F7A:
  push  trh
  push  trl
  push  acc
  push  b
  clr1  psw, rambk0
  mov   #$01, fpr
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$46, trl
  callf label_1CC2
  set1  psw, rambk0
  st    $54
  ld    trh
  st    $55
  ld    trl
  st    $56
  clr1  vrmad2, $00
  mov   #$00, vrmad1
  mov   #$04, b
label_2FA7:
  clr1  psw, rambk0
  ld    $6F
  set1  psw, rambk0
  st    $7D
  ld    trh
  st    $7E
  ld    trl
  st    $7F
  callf label_E027
  callf label_358D
  ld    trl
  bnz   label_2FC3
  inc   trh
label_2FC3:
  dbnz  b, label_2FA7
  pop   b
  pop   acc
  pop   trl
  pop   trh
  ret
label_2FCF:
  clr1  vrmad2, $00
  mov   #$00, vrmad1
  mov   #$04, b
label_2FD7:
  mov   #$03, c
  callf label_35AF
  ld    $54
  st    $7D
  ld    $55
  st    $7E
  ld    $56
  st    $7F
  callf label_E01B
  bnz   label_300E
label_2FEE:
  clr1  psw, rambk0
  ld    $6F
  set1  psw, rambk0
  st    $54
  clr1  psw, cy
  mov   #$80, acc
  add   trl
  st    $56
  st    trl
  mov   #$00, acc
  addc  trh
  st    $55
  st    trh
  dbnz  b, label_2FD7
  ret
label_300E:
  ld    $54
  st    $7D
  ld    $55
  st    $7E
  ld    $56
  st    $7F
  push  b
  push  c
  callf label_E024
  pop   c
  pop   b
  callf label_E01B
  bz    label_2FEE
  dbnz  c, label_300E
  callf label_303D
  ld    $5C
  bnz   label_3036
  br    label_2FCF
label_3036:
  ld    $5C
  or    #$08
  st    $5C
  ret
label_303D:
  push  acc
  push  b
  push  c
  set1  vrmad2, $00
  mov   #$00, vrmad1
  mov   #$01, fpr
  clr1  psw, rambk0
  mov   #$01, $6F
  set1  psw, rambk0
  mov   #$FE, trh
  mov   #$46, trl
  ldf
  clr1  psw, cy
  rolc
  st    vrmad1
  mov   #$FF, vtrbf
  mov   #$FF, vtrbf
  set1  vrmad2, $00
  mov   #$CC, vrmad1
label_3069:
  ld    vtrbf
  bne   #$FC, label_3079
  ld    vtrbf
  be    #$FF, label_3086
  ld    vrmad1
  sub   #$02
  st    vrmad1
label_3079:
  ld    vrmad1
  and   #$FE
  sub   #$02
  be    #$8E, label_30DB
  st    vrmad1
  br    label_3069
label_3086:
  ld    vrmad1
  sub   #$02
  st    vrmad1
  mov   #$FA, vtrbf
  mov   #$FF, vtrbf
  ror
  or    #$80
  push  acc
  mov   #$01, $7D
  mov   #$FE, $7E
  mov   #$00, $7F
  callf label_E027
  pop   acc
  st    $C6
  mov   #$00, $C7
  mov   #$03, c
  push  acc
label_30AF:
  push  c
  callf label_E024
  pop   c
  callf label_E01B
  bz    label_30C0
  dbnz  c, label_30AF
  br    label_30D1
label_30C0:
  pop   acc
  clr1  psw, cy
  rolc
  st    $55
  mov   #$00, acc
  st    $56
  rolc
  st    $54
  br    label_30E1
label_30D1:
  ld    $5C
  or    #$40
  st    $5C
  pop   acc
  br    label_30E1
label_30DB:
  ld    $5C
  or    #$04
  st    $5C
label_30E1:
  pop   c
  pop   b
  pop   acc
  ret
label_30E8:
  push  acc
  push  b
  push  c
  ld    trh
  and   #$01
  st    vrmad2
  ld    trl
  and   #$80
  st    vrmad1
  call  label_358D
  mov   #$04, b
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  clr1  psw, rambk0
  ld    $6F
  set1  psw, rambk0
  st    $7D
  ld    trh
  and   #$FE
  st    $7E
  mov   #$00, $7F
label_3115:
  push  trh
  push  trl
  callf label_E027
  pop   trl
  pop   trh
  ld    $7E
  and   #$01
  st    c
  ld    trh
  and   #$01
  xor   c
  bnz   label_313C
  ld    $7F
  and   #$80
  st    c
  ld    trl
  and   #$80
  xor   c
  bz    label_3140
label_313C:
  call  label_358D
  br    label_314E
label_3140:
  ld    vrmad1
  add   #$80
  st    vrmad1
  ld    vrmad2
  addc  #$00
  and   #$01
  st    vrmad2
label_314E:
  ld    $7F
  add   #$80
  st    $7F
  ld    $7E
  addc  #$00
  st    $7E
  dbnz  b, label_3115
  clr1  psw, rambk0
  push  $6F
  push  trh
  push  trl
  mov   #$01, fpr
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$46, trl
  callf label_1CC2
  set1  psw, rambk0
  st    $7D
  ld    trh
  or    #$01
  st    $7E
  mov   #$80, $7F
  callf label_E027
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  call  label_35D1
  pop   trl
  pop   trh
  clr1  psw, rambk0
  pop   $6F
  ld    $6F
  st    fpr
  set1  psw, rambk0
  ld    trh
  ror
  or    #$80
  st    $42
  st    $44
  mov   #$00, $43
  mov   #$00, $45
  ld    trh
  and   #$7E
  st    vrmad1
  clr1  vrmad2, $00
  ld    vtrbf
  st    $40
  ld    vtrbf
  st    $41
label_31B8:
  ld    $44
  rol
  and   #$7E
  st    vrmad1
  clr1  vrmad2, $00
  mov   #$FF, vtrbf
  mov   #$FF, vtrbf
  mov   #$4C, vrmad1
  mov   #$1F, b
label_31CD:
  ld    vtrbf
  be    #$FC, label_31E5
  ld    vtrbf
label_31D4:
  ld    vrmad1
  sub   #$04
  st    vrmad1
  dbnz  b, label_31CD
  ld    $5C
  or    #$04
  st    $5C
  jmp   label_3376
label_31E5:
  ld    vtrbf
  bne   #$FF, label_31D4
  dec   vrmad1
  dec   vrmad1
  ld    vrmad1
  or    #$80
  ror
  or    #$80
  st    $44
  ld    $40
  st    vtrbf
  ld    $41
  st    vtrbf
  ld    $44
  clr1  psw, cy
  rolc
  st    $7E
  mov   #$00, acc
  rolc
  st    $7D
  mov   #$00, $7F
  mov   #$80, vrmad1
  clr1  vrmad2, $00
  mov   #$04, c
  mov   #$03, b
label_321A:
  push  b
  push  c
  callf label_E024
  pop   c
  pop   b
  callf label_E01B
  bz    label_322F
  dbnz  b, label_321A
  jmp   label_31B8
label_322F:
  ld    $7F
  add   #$80
  st    $7F
  ld    $7E
  addc  #$00
  st    $7E
  call  label_35D1
  ld    vrmad1
  add   #$80
  st    vrmad1
  mov   #$00, acc
  addc  vrmad2
  and   #$01
  st    vrmad2
  mov   #$03, b
  dbnz  c, label_321A
  mov   #$01, fpr
  clr1  psw, rambk0
  mov   #$01, $6F
  set1  psw, rambk0
  mov   #$FE, trh
  mov   #$4A, trl
  ldf
  xor   $42
  bnz   label_32AE
  inc   trl
  ldf
  xor   $43
  dec   trl
  bnz   label_32AE
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  call  label_358D
  mov   #$01, $7D
  mov   #$FE, $7E
  mov   #$00, $7F
  callf label_E027
  ld    $44
  st    $CA
  ld    $45
  st    $CB
  mov   #$03, c
label_328E:
  push  c
  callf label_E024
  pop   c
  callf label_E01B
  bz    label_32A5
  dbnz  c, label_328E
  ld    $5C
  or    #$40
  st    $5C
  jmp   label_3376
label_32A5:
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  call  label_35AF
  jmp   label_32EA
label_32AE:
  ldf
label_32AF:
  and   #$7F
  rol
  st    $00
  ld    @R0
  st    b
  inc   $00
  ld    @R0
  st    c
  dec   $00
  ld    b
  be    #$FA, label_32DD
label_32C3:
  ld    b
  xor   $42
  bnz   label_32D9
  ld    c
  xor   $43
  bnz   label_32D9
  ld    $44
  st    @R0
  inc   $00
  ld    $45
  st    @R0
  jmp   label_32EA
label_32D9:
  ld    b
  jmp   label_32AF
label_32DD:
  ld    c
  bne   #$FF, label_32C3
  ld    $5C
  or    #$08
  st    $5C
  jmp   label_3376
label_32EA:
  mov   #$01, fpr
  clr1  psw, rambk0
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$46, trl
  callf label_1CC2
  set1  psw, rambk0
  st    $7D
  ld    trh
  or    #$01
  st    $7E
  mov   #$80, $7F
  mov   #$03, b
label_330B:
  push  b
  callf label_E024
  pop   b
  callf label_E01B
  bnz   label_3319
  jmp   label_3376
label_3319:
  dbnz  b, label_330B
  mov   #$80, vrmad1
  set1  vrmad2, $00
  call  label_358D
  mov   #$01, fpr
  clr1  psw, rambk0
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$46, trl
  callf label_1CC2
  set1  psw, rambk0
  st    $54
  st    $7D
  ld    trh
  st    $55
  st    $7E
  ld    trl
  st    $56
  st    $7F
  mov   #$00, vrmad1
  clr1  vrmad2, $00
  mov   #$03, c
label_334E:
  callf label_E027
  call  label_358D
  ld    $7F
  add   #$80
  st    $7F
  mov   #$00, acc
  addc  $7E
  st    $7E
  dbnz  c, label_334E
  callf label_303D
  ld    $5C
  bz    label_3371
  or    #$20
  st    $5C
  brf   label_3376
label_3371:
  callf label_2FCF
  ld    $5C
label_3376:
  pop   c
  pop   b
  pop   acc
  ret

  .byte $44,$65,$6C,$65,$74,$65,$20,$3F,$00,$44,$65,$6C,$65,$74,$65,$64 ; Delete ?.Deleted
  .byte $20,$00 ;  .

label_338F:
  callf label_E015
  mov   #$03, ie
  mov   #$02, ip
  clr1  btcr, $06
  clr1  btcr, $07
  clr1  isl, $04
  set1  btcr, $00
  set1  btcr, $06
  set1  ie, $07
  mov   #$09, mcr
  mov   #$05, cnr
  mov   #$20, tdr
  mov   #$80, vccr
  mov   #$00, stad
  mov   #$00, b
  mov   #$80, p1ddr
  mov   #$BF, p1fcr
  clr1  p1, $02
  clr1  p1, $05
  set1  p1, $06
  set1  p1, $07
  mov   #$00, p3ddr
  mov   #$05, p3int
  mov   #$FF, p3
  mov   #$00, i01cr
  mov   #$00, i23cr
  mov   #$00, scon0
  mov   #$00, scon1
  mov   #$FF, t1lr
  mov   #$FF, t1lc
  mov   #$00, t1cnt
  mov   #$00, t0con
  ret
label_33E6:
  ld    p7
  and   #$01
  bnz   label_3413
  mov   #$00, t1cnt
  clr1  p1, $07
  mov   #$FF, t1lc
  mov   #$FE, t1lr
  mov   #$50, t1cnt
  mov   #$00, $34
label_33FD:
  set1  pcon, $00
  ld    $34
  be    #$03, label_33FD
  bp    psw, cy, label_33FD
label_3407:
  set1  p1, $07
  mov   #$FF, t1lr
  mov   #$FF, t1lc
  mov   #$00, t1cnt
  ret
label_3413:
  mov   #$00, t1cnt
  clr1  p1, $07
  mov   #$FF, t1lc
  mov   #$FE, t1lr
  mov   #$50, t1cnt
  mov   #$00, $34
label_3424:
  set1  pcon, $00
  ld    $34
  bnz   label_3407
  br    label_3424
label_342C:
  push  psw
  set1  btcr, $05
  set1  btcr, $04
  clr1  btcr, $03
  clr1  psw, rambk0
  mov   #$00, $73
  set1  btcr, $02
  br    label_3448
label_343D:
  push  psw
  clr1  btcr, $03
  clr1  psw, rambk0
  mov   #$FF, $73
  clr1  btcr, $02
label_3448:
  pop   psw
  ret
label_344B:
  push  b
  push  c
  push  $00
  mov   #$52, $00
  st    @R0
  and   #$F0
  ror
  ror
  ror
  ror
  st    c
  mov   #$0A, b
  mov   #$00, acc
  mul
  ld    @R0
  and   #$0F
  add   c
  pop   $00
  pop   c
  pop   b
  ret
label_3470:
  push  b
  push  c
  push  $01
  push  $00
  mov   #$00, b
  mov   #$00, c
  mov   #$52, $01
  ld    @R0
  st    @R1
  inc   $00
  inc   $01
  ld    @R0
  st    @R1
  mov   #$52, $01
label_348C:
  ld    @R1
  be    @R1, #$10, label_3493
  bp    psw, cy, label_34A4
label_3493:
  sub   #$10
  st    @R1
  ld    b
  add   #$E8
  st    b
  ld    c
  addc  #$03
  st    c
  br    label_348C
label_34A4:
  ld    @R1
  bz    label_34B8
  sub   #$01
  st    @R1
  ld    b
  add   #$64
  st    b
  ld    c
  addc  #$00
  st    c
  br    label_34A4
label_34B8:
  inc   $01
label_34BA:
  ld    @R1
  be    @R1, #$10, label_34C1
  bp    psw, cy, label_34D2
label_34C1:
  sub   #$10
  st    @R1
  ld    b
  add   #$0A
  st    b
  ld    c
  addc  #$00
  st    c
  br    label_34BA
label_34D2:
  ld    @R1
  bz    label_34E6
  sub   #$01
  st    @R1
  ld    b
  add   #$01
  st    b
  ld    c
  addc  #$00
  st    c
  br    label_34D2
label_34E6:
  mov   #$52, $01
  ld    c
  st    @R1
  inc   $01
  ld    b
  st    @R1
  pop   $00
  pop   $01
  pop   c
  pop   b
  ret
label_34FA:
  push  b
  push  c
  push  $00
  st    c
  mov   #$0A, b
  mov   #$00, acc
  div
  ld    c
  rol
  rol
  rol
  rol
  and   #$F0
  add   b
  pop   $00
  pop   c
  pop   b
  ret
label_351A:
  push  b
  push  c
  push  $01
  push  $00
  mov   #$00, b
  mov   #$00, c
  mov   #$52, $01
  ld    @R0
  st    @R1
  inc   $00
  inc   $01
  ld    @R0
  st    @R1
  st    c
  dec   $01
  ld    @R1
  mov   #$64, b
  div
  mov   #$0A, b
  div
  ld    c
  and   #$0F
  st    b
  rol
  rol
  rol
  rol
  st    @R1
  mov   #$03, acc
  mov   #$E8, c
  mul
  st    b
  ld    @R0
  sub   c
  st    c
  dec   $00
  ld    @R0
  subc  b
  mov   #$64, b
  div
  ld    c
  and   #$0F
  or    @R1
  st    @R1
  inc   $01
  ld    b
  st    c
  mov   #$00, acc
  mov   #$0A, b
  div
  ld    c
  rol
  rol
  rol
  rol
  and   #$F0
  st    @R1
  ld    b
  and   #$0F
  or    @R1
  st    @R1
  pop   $00
  pop   $01
  pop   c
  pop   b
  ret
label_358D:
  push  b
  mov   #$80, $00
  mov   #$20, b
label_3595:
  ld    @R0
  st    vtrbf
  inc   $00
  ld    @R0
  st    vtrbf
  inc   $00
  ld    @R0
  st    vtrbf
  inc   $00
  ld    @R0
  st    vtrbf
  inc   $00
  dbnz  b, label_3595
  pop   b
  ret
label_35AF:
  push  b
  mov   #$80, $00
  mov   #$20, b
label_35B7:
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  dbnz  b, label_35B7
  pop   b
  ret
label_35D1:
  push  b
  push  c
  push  vrmad1
  push  vrmad2
  mov   #$80, $00
  mov   #$04, b
label_35DF:
  mov   #$08, c
  mov   #$10, $01
label_35E5:
  ld    @R0
  st    @R1
  inc   $00
  inc   $01
  ld    @R0
  st    @R1
  inc   $00
  inc   $01
  ld    @R0
  st    @R1
  inc   $00
  inc   $01
  ld    @R0
  st    @R1
  inc   $00
  inc   $01
  dbnz  c, label_35E5
  ld    $00
  sub   #$20
  st    $00
  mov   #$08, c
label_3609:
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  ld    vtrbf
  st    @R0
  inc   $00
  dbnz  c, label_3609
  ld    vrmad1
  sub   #$20
  st    vrmad1
  mov   #$10, $01
  mov   #$08, c
label_362C:
  ld    @R1
  st    vtrbf
  inc   $01
  ld    @R1
  st    vtrbf
  inc   $01
  ld    @R1
  st    vtrbf
  inc   $01
  ld    @R1
  st    vtrbf
  inc   $01
  dbnz  c, label_362C
  dbnz  b, label_35DF
  pop   vrmad2
  pop   vrmad1
  pop   c
  pop   b
  ret

  .byte $F8,$4E,$F8,$07,$D8,$4E,$03,$5C,$E1,$01,$90,$06,$02,$70,$E1,$30 ; .N...N.\.....p.0
  .byte $90,$EE,$A0

label_3662:
  set1  p3int, $00
  set1  pcon, $00
  clr1  p3int, $00
  ld    p7
  and   #$01
  bnz   label_3674
  ld    $70
  and   #$0F
  bnz   label_3662
label_3674:
  ret
label_3675:
  set1  p3int, $00
  set1  pcon, $00
  clr1  p3int, $00
  ld    p7
  and   #$01
  bnz   label_3685
  ld    $70
  bnz   label_3675
label_3685:
  ret
label_3686:
  set1  psw, rambk0
  mov   #$00, $00
label_368B:
  mov   #$00, @R0
  inc   $00
  ld    $00
  bnz   label_368B
  clr1  psw, rambk0
  ret
label_3696:
  push  b
  push  psw
  clr1  psw, rambk0
  ld    $30
  st    b
label_36A0:
  set1  p3int, $00
  set1  pcon, $00
  clr1  p3int, $00
  call  label_3760
  bnz   label_36D3
  ld    $70
  and   #$10
  bnz   label_36D3
  ld    $70
  and   #$20
  bnz   label_36D3
  ld    $70
  and   #$04
  bnz   label_36D3
  ld    $70
  and   #$08
  bnz   label_36D3
  ld    $34
  bz    label_36A0
  mov   #$00, $34
  ld    $70
  and   #$01
  bnz   label_36D3
  ld    $70
  and   #$02
label_36D3:
  pop   psw
  pop   b
  ret
label_36D8:
  push  b
  push  psw
  clr1  psw, rambk0
  ld    $30
  st    b
label_36E2:
  set1  p3int, $00
  set1  pcon, $00
  clr1  p3int, $00
  call  label_3760
  bnz   label_3715
  ld    $70
  and   #$10
  bnz   label_3715
  ld    $70
  and   #$20
  bnz   label_3715
  ld    $34
  bz    label_36E2
  mov   #$00, $34
  ld    $70
  and   #$01
  bnz   label_3715
  ld    $70
  and   #$02
  bnz   label_3715
  ld    $70
  and   #$04
  bnz   label_3715
  ld    $70
  and   #$08
label_3715:
  brf   label_36D3
label_3718:
  push  b
  push  psw
  clr1  psw, rambk0
  ld    $30
  st    b
  call  label_342C
  mov   #$00, $74
label_3727:
  set1  p3int, $00
  set1  pcon, $00
  clr1  p3int, $00
  call  label_3760
  bnz   label_375B
  ld    $70
  and   #$10
  bnz   label_375B
  ld    $70
  and   #$20
  bnz   label_375B
  ld    $74
  be    #$04, label_3727
  bp    psw, cy, label_3727
  ld    $70
  and   #$01
  bnz   label_375B
  ld    $70
  and   #$02
  bnz   label_375B
  ld    $70
  and   #$04
  bnz   label_375B
  ld    $70
  and   #$08
label_375B:
  call  label_343D
  brf   label_36D3
label_3760:
  ld    p7
  and   #$01
  bnz   label_3798
  ld    $33
  bz    label_379C
  ld    $35
  and   #$01
  bnz   label_379C
  ld    $30
  xor   b
  bnz   label_37CE
  ld    p7
  and   #$09
  be    #$08, label_377F
  br    label_3794
label_377F:
  ld    $2B
  bz    label_3794
  be    #$FF, label_3794
  ld    $70
  bne   #$10, label_3790
  mov   #$FF, $2B
  br    label_3794
label_3790:
  mov   #$FC, acc
  ret
label_3794:
  mov   #$00, acc
  ret
label_3798:
  mov   #$FF, acc
  ret
label_379C:
  ld    $33
  bnz   label_37A2
  br    label_37AB
label_37A2:
  ld    $35
  be    #$01, label_37B8
  and   #$01
  bz    label_3794
label_37AB:
  ld    $35
  bz    label_37B8
  and   #$C0
  be    #$40, label_3794
  mov   #$FE, acc
  ret
label_37B8:
  ld    $73
  bnz   label_37C7
  call  label_343D
  callf label_0421
  call  label_342C
  clr1  p3int, $00
  br    label_3794
label_37C7:
  callf label_0421
  clr1  p3int, $00
  br    label_3794
label_37CE:
  mov   #$FD, acc
  ret
label_37D2:
  push  acc
  push  b
  push  c
  push  $60
  push  $61
  push  trh
  push  trl
  push  ocr
  mov   #$81, ocr
  mov   #$00, trl
  mov   #$E8, trh
  add   trl
  st    trl
  mov   #$00, $64
  ld    $60
  be    #$07, label_37FC
  bp    psw, cy, label_37FC
  jmp   label_38CF
label_37FC:
  ld    $61
  be    #$03, label_3806
  bp    psw, cy, label_3806
  jmp   label_38CF
label_3806:
  ld    $60
  st    c
  mov   #$00, acc
  mov   #$06, b
  mul
  ld    c
  st    $60
  and   #$07
  st    $66
  ld    $60
  and   #$38
  ror
  ror
  ror
  or    #$80
  st    $65
  ld    $61
  rol
  rol
  rol
  st    $61
  and   #$08
  bz    label_3835
  ld    $65
  add   #$40
  st    $65
label_3835:
  ld    $61
  and   #$10
  bz    label_383E
  mov   #$01, $64
label_383E:
  push  trl
  push  trh
  mov   #$E0, trl
  mov   #$38, trh
  ld    $66
  ldc
  st    $6A
  mov   #$E8, trl
  mov   #$38, trh
  ld    $66
  ldc
  st    $6B
  pop   trh
  pop   trl
  ld    $64
  st    xbnk
  ld    $65
  st    $02
  inc   acc
  st    $03
  mov   #$08, c
label_386B:
  mov   #$00, $68
  mov   #$00, $69
  mov   #$00, acc
  ldc
  st    $68
  ld    $67
  bz    label_3881
  ld    $68
  xor   #$FC
  st    $68
label_3881:
  ld    $66
  st    b
  bz    label_3896
label_3887:
  clr1  psw, cy
  ld    $68
  rorc
  st    $68
  ld    $69
  rorc
  st    $69
  dbnz  b, label_3887
label_3896:
  ld    @R2
  and   $6A
  or    $68
  st    @R2
  ld    $66
  be    #$02, label_38AA
  bp    psw, cy, label_38AA
  ld    @R3
  and   $6B
  or    $69
  st    @R3
label_38AA:
  ld    $02
  and   #$0F
  be    #$06, label_38C0
  bn    psw, cy, label_38C0
  ld    $02
  add   #$06
  st    $02
  inc   acc
  st    $03
  br    label_38CA
label_38C0:
  ld    $02
  add   #$0A
  st    $02
  inc   acc
  st    $03
label_38CA:
  inc   trh
  dbnz  c, label_386B
label_38CF:
  pop   ocr
  pop   trl
  pop   trh
  pop   $61
  pop   $60
  pop   c
  pop   b
  pop   acc
  ret

  .byte $03,$81,$C0,$E0,$F0,$F8,$FC,$FE,$FF,$FF,$FF,$7F,$3F,$1F,$0F,$07 ; ............?...

label_38F0:
  push  acc
  push  b
  push  c
  push  trl
  push  trh
  push  ocr
  mov   #$81, ocr
  mov   #$00, xbnk
  rol
  rol
  rol
  st    b
  and   #$F8
  mov   #$73, trl
  mov   #$04, trh
  add   trl
  st    trl
  ld    b
  and   #$01
  addc  trh
  st    trh
  ld    $60
  be    #$06, label_3994
  bn    psw, cy, label_3994
  or    #$80
  st    $65
  ld    $61
  be    #$04, label_3994
  bn    psw, cy, label_3994
  ror
  ror
  and   #$C0
  st    $68
  and   #$80
  bz    label_393C
  mov   #$01, xbnk
label_393C:
  ld    $68
  and   #$40
  add   $65
  st    $02
  ld    $67
  bnz   label_396D
  mov   #$00, acc
  mov   #$04, c
  st    b
label_3950:
  ld    b
  ldc
  st    @R2
  inc   b
  ld    $02
  add   #$06
  st    $02
  ld    b
  ldc
  st    @R2
  inc   b
  ld    $02
  add   #$0A
  st    $02
  dbnz  c, label_3950
  br    label_3994
label_396D:
  mov   #$00, acc
  mov   #$04, c
  st    b
label_3975:
  ld    b
  ldc
  xor   #$FF
  st    @R2
  inc   b
  ld    $02
  add   #$06
  st    $02
  ld    b
  ldc
  xor   #$FF
  st    @R2
  inc   b
  ld    $02
  add   #$0A
  st    $02
  dbnz  c, label_3975
label_3994:
  pop   ocr
  pop   trh
  pop   trl
  pop   c
  pop   b
  pop   acc
  ret
label_39A1:
  push  acc
  push  b
  push  ocr
  mov   #$81, ocr
  mov   #$00, xbnk
  ld    $60
  be    #$06, label_39FE
  bn    psw, cy, label_39FE
  or    #$80
  st    $65
  ld    $61
  be    #$04, label_39FE
  bn    psw, cy, label_39FE
  ror
  ror
  and   #$C0
  st    $68
  and   #$80
  bz    label_39CE
  mov   #$01, xbnk
label_39CE:
  ld    $68
  and   #$40
  add   $65
  st    $02
  mov   #$04, c
  ld    $67
  bz    label_39E4
  mov   #$FF, acc
  st    b
  br    label_39E9
label_39E4:
  mov   #$00, acc
  st    b
label_39E9:
  ld    b
  st    @R2
  ld    $02
  add   #$06
  st    $02
  ld    b
  st    @R2
  ld    $02
  add   #$0A
  st    $02
  dbnz  c, label_39E9
label_39FE:
  pop   ocr
  pop   b
  pop   acc
  ret
label_3A05:
  push  acc
  push  b
  push  trh
  push  trl
  push  $60
  push  $61
  mov   #$00, acc
  st    b
label_3A16:
  ld    b
  inc   b
  ldc
  bz    label_3A34
  be    #$0D, label_3A41
  be    #$0A, label_3A50
  call  label_37D2
  inc   $60
  ld    $60
  be    #$07, label_3A16
  bp    psw, cy, label_3A16
  mov   #$00, $60
  br    label_3A16
label_3A34:
  pop   $61
  pop   $60
  pop   trl
  pop   trh
  pop   b
  pop   acc
  ret
label_3A41:
  inc   $61
  ld    $61
  be    #$03, label_3A16
  bp    psw, cy, label_3A16
  mov   #$00, $61
  br    label_3A16
label_3A50:
  mov   #$00, $60
  br    label_3A16

  .byte $60,$60,$60,$61,$61,$05,$61,$04,$60,$00,$61,$02,$61,$00,$23,$02 ; ```aa.a.`.a.a.#.
  .byte $02,$61,$02,$04,$13,$02,$E1,$F0,$C0,$C0,$C0,$C0,$1B,$6E,$0F,$D2 ; .a...........n..
  .byte $62,$60,$03,$02,$E1,$0F,$1B,$6E,$0F,$D2,$62,$00,$62,$60,$71,$02 ; b`.....n..b.b`q.
  .byte $53,$02,$DE,$71,$00,$71,$02,$70,$00,$71,$04,$71,$05,$70,$61,$70 ; S..q.q.p.q.q.pap
  .byte $60,$A0 ; `.

label_3A97:
  push  $60
  push  $61
  push  trh
  push  trl
  push  $00
  push  b
  push  acc
  ld    @R0
  and   #$0F
  call  label_3B6E
  call  label_37D2
  inc   $00
  inc   $60
  ld    @R0
  st    b
  and   #$F0
  ror
  ror
  ror
  ror
  call  label_3B6E
  call  label_37D2
  inc   $60
  ld    b
  and   #$0F
  call  label_3B6E
  call  label_37D2
  pop   acc
  pop   b
  pop   $00
  pop   trl
  pop   trh
  pop   $61
  pop   $60
  ret
label_3AD6:
  push  $60
  push  $61
  push  trh
  push  trl
  push  b
  push  acc
  st    b
  and   #$F0
  ror
  ror
  ror
  ror
  call  label_3B6E
  call  label_37D2
  inc   $60
  ld    b
  and   #$0F
  call  label_3B6E
  call  label_37D2
  pop   acc
  pop   b
  pop   trl
  pop   trh
  pop   $61
  pop   $60
  ret
label_3B05:
  push  $60
  push  $61
  push  trh
  push  trl
  push  b
  push  acc
  st    b
  and   #$F0
  ror
  ror
  ror
  ror
  call  label_38F0
  inc   $60
  ld    b
  and   #$0F
  call  label_38F0
  pop   acc
  pop   b
  pop   trl
  pop   trh
  pop   $61
  pop   $60
  ret
label_3B30:
  push  $60
  push  $61
  push  trh
  push  trl
  push  $00
  push  b
  push  acc
  mov   #$02, b
label_3B41:
  push  b
  ld    @R0
  st    b
  and   #$F0
  ror
  ror
  ror
  ror
  call  label_38F0
  inc   $60
  ld    b
  and   #$0F
  call  label_38F0
  inc   $00
  inc   $60
  pop   b
  dbnz  b, label_3B41
  pop   acc
  pop   b
  pop   $00
  pop   trl
  pop   trh
  pop   $61
  pop   $60
  ret
label_3B6E:
  be    #$09, label_3B76
  bp    psw, cy, label_3B76
  add   #$27
label_3B76:
  add   #$30
  ret
label_3B79:
  push  acc
  push  ocr
  ld    ocr
  be    #$81, label_3B85
  mov   #$81, ocr
label_3B85:
  mov   #$02, xbnk
  mov   #$01, xram_0184
label_3B8B:
  pop   ocr
  ld    ocr
  be    #$81, label_3B92
label_3B92:
  pop   acc
  ret
label_3B95:
  push  acc
  push  ocr
  ld    ocr
  be    #$81, label_3BA1
  mov   #$81, ocr
label_3BA1:
  mov   #$02, xbnk
  mov   #$00, xram_0184
  br    label_3B8B
label_3BA9:
  push  b
  call  label_3C07
  ld    $67
  st    b
  mov   #$02, xbnk
  mov   #$80, $02
label_3BB7:
  ld    b
  push  ocr
  mov   #$81, ocr
  st    @R2
  pop   ocr
  inc   $02
  ld    $02
  be    #$86, label_3BCA
  br    label_3BB7
label_3BCA:
  pop   b
  ret
label_3BCD:
  push  acc
  push  b
  mov   #$80, $02
  ld    $67
  st    b
label_3BD8:
  ld    b
  push  ocr
  mov   #$81, ocr
  st    @R2
  inc   $02
  st    @R2
  inc   $02
  st    @R2
  inc   $02
  st    @R2
  inc   $02
  st    @R2
  inc   $02
  st    @R2
  inc   $02
  pop   ocr
  ld    $02
  and   #$0F
  bne   #$0C, label_3BD8
  ld    $02
  add   #$04
  st    $02
  bnz   label_3BD8
  pop   b
  pop   acc
  ret
label_3C07:
  mov   #$00, xbnk
  call  label_3BCD
  mov   #$01, xbnk
  call  label_3BCD
  ret
label_3C12:
  push  ocr
  mov   #$81, ocr
  mov   #$02, xbnk
  mov   #$00, xram_0181
  mov   #$10, xram_0182
  mov   #$00, xram_0183
  mov   #$00, xbnk
  pop   ocr
  mov   #$00, $6E
  mov   #$01, fpr
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$00, trl
  mov   #$10, b
label_3C3A:
  ldf
  xor   #$55
  bnz   label_3C46
  inc   trl
  dbnz  b, label_3C3A
  br    label_3C4B
label_3C46:
  mov   #$FF, acc
  br    label_3C7A
label_3C4B:
  call  label_3CE6
  bnz   label_3C7A
  mov   #$A3, ocr
  mov   #$7F, sp
  set1  psw, rambk0
  not1  ext, $00
  jmpf  $00

  .byte $23,$0E,$A3,$23,$06,$7F,$D9,$01,$0B,$8F,$20,$10,$07,$20,$10,$17 ; #..#...... .. ..
  .byte $D8,$63,$D9,$63,$DA,$60,$22,$30,$02,$0E,$86,$21,$02,$EF ; .c.c.`"0...!..

label_3C7A:
  clr1  p3int, $00
  mov   #$01, xbnk
  call  label_3BCD
  be    #$FE, label_3C8F
  be    #$FD, label_3C97
  mov   #$0E, trl
  mov   #$04, trh
  br    label_3C9D
label_3C8F:
  mov   #$A3, trl
  mov   #$3D, trh
  br    label_3C9D
label_3C97:
  mov   #$B6, trl
  mov   #$3D, trh
label_3C9D:
  mov   #$00, $60
  mov   #$00, $61
  call  label_3A05
  mov   #$01, $33
label_3CA8:
  call  label_3696
  be    #$FF, label_3CB2
  be    #$FD, label_3CB2
  br    label_3CA8
label_3CB2:
  call  label_3686
  jmpf  label_02EF

  .byte $1B,$79,$02,$7D,$90,$24,$61,$01,$D9,$01,$02,$6D,$71,$01,$80,$1A ; .y.}.$a....mq...
  .byte $FF,$01,$F0,$7F,$01,$14,$A2,$7E,$7F,$01,$0F,$02,$7F,$E1,$80,$12 ; .......~........
  .byte $7F,$20,$E0,$24,$23,$00,$00,$1B,$95,$A0,$23,$00,$FF,$01,$F8 ; . .$#.....#....

label_3CE6:
  mov   #$01, fpr
  mov   #$01, $6F
  mov   #$FE, trh
  mov   #$46, trl
  callf label_1CC2
  ld    $6F
  st    $54
  ld    trh
  st    $55
  ld    trl
  st    $56
  mov   #$FE, trh
  mov   #$4A, trl
  callf label_1CC2
  ld    $52
  st    $58
label_3D0E:
  mov   #$10, c
label_3D11:
  ldf
  bz    label_3D17
  be    #$CC, label_3D6D
label_3D17:
  ld    trl
  add   #$20
  st    trl
  ld    trh
  addc  #$00
  st    trh
  dbnz  c, label_3D11
  ld    $54
  st    fpr
  st    $6F
  ld    $55
  st    trh
  ld    $56
  st    trl
  ld    $58
  clr1  psw, cy
  rolc
  st    c
  mov   #$00, acc
  rolc
  st    $52
  ld    trl
  add   c
  st    trl
  ld    trh
  add   $52
  st    trh
  ldf
  bne   #$FA, label_3D5D
  inc   trl
  ldf
  dec   trl
  be    #$FF, label_3D9B
  bz    label_3D5D
  br    label_3D9B
label_3D5D:
  inc   trl
  ldf
  bnz   label_3D9B
  dec   trl
  callf label_1CC2
  ld    $52
  st    $58
  br    label_3D0E
label_3D6D:
  inc   trl
  inc   trl
  ldf
  bnz   label_3D9F
  inc   trl
  ldf
  bnz   label_3D9F
  ld    trl
  add   #$16
  st    trl
  ldf
  bnz   label_3D9F
  dec   trl
  ldf
  bz    label_3D9F
  dec   acc
  st    $6D
  inc   trl
  inc   trl
  ldf
  bz    label_3D9F
  inc   trl
  ldf
  bnz   label_3D9F
  mov   #$00, acc
  ret
label_3D9B:
  mov   #$FE, acc
  ret
label_3D9F:
  mov   #$FD, acc
  ret

  .byte $4E,$6F,$20,$73,$61,$76,$65,$64,$0D,$0A,$20,$20,$20,$20,$67,$61 ; No saved\r\n    ga
  .byte $6D,$65,$00,$47,$61,$6D,$65,$20,$69,$73,$20,$0D,$0A,$20,$64,$61 ; me.Game is \r\n da
  .byte $6D,$61,$67,$65,$64,$00 ; maged.

label_3DC9:
  push  acc
  push  b
  push  c
  ld    psw
  st    b
  clr1  psw, rambk0
  ld    $73
  bnz   label_3DE0
  bn    btcr, $03, label_3DE0
  clr1  btcr, $03
  inc   $74
label_3DE0:
  bn    btcr, $01, label_3E33
  clr1  btcr, $01
  callf label_E02A
  ld    p3
  xor   #$FF
  bnz   label_3DF5
  ld    $70
  st    $72
  mov   #$00, $70
label_3DF5:
  ld    $6E
  be    #$FF, label_3E2F
  ld    p7
  and   #$02
  bnz   label_3E2F
  inc   $6E
  ld    $6E
  be    #$06, label_3E2F
  bp    psw, cy, label_3E2F
  mov   #$16, trl
  mov   #$3F, trh
  mov   #$00, $67
  mov   #$00, $60
  mov   #$00, $61
  callf label_3C07
  call  label_3A05
  mov   #$7F, sp
  mov   #$3E, acc
  push  acc
  mov   #$3E, acc
  push  acc
  mov   #$FF, $6E
  reti
label_3E2F:
  inc   $33
  inc   $34
label_3E33:
  ld    b
  st    psw
  pop   c
  pop   b
  pop   acc
  ret

  .byte $23,$0E,$A3,$20,$33,$8F,$20,$10,$07,$20,$10,$17,$20,$36,$86,$D8 ; #.. 3. .. .. 6..
  .byte $63,$D9,$63,$DA,$60,$22,$33,$01,$20,$36,$96,$31,$FF,$4B,$31,$FE ; c.c.`"3. 6.1.K1.
  .byte $02,$01,$F5,$20,$04,$21,$01,$F0 ; ... .!..

label_3E66:
  push  acc
  push  psw
  clr1  psw, rambk0
  mov   #$01, $33
  ld    $70
  st    $72
  ld    p3
  xor   #$FF
  st    $70
  xor   $72
  and   $70
  st    $71
  and   #$40
  bz    label_3E98
  inc   $30
  ld    $30
  be    #$02, label_3E98
  bp    psw, cy, label_3E98
  bne   #$03, label_3E95
  mov   #$00, $30
  br    label_3E98
label_3E95:
  mov   #$02, $30
label_3E98:
  ld    $71
  and   #$80
  bz    label_3EA0
  not1  $35, $00
label_3EA0:
  clr1  p3int, $01
  pop   psw
  pop   acc
  ret
label_3EA7:
  clr1  p3int, $00
  call  label_3BA9
  clr1  psw, rambk0
  mov   #$FF, $6E
  mov   #$A2, ocr
  nop
  nop
  mov   #$92, ocr
  mov   #$AD, p1ddr
  mov   #$BF, p1fcr
  set1  p1, $06
  clr1  p1, $07
  mov   #$00, t1cnt
  callf label_1007
  callf label_1017
label_3ECB:
  ld    mplsta
  bp    acc, $06, label_3EE2
  ld    p7
  xor   #$FF
  and   #$0C
  sub   $23
  bz    label_3EE2
  ld    p7
  and   #$01
  bz    label_3EED
  br    label_3ECB
label_3EE2:
  callf label_1007
  callf label_1017
  callf label_3C07
  br    label_3ECB
label_3EED:
  mov   #$80, p1ddr
  mov   #$BF, p1fcr
  callf label_3686
  mov   #$FF, t1lr
  mov   #$FF, t1lc
  mov   #$00, t1cnt
  set1  p3int, $00
  clr1  vsel, $00
  clr1  vsel, $01
  clr1  mplsw, $02
  mov   #$A3, ocr
  mov   #$00, $6E
  mov   #$00, $30
  mov   #$01, $35
  jmpf  label_02EF

  .byte $50,$6C,$65,$61,$73,$65,$20,$20,$0D,$0A,$20,$63,$68,$61,$6E,$67 ; Please  \r\n chang
  .byte $65,$20,$0D,$0A,$20,$62,$61,$74,$74,$65,$72,$79,$00,$FF,$FF,$FF ; e \r\n battery....
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$00,$00,$00,$00,$00,$00

  .org $7FF6
  .byte $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$11,$FE,$00,$11,$BC,$01
  .byte $11,$FB,$01,$11,$6D,$02,$11,$8C,$02,$11,$E6,$02,$11,$05,$03 ; ....m..........

label_E015:
  brf   label_E33B

  .byte $11,$2E,$03 ; ...

label_E01B:
  brf   label_E388

  .byte $11,$8C,$03,$11,$9A,$03

label_E024:
  brf   label_E3CE
label_E027:
  brf   label_E3F6
label_E02A:
  brf   label_E412
label_E02D:
  brf   label_E4A0

  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $23,$06,$5F,$23,$4D,$FF,$23,$4C,$00,$23,$45,$FF,$23,$44,$00,$10 ; #._#M.#L.#E.#D..
  .byte $2A,$02,$23,$02,$FF,$10,$17,$00,$10,$42,$00,$23,$02,$00,$10,$0E ; *.#......B.#....
  .byte $00,$10,$39,$00,$23,$02,$FF,$10,$05,$00,$F9,$07,$01,$FC,$23,$05 ; ..9.#.........#.
  .byte $00,$23,$04,$00,$22,$7D,$00,$02,$7D,$13,$54,$03,$02,$51,$03,$04 ; .#.."}..}.T..Q..
  .byte $E1,$7F,$41,$7F,$03,$10,$65,$00,$63,$04,$03,$04,$90,$ED,$63,$05 ; ..A...e.c.....c.
  .byte $03,$05,$90,$E7,$62,$7D,$02,$7D,$41,$02,$DE,$A0,$23,$05,$00,$23 ; ....b}.}A...#..#
  .byte $04,$00,$22,$7D,$00,$02,$7D,$13,$54,$50,$33,$02,$29,$03,$04,$F0 ; .."}..}.TP3.)...
  .byte $03,$05,$F0,$13,$4C,$03,$02,$E1,$02,$D2,$7D,$F0,$13,$44,$23,$00 ; ....L.....}..D#.
  .byte $FA,$98,$5C,$FD,$53,$00,$FA,$23,$00,$FA,$78,$5C,$FD,$53,$00,$FA ; ..\.S..#..x\.S..
  .byte $03,$04,$D1,$7F,$13,$04,$63,$04,$03,$04,$90,$CD,$63,$05,$03,$05 ; ......c.....c...
  .byte $90,$C7,$62,$7D,$02,$7D,$41,$02,$BE,$FB,$44,$A0,$50,$13,$03,$BE ; ..b}.}A...D.P...
  .byte $03,$BE,$03,$50,$F3,$03,$7E,$00,$F8,$A0,$50,$F3,$02,$7F,$00,$FA ; ...P..~...P.....
  .byte $A0,$23,$06,$5F,$23,$46,$00,$23,$45,$FF,$23,$4D,$FF,$23,$4C,$00 ; .#._#F.#E.#M.#L.
  .byte $10,$69,$01,$22,$77,$00,$22,$78,$14,$22,$79,$88,$23,$02,$00,$10 ; .i."w."x."y.#...
  .byte $4D,$FF,$23,$02,$FF,$10,$47,$FF,$52,$79,$F1,$52,$78,$EE,$10,$4F ; M.#...G.Ry.Rx..O
  .byte $00,$02,$77,$13,$44,$62,$77,$02,$76,$13,$4C,$D2,$75,$80,$D7,$F9 ; ..w.Dbw.v.L.u...
  .byte $07,$01,$FC,$23,$06,$5F,$23,$46,$00,$23,$45,$FF,$23,$4D,$FF,$23 ; ...#._#F.#E.#M.#
  .byte $4C,$00,$10,$27,$01,$22,$77,$00,$22,$78,$14,$22,$79,$88,$23,$02 ; L..'."w."x."y.#.
  .byte $00,$10,$0B,$FF,$23,$02,$FF,$10,$05,$FF,$52,$79,$F1,$52,$78,$EE ; ....#.....Ry.Rx.
  .byte $10,$0D,$00,$02,$77,$13,$44,$62,$77,$02,$76,$13,$4C,$01,$D9,$22 ; .\r..w.Dbw.v.L.."
  .byte $75,$00,$22,$76,$00,$23,$05,$00,$23,$04,$00,$22,$7D,$00,$02,$7D ; u."v.#..#.."}..}
  .byte $13,$54,$50,$33,$02,$0E,$62,$76,$02,$76,$90,$02,$62,$75,$03,$04 ; .TP3..bv.v..bu..
  .byte $D1,$7F,$13,$04,$63,$04,$03,$04,$90,$E8,$63,$05,$03,$05,$90,$E2 ; ....c.....c.....
  .byte $62,$7D,$02,$7D,$41,$02,$D9,$A0,$23,$06,$5F,$10,$BE,$00,$22,$78 ; b}.}A...#._..."x
  .byte $28,$22,$79,$10,$23,$02,$00,$10,$A5,$FE,$23,$02,$FF,$10,$9F,$FE ; ("y.#.....#.....
  .byte $52,$79,$F1,$52,$78,$EE,$F9,$07,$01,$FC,$23,$06,$5F,$23,$46,$00 ; Ry.Rx.....#._#F.
  .byte $23,$45,$FF,$23,$44,$00,$10,$93,$00,$23,$54,$00,$23,$05,$00,$23 ; #E.#D....#T.#..#
  .byte $04,$00,$23,$02,$00,$10,$1B,$00,$22,$77,$00,$23,$04,$00,$23,$03 ; ..#....."w.#..#.
  .byte $0A,$10,$21,$00,$90,$08,$62,$77,$02,$77,$13,$44,$01,$ED,$F9,$07 ; \n.!...bw.w.D....
  .byte $01,$FC,$03,$02,$51,$03,$04,$E1,$7F,$31,$7F,$04,$63,$04,$01,$F2 ; ....Q....1..c...
  .byte $10,$CA,$FE,$A0,$50,$F3,$02,$90,$0D,$63,$04,$03,$04,$E1,$7F,$90 ; ....P...\rc......
  .byte $F3,$BF,$04,$53,$03,$EE,$A0,$23,$06,$5F,$10,$3F,$00,$22,$78,$C4 ; ...S...#._.?."x.
  .byte $22,$79,$50,$23,$02,$00,$10,$26,$FE,$23,$02,$FF,$10,$20,$FE,$52 ; "yP#...&.#... .R
  .byte $79,$F1,$52,$78,$EE,$F9,$07,$01,$FC,$23,$06,$5F,$10,$1D,$00,$22 ; y.Rx.....#._..."
  .byte $78,$01,$22,$79,$FC,$23,$02,$00,$10,$04,$FE,$23,$02,$FF,$10,$FE ; x."y.#.....#....
  .byte $FD,$52,$79,$F1,$52,$78,$EE,$F9,$07,$01,$FC ; .Ry.Rx.....

label_E33B:
  set1  ext, $03
  clr1  ext, $01
  mov   #$FF, sfr_0155
  mov   #$00, sfr_0148
  set1  sfr_0151, $05
  ret
label_E348:
  mov   #$80, $00
  ld    $7D
  and   #$01
  st    fpr
  ld    $7E
  st    trh
  ld    $7F
  st    trl
label_E359:
  ld    @R0
  stf
  st    b
  inc   $00
  ld    $00
  bz    label_E36F
  inc   trl
  ld    trl
  and   #$7F
  bnz   label_E359
  not1  trl, $07
  br    label_E359
label_E36F:
  bn    $7C, $00, label_E37A
label_E372:
  ldf
  xor   b
  bp    acc, $07, label_E372
  br    label_E387
label_E37A:
  ldf
  st    c
  not1  c, $06
label_E37F:
  not1  c, $06
  ldf
  xor   c
  bp    acc, $06, label_E37F
label_E387:
  ret
label_E388:
  mov   #$80, $00
  ld    $7D
  st    fpr
  ld    $7E
  st    trh
  ld    $7F
  st    trl
label_E397:
  ldf
  xor   @R0
  bnz   label_E3AB
  inc   trl
  ld    trl
  and   #$7F
  bnz   label_E3A5
  not1  trl, $07
label_E3A5:
  inc   $00
  ld    $00
  bnz   label_E397
label_E3AB:
  ret

  .byte $22,$00,$80,$23,$54,$00,$02,$7E,$13,$05,$02,$7F,$13,$04,$50,$14 ; "..#T..~......P.
  .byte $A0,$22,$00,$80,$23,$54,$01,$02,$7E,$13,$05,$02,$7F,$13,$04,$50 ; ."..#T..~......P
  .byte $14,$A0

label_E3CE:
  set1  fpr, $01
  call  label_E3D7
  clr1  fpr, $01
  call  label_E348
  ret
label_E3D7:
  mov   #$55, trh
  mov   #$55, trl
  mov   #$AA, acc
  stf
  mov   #$2A, trh
  mov   #$AA, trl
  mov   #$55, acc
  stf
  mov   #$55, trh
  mov   #$55, trl
  mov   #$A0, acc
  stf
  ret
label_E3F6:
  mov   #$80, $00
  ld    $7D
  st    fpr
  ld    $7E
  st    trh
  ld    $7F
  st    trl
label_E405:
  ldf
  st    @R0
  inc   $00
  ld    $00
  inc   trl
  bz    label_E411
  br    label_E405
label_E411:
  ret
label_E412:
  push  acc
  push  b
  push  c
  push  $00
  ld    $1E
  bnz   label_E420
  jmp   label_E495
label_E420:
  mov   #$00, $1E
  inc   $1D
  ld    $1D
  be    #$3C, label_E42C
  jmp   label_E497
label_E42C:
  mov   #$00, $1D
  inc   $1C
  ld    $1C
  be    #$3C, label_E438
  jmp   label_E497
label_E438:
  mov   #$00, $1C
  inc   $1B
  ld    $1B
  be    #$18, label_E444
  jmp   label_E497
label_E444:
  mov   #$00, $1B
  inc   $1A
  ld    $19
  be    #$0B, label_E461
  be    #$09, label_E461
  be    #$06, label_E461
  be    #$04, label_E461
  be    #$02, label_E468
  ld    $1A
  be    #$20, label_E47A
  jmp   label_E497
label_E461:
  ld    $1A
  be    #$1F, label_E47A
  jmp   label_E497
label_E468:
  ld    $1F
  bnz   label_E473
  ld    $1A
  be    #$1D, label_E47A
  jmp   label_E497
label_E473:
  ld    $1A
  be    #$1E, label_E47A
  jmp   label_E497
label_E47A:
  mov   #$01, $1A
  inc   $19
  ld    $19
  be    #$0D, label_E486
  jmp   label_E497
label_E486:
  mov   #$01, $19
  inc   $18
  ld    $18
  bnz   label_E491
  inc   $17
label_E491:
  call  label_E4A0
  jmp   label_E497
label_E495:
  set1  $1E, $00
label_E497:
  pop   $00
  pop   c
  pop   b
  pop   acc
  ret
label_E4A0:
  push  $00
  push  b
  push  c
  clr1  psw, cy
  ld    $18
  st    c
  mov   #$04, b
  ld    $17
  div
  st    $50
  ld    c
  st    $51
  ld    b
  bnz   label_E4DE
  set1  $1F, $00
  ld    $18
  st    c
  mov   #$64, b
  ld    $17
  div
  ld    b
  bnz   label_E4E1
  ld    $51
  st    c
  ld    $50
  mov   #$64, b
  div
  ld    b
  bnz   label_E4DE
  set1  $1F, $00
  jmp   label_E4E1
label_E4DE:
  mov   #$00, $1F
label_E4E1:
  pop   c
  pop   b
  pop   $00
  ret

  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$00,$00,$00,$00,$00,$00,$00,$00
  .byte $00,$00,$00,$00,$00,$00,$00,$00,$00,$20,$00,$00,$70,$D8,$88,$78 ; ......... ..p..x
  .byte $78,$78,$20,$F0,$98,$D8,$D8,$70,$00,$20,$50,$50,$20,$C0,$60,$60 ; xx ....p. PP .``
  .byte $10,$40,$00,$00,$00,$00,$00,$00,$70,$20,$70,$F8,$10,$F8,$30,$F8 ; .@......p p...0.
  .byte $70,$70,$00,$00,$10,$00,$40,$70,$70,$70,$F0,$70,$E0,$F8,$F8,$70 ; pp....@ppp.p...p
  .byte $88,$70,$38,$88,$80,$88,$88,$70,$F0,$70,$F0,$78,$F8,$88,$88,$88 ; .p8....p.p.x....
  .byte $88,$88,$F8,$70,$88,$70,$20,$00,$40,$00,$80,$00,$08,$00,$30,$00 ; ...p.p .@.....0.
  .byte $80,$20,$10,$80,$60,$00,$00,$00,$00,$00,$00,$00,$40,$00,$00,$00 ; . ..`.......@...
  .byte $00,$00,$00,$10,$20,$40,$00,$00,$20,$20,$80,$08,$00,$90,$20,$00 ; .... @..  .... .
  .byte $00,$00,$20,$00,$00,$E0,$38,$10,$40,$F8,$00,$20,$D8,$00,$30,$00 ; .. ...8.@.. ..0.
  .byte $00,$50,$20,$20,$70,$F8,$00,$70,$20,$00,$38,$00,$00,$00,$00,$00 ; .P  p..p .8.....
  .byte $00,$00,$00,$00,$00,$00,$00,$00,$00,$F8,$08,$20,$00,$10,$40,$20 ; ........... ..@ 
  .byte $00,$40,$00,$50,$00,$00,$40,$00,$00,$10,$00,$70,$40,$20,$00,$00 ; .@.P..@....p@ ..
  .byte $20,$10,$00,$80,$00,$00,$20,$00,$00,$00,$00,$00,$40,$00,$00,$70 ;  ..... .....@..p
  .byte $90,$00,$00,$00,$00,$00,$20,$E0,$00,$00,$00,$20,$88,$00,$00,$50 ; ...... .... ...P
  .byte $00,$00,$00,$00,$00,$00,$10,$00,$F8,$88,$88,$F8,$F8,$A8,$00,$00
  .byte $A8,$50,$00,$00,$00,$A8,$50,$F8,$00,$00,$00,$00,$00,$00,$00,$00 ; .P....P.........
  .byte $00,$00,$00,$00,$00,$00,$00,$00,$50,$A8,$00,$00,$D8,$D8,$D8,$F8 ; ........P.......
  .byte $F8,$F8,$70,$D8,$D8,$D8,$D8,$F8,$00,$20,$50,$50,$78,$C8,$90,$20 ; ..p...... PPx.. 
  .byte $20,$20,$20,$20,$00,$00,$00,$08,$88,$60,$88,$10,$30,$80,$40,$08 ;     .....`..0.@.
  .byte $88,$88,$60,$60,$20,$00,$20,$88,$88,$88,$88,$88,$90,$80,$80,$88 ; ..`` . .........
  .byte $88,$20,$10,$90,$80,$D8,$88,$88,$88,$88,$88,$80,$20,$88,$88,$88 ; . .......... ...
  .byte $88,$88,$08,$40,$50,$10,$50,$00,$20,$00,$80,$00,$08,$00,$48,$78 ; ...@P.P. .....Hx
  .byte $80,$00,$00,$80,$20,$00,$00,$00,$00,$00,$00,$00,$40,$00,$00,$00 ; .... .......@...
  .byte $00,$00,$00,$20,$20,$20,$20,$20,$70,$20,$C0,$18,$28,$D0,$70,$00 ; ...     p ..(.p.
  .byte $20,$20,$70,$20,$20,$C0,$18,$20,$20,$88,$38,$30,$48,$00,$20,$70 ;   p  ..  .80H. p
  .byte $70,$F8,$70,$70,$70,$88,$88,$88,$70,$00,$20,$00,$00,$00,$F8,$00 ; p.ppp...p. .....
  .byte $00,$00,$00,$00,$00,$00,$00,$00,$00,$08,$10,$F8,$F8,$F8,$F8,$F8
  .byte $78,$78,$F8,$F8,$C0,$F8,$F8,$88,$78,$E0,$A8,$00,$40,$20,$70,$F8 ; xx......x...@ p.
  .byte $F8,$10,$20,$80,$F8,$40,$F8,$F8,$70,$20,$08,$F8,$40,$70,$F8,$00 ; .. ..@..p ..@p..
  .byte $90,$20,$80,$F8,$F8,$C0,$90,$A0,$00,$80,$20,$50,$50,$88,$20,$50 ; . ........ PP. P
  .byte $00,$00,$F8,$00,$00,$00,$20,$70,$70,$F8,$88,$88,$F8,$50,$A8,$00 ; ...... pp....P..
  .byte $00,$A8,$70,$A8,$50,$50,$A8,$F8,$00,$00,$00,$00,$00,$00,$00,$00 ; ..p.PP..........
  .byte $00,$00,$00,$00,$00,$00,$00,$00,$A8,$A8,$70,$00,$D8,$D8,$F8,$C0 ; ..........p.....
  .byte $C0,$C0,$F8,$D8,$F8,$F0,$D8,$D8,$00,$20,$50,$F8,$A0,$10,$A0,$40 ; ......... P....@
  .byte $40,$10,$A8,$20,$00,$00,$00,$10,$98,$20,$08,$20,$50,$F0,$80,$10 ; @.. ..... . P...
  .byte $88,$88,$60,$60,$40,$F8,$10,$08,$08,$88,$88,$80,$88,$80,$80,$80 ; ..``@...........
  .byte $88,$20,$10,$A0,$80,$A8,$C8,$88,$88,$88,$88,$80,$20,$88,$88,$88 ; . .......... ...
  .byte $50,$88,$10,$40,$F8,$10,$88,$00,$10,$70,$B0,$70,$68,$70,$40,$88 ; P..@.....p.php@.
  .byte $B0,$60,$30,$90,$20,$D0,$B0,$70,$F0,$68,$B0,$70,$E0,$88,$88,$88 ; .`0. ..p.h.p....
  .byte $88,$88,$F8,$20,$20,$20,$10,$40,$A8,$20,$E0,$38,$48,$B0,$F8,$00 ; ...   .@. .8H...
  .byte $60,$30,$F8,$70,$50,$80,$08,$20,$20,$B8,$28,$28,$90,$00,$30,$F8 ; `0.pP..  .((..0.
  .byte $88,$F8,$F8,$F8,$F8,$88,$50,$70,$A8,$00,$20,$00,$00,$00,$08,$F8 ; ......Pp.. .....
  .byte $10,$20,$00,$10,$40,$00,$F0,$00,$00,$28,$20,$88,$20,$10,$48,$20 ; . ..@....( . .H 
  .byte $48,$90,$08,$50,$08,$08,$48,$88,$48,$20,$A8,$F8,$40,$F8,$00,$08 ; H..P..H.H ..@...
  .byte $10,$10,$10,$F8,$08,$A0,$20,$08,$00,$40,$08,$40,$F8,$10,$08,$F8 ; ...... ..@.@....
  .byte $90,$A0,$80,$88,$88,$00,$40,$E0,$40,$40,$50,$88,$20,$50,$70,$50 ; ......@.@@P. PpP
  .byte $00,$70,$88,$20,$F8,$50,$40,$F8,$00,$70,$F8,$88,$88,$A8,$50,$A8 ; .p. .P@..p....P.
  .byte $00,$00,$88,$50,$A8,$A8,$50,$F8,$00,$00,$00,$00,$00,$00,$00,$00 ; ...P..P.........
  .byte $00,$00,$00,$00,$00,$00,$00,$00,$88,$70,$70,$00,$D8,$D8,$F8,$70 ; .........pp....p
  .byte $F0,$F8,$D8,$F0,$F8,$E0,$F8,$D8,$00,$20,$00,$50,$70,$20,$40,$00 ; ......... .Pp @.
  .byte $40,$10,$70,$F8,$00,$F8,$00,$20,$A8,$20,$10,$10,$90,$08,$F0,$20 ; @.p.... . ..... 
  .byte $70,$78,$00,$00,$80,$00,$08,$10,$68,$88,$F0,$80,$88,$F0,$F0,$B8 ; px......h.......
  .byte $F8,$20,$10,$C0,$80,$A8,$A8,$88,$F0,$88,$F0,$70,$20,$88,$88,$A8 ; . .........p ...
  .byte $20,$50,$20,$40,$20,$10,$00,$00,$00,$08,$C8,$80,$98,$88,$E0,$88 ;  P @ ...........
  .byte $C8,$20,$10,$A0,$20,$A8,$C8,$88,$88,$98,$C8,$80,$40,$88,$88,$88 ; . .. .......@...
  .byte $50,$88,$10,$40,$20,$10,$F8,$F8,$20,$20,$F0,$78,$F8,$90,$00,$00 ; P..@ ...  .x....
  .byte $E0,$38,$00,$F8,$88,$80,$08,$20,$20,$A0,$28,$20,$00,$A8,$20,$F8 ; .8.....  .( .. .
  .byte $88,$F8,$F8,$F8,$F8,$88,$20,$20,$20,$00,$20,$20,$00,$60,$F8,$08 ; ......   .  .`..
  .byte $20,$F8,$70,$F8,$F8,$70,$10,$A8,$F8,$30,$60,$88,$20,$30,$48,$F8 ;  .p..p...0`. 0H.
  .byte $88,$10,$08,$50,$C8,$10,$50,$48,$A8,$F8,$A8,$20,$60,$20,$00,$50 ; ...P..PH... ` .P
  .byte $20,$10,$88,$80,$08,$10,$20,$08,$70,$80,$50,$F8,$48,$10,$F8,$08 ;  ..... .p.P.H...
  .byte $90,$A0,$88,$88,$88,$08,$00,$00,$A8,$20,$88,$20,$88,$20,$70,$20 ; ......... . . p 
  .byte $20,$50,$88,$50,$00,$20,$00,$E8,$00,$00,$70,$F8,$88,$00,$A8,$50 ;  P.P. ....p....P
  .byte $A8,$00,$A8,$A8,$50,$50,$A8,$F8,$00,$00,$00,$00,$00,$00,$00,$00 ; ....PP..........
  .byte $00,$00,$00,$00,$00,$00,$00,$00,$88,$A8,$70,$70,$F8,$F8,$F8,$18 ; ..........pp....
  .byte $C0,$D8,$D8,$D8,$F8,$F0,$D8,$D8,$00,$00,$00,$F8,$28,$40,$A8,$00 ; ............(@..
  .byte $40,$10,$A8,$20,$60,$00,$00,$40,$C8,$20,$20,$08,$F8,$08,$88,$40 ; @.. `..@.  ....@
  .byte $88,$08,$60,$60,$40,$F8,$10,$20,$A8,$F8,$88,$80,$88,$80,$80,$88 ; ..``@.. ........
  .byte $88,$20,$10,$A0,$80,$88,$98,$88,$80,$A8,$A0,$08,$20,$88,$88,$A8 ; . .......... ...
  .byte $50,$20,$40,$40,$F8,$10,$00,$00,$00,$78,$88,$80,$88,$F8,$40,$78 ; P @@.....x....@x
  .byte $88,$20,$10,$C0,$20,$A8,$88,$88,$F0,$78,$80,$70,$40,$88,$88,$A8 ; . .. ....x.p@...
  .byte $20,$78,$20,$20,$20,$20,$10,$40,$20,$A8,$E0,$38,$40,$38,$00,$F8 ;  x    .@ ..8@8..
  .byte $60,$30,$F8,$70,$50,$80,$08,$20,$20,$A0,$E8,$20,$00,$00,$70,$F8 ; `0.pP..  .. ..p.
  .byte $88,$70,$20,$70,$20,$88,$50,$F8,$70,$E0,$00,$20,$80,$60,$08,$30 ; .p p .P.p.. .`.0
  .byte $60,$88,$20,$30,$48,$10,$F0,$A8,$00,$20,$A0,$08,$20,$50,$48,$20 ; `. 0H.... .. PH 
  .byte $08,$10,$08,$10,$08,$20,$40,$08,$18,$20,$08,$20,$50,$20,$00,$20 ; ..... @.. . P . 
  .byte $70,$10,$88,$80,$08,$08,$A8,$50,$00,$88,$20,$40,$50,$10,$08,$08 ; p......P.. @P...
  .byte $10,$A8,$90,$88,$08,$08,$00,$00,$10,$10,$20,$50,$50,$88,$70,$20 ; .......... PP.p 
  .byte $00,$70,$88,$20,$F8,$50,$00,$D8,$00,$00,$00,$70,$F8,$00,$00,$A8 ; .p. .P.....p....
  .byte $50,$A8,$88,$50,$A8,$A8,$50,$F8,$00,$00,$00,$00,$00,$00,$00,$00 ; P..P..P.........
  .byte $00,$00,$00,$00,$00,$00,$00,$00,$50,$70,$F8,$F8,$D8,$70,$D8,$F8 ; ........Pp...p..
  .byte $F8,$F8,$F8,$D8,$D8,$D8,$D8,$F8,$00,$00,$00,$50,$F0,$98,$90,$00 ; ...........P....
  .byte $20,$20,$20,$20,$20,$00,$60,$80,$88,$20,$40,$88,$10,$88,$88,$40 ;      .`.. @....@
  .byte $88,$10,$60,$20,$20,$00,$20,$00,$A8,$88,$88,$88,$90,$80,$80,$88 ; ..`  . .........
  .byte $88,$20,$90,$90,$80,$88,$88,$88,$80,$90,$90,$08,$20,$88,$50,$A8 ; . .......... .P.
  .byte $88,$20,$80,$40,$20,$10,$00,$00,$00,$88,$88,$88,$88,$80,$40,$08 ; . .@ .........@.
  .byte $88,$20,$90,$A0,$20,$88,$88,$88,$80,$08,$80,$08,$48,$98,$50,$A8 ; . .. .......H.P.
  .byte $50,$08,$40,$20,$20,$20,$20,$20,$20,$70,$C0,$18,$20,$28,$00,$70 ; P.@      p.. (.p
  .byte $20,$20,$70,$20,$20,$C0,$18,$20,$20,$E0,$88,$E0,$00,$00,$50,$70 ;   p  ..  .....Pp
  .byte $70,$20,$70,$20,$70,$88,$88,$20,$88,$A0,$00,$20,$40,$00,$10,$20 ; p p p.. ... @.. 
  .byte $A0,$08,$20,$50,$50,$10,$10,$08,$00,$20,$20,$10,$20,$90,$48,$20 ; .. PP....  . .H 
  .byte $10,$10,$08,$20,$10,$50,$40,$10,$10,$20,$10,$20,$40,$40,$00,$50 ; ... .P@.. . @@.P
  .byte $A8,$20,$88,$80,$10,$08,$A8,$20,$70,$F8,$50,$40,$40,$10,$08,$10 ; . ..... p.P@@...
  .byte $20,$A8,$A0,$88,$10,$10,$00,$00,$00,$08,$50,$88,$20,$50,$70,$20 ;  .........P. Pp 
  .byte $00,$00,$F8,$00,$20,$00,$00,$70,$70,$70,$70,$70,$70,$A8,$00,$00 ; .... ..pppppp...
  .byte $A8,$50,$70,$A8,$50,$50,$A8,$F8,$20,$20,$20,$20,$20,$20,$20,$20 ; .Pp.PP..        
  .byte $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$00,$00,$70,$20,$D8,$F0 ;           ..p ..
  .byte $78,$78,$D8,$F8,$D8,$D8,$D8,$70,$00,$20,$00,$50,$20,$18,$68,$00 ; xx.....p. .P .h.
  .byte $10,$40,$00,$00,$40,$00,$60,$00,$70,$70,$F8,$70,$10,$70,$70,$40 ; .@..@.`.pp.p.pp@
  .byte $70,$60,$00,$40,$10,$00,$40,$20,$70,$88,$F0,$70,$E0,$F8,$80,$78 ; p`.@..@ p..p...x
  .byte $88,$70,$60,$88,$F8,$88,$88,$70,$80,$68,$88,$F0,$20,$70,$20,$50 ; .p`....p.h.. p P
  .byte $88,$20,$F8,$70,$20,$70,$00,$F8,$00,$78,$F0,$70,$78,$70,$40,$70 ; . .p p...x.pxp@p
  .byte $88,$70,$60,$90,$70,$88,$88,$70,$80,$08,$80,$F0,$30,$68,$20,$50 ; .p`.p..p....0h P
  .byte $88,$70,$F8,$10,$20,$40,$00,$00,$20,$20,$80,$08,$00,$38,$00,$20 ; .p.. @..  ...8. 
  .byte $00,$00,$20,$00,$00,$E0,$38,$10,$40,$00,$F8,$E0,$00,$00,$70,$00 ; .. ...8.@.....p.
  .byte $00,$00,$00,$00,$00,$F8,$00,$20,$70,$E0,$00,$E0,$20,$00,$20,$40 ; ....... p... . @
  .byte $20,$30,$70,$90,$40,$F8,$F0,$30,$00,$40,$20,$20,$F8,$10,$90,$20 ;  0p.@..0.@  ... 
  .byte $60,$20,$F8,$40,$E0,$88,$38,$60,$60,$40,$20,$40,$40,$80,$F8,$80 ; ` .@..8``@ @@...
  .byte $20,$40,$88,$78,$60,$00,$20,$10,$08,$08,$80,$38,$40,$F8,$F8,$20 ;  @.x`. ....8@.. 
  .byte $40,$B0,$C0,$F8,$20,$E0,$00,$00,$00,$00,$88,$00,$00,$20,$00,$20 ; @... ........ . 
  .byte $00,$00,$00,$00,$20,$00,$00,$00,$F8,$F8,$F8,$F8,$F8,$50,$A8,$00 ; .... ........P..
  .byte $00,$A8,$00,$00,$00,$A8,$50,$F8,$00,$00,$00,$00,$00,$00,$00,$00 ; ......P.........

  .org $EFF8
  .byte $00,$00,$00,$00,$00,$00,$00,$00,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
  .byte $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF

  ;; Pad size of binary
  .cnop $00, $0200

  ;; PSW bits
p EQU $00
rambk0 EQU $01
ov EQU $02
irbk0 EQU $03
irbk1 EQU $04
ac EQU $06
cy EQU $07

  ;; Special Function Registers
acc EQU $0100
psw EQU $0101
b EQU $0102
c EQU $0103
trl EQU $0104
trh EQU $0105
sp EQU $0106
pcon EQU $0107
ie EQU $0108
ip EQU $0109
sfr_010a EQU $010A
sfr_010b EQU $010B
sfr_010c EQU $010C
ext EQU $010D
ocr EQU $010E
sfr_010f EQU $010F
t0con EQU $0110
t0prr EQU $0111
t0l EQU $0112
t0lr EQU $0113
t0h EQU $0114
t0hr EQU $0115
sfr_0116 EQU $0116
sfr_0117 EQU $0117
t1cnt EQU $0118
sfr_0119 EQU $0119
t1lc EQU $011A
t1lr EQU $011B
t1hc EQU $011C
t1hr EQU $011D
sfr_011e EQU $011E
sfr_011f EQU $011F
mcr EQU $0120
sfr_0121 EQU $0121
stad EQU $0122
cnr EQU $0123
tdr EQU $0124
xbnk EQU $0125
sfr_0126 EQU $0126
vccr EQU $0127
sfr_0128 EQU $0128
sfr_0129 EQU $0129
sfr_012a EQU $012A
sfr_012b EQU $012B
sfr_012c EQU $012C
sfr_012d EQU $012D
sfr_012e EQU $012E
sfr_012f EQU $012F
scon0 EQU $0130
sbuf0 EQU $0131
sbr EQU $0132
sfr_0133 EQU $0133
scon1 EQU $0134
sbuf1 EQU $0135
sfr_0136 EQU $0136
sfr_0137 EQU $0137
sfr_0138 EQU $0138
sfr_0139 EQU $0139
sfr_013a EQU $013A
sfr_013b EQU $013B
sfr_013c EQU $013C
sfr_013d EQU $013D
sfr_013e EQU $013E
sfr_013f EQU $013F
sfr_0140 EQU $0140
sfr_0141 EQU $0141
sfr_0142 EQU $0142
sfr_0143 EQU $0143
p1 EQU $0144
p1ddr EQU $0145
p1fcr EQU $0146
sfr_0147 EQU $0147
sfr_0148 EQU $0148
sfr_0149 EQU $0149
sfr_014a EQU $014A
sfr_014b EQU $014B
p3 EQU $014C
p3ddr EQU $014D
p3int EQU $014E
sfr_014f EQU $014F
sfr_0150 EQU $0150
sfr_0151 EQU $0151
sfr_0152 EQU $0152
sfr_0153 EQU $0153
fpr EQU $0154
sfr_0155 EQU $0155
sfr_0156 EQU $0156
sfr_0157 EQU $0157
sfr_0158 EQU $0158
sfr_0159 EQU $0159
sfr_015a EQU $015A
sfr_015b EQU $015B
p7 EQU $015C
i01cr EQU $015D
i23cr EQU $015E
isl EQU $015F
mplsw EQU $0160
mplsta EQU $0161
mplrst EQU $0162
vsel EQU $0163
vrmad1 EQU $0164
vrmad2 EQU $0165
vtrbf EQU $0166
vlreg EQU $0167
sfr_0168 EQU $0168
sfr_0169 EQU $0169
sfr_016a EQU $016A
sfr_016b EQU $016B
sfr_016c EQU $016C
sfr_016d EQU $016D
sfr_016e EQU $016E
sfr_016f EQU $016F
sfr_0170 EQU $0170
sfr_0171 EQU $0171
sfr_0172 EQU $0172
sfr_0173 EQU $0173
sfr_0174 EQU $0174
sfr_0175 EQU $0175
sfr_0176 EQU $0176
sfr_0177 EQU $0177
sfr_0178 EQU $0178
sfr_0179 EQU $0179
sfr_017a EQU $017A
sfr_017b EQU $017B
sfr_017c EQU $017C
sfr_017d EQU $017D
sfr_017e EQU $017E
btcr EQU $017F
xram EQU $0180
xram_0181 EQU $0181
xram_0182 EQU $0182
xram_0183 EQU $0183
xram_0184 EQU $0184
xram_0185 EQU $0185
xram_0186 EQU $0186
xram_0187 EQU $0187
xram_0188 EQU $0188
xram_0189 EQU $0189
xram_018a EQU $018A
xram_018b EQU $018B
xram_018c EQU $018C
xram_018d EQU $018D
xram_018e EQU $018E
xram_018f EQU $018F
xram_0190 EQU $0190
xram_0191 EQU $0191
xram_0192 EQU $0192
xram_0193 EQU $0193
xram_0194 EQU $0194
xram_0195 EQU $0195
xram_0196 EQU $0196
xram_0197 EQU $0197
xram_0198 EQU $0198
xram_0199 EQU $0199
xram_019a EQU $019A
xram_019b EQU $019B
xram_019c EQU $019C
xram_019d EQU $019D
xram_019e EQU $019E
xram_019f EQU $019F
xram_01a0 EQU $01A0
xram_01a1 EQU $01A1
xram_01a2 EQU $01A2
xram_01a3 EQU $01A3
xram_01a4 EQU $01A4
xram_01a5 EQU $01A5
xram_01a6 EQU $01A6
xram_01a7 EQU $01A7
xram_01a8 EQU $01A8
xram_01a9 EQU $01A9
xram_01aa EQU $01AA
xram_01ab EQU $01AB
xram_01ac EQU $01AC
xram_01ad EQU $01AD
xram_01ae EQU $01AE
xram_01af EQU $01AF
xram_01b0 EQU $01B0
xram_01b1 EQU $01B1
xram_01b2 EQU $01B2
xram_01b3 EQU $01B3
xram_01b4 EQU $01B4
xram_01b5 EQU $01B5
xram_01b6 EQU $01B6
xram_01b7 EQU $01B7
xram_01b8 EQU $01B8
xram_01b9 EQU $01B9
xram_01ba EQU $01BA
xram_01bb EQU $01BB
xram_01bc EQU $01BC
xram_01bd EQU $01BD
xram_01be EQU $01BE
xram_01bf EQU $01BF
xram_01c0 EQU $01C0
xram_01c1 EQU $01C1
xram_01c2 EQU $01C2
xram_01c3 EQU $01C3
xram_01c4 EQU $01C4
xram_01c5 EQU $01C5
xram_01c6 EQU $01C6
xram_01c7 EQU $01C7
xram_01c8 EQU $01C8
xram_01c9 EQU $01C9
xram_01ca EQU $01CA
xram_01cb EQU $01CB
xram_01cc EQU $01CC
xram_01cd EQU $01CD
xram_01ce EQU $01CE
xram_01cf EQU $01CF
xram_01d0 EQU $01D0
xram_01d1 EQU $01D1
xram_01d2 EQU $01D2
xram_01d3 EQU $01D3
xram_01d4 EQU $01D4
xram_01d5 EQU $01D5
xram_01d6 EQU $01D6
xram_01d7 EQU $01D7
xram_01d8 EQU $01D8
xram_01d9 EQU $01D9
xram_01da EQU $01DA
xram_01db EQU $01DB
xram_01dc EQU $01DC
xram_01dd EQU $01DD
xram_01de EQU $01DE
xram_01df EQU $01DF
xram_01e0 EQU $01E0
xram_01e1 EQU $01E1
xram_01e2 EQU $01E2
xram_01e3 EQU $01E3
xram_01e4 EQU $01E4
xram_01e5 EQU $01E5
xram_01e6 EQU $01E6
xram_01e7 EQU $01E7
xram_01e8 EQU $01E8
xram_01e9 EQU $01E9
xram_01ea EQU $01EA
xram_01eb EQU $01EB
xram_01ec EQU $01EC
xram_01ed EQU $01ED
xram_01ee EQU $01EE
xram_01ef EQU $01EF
xram_01f0 EQU $01F0
xram_01f1 EQU $01F1
xram_01f2 EQU $01F2
xram_01f3 EQU $01F3
xram_01f4 EQU $01F4
xram_01f5 EQU $01F5
xram_01f6 EQU $01F6
xram_01f7 EQU $01F7
xram_01f8 EQU $01F8
xram_01f9 EQU $01F9
xram_01fa EQU $01FA
xram_01fb EQU $01FB
sfr_01fc EQU $01FC
sfr_01fd EQU $01FD
sfr_01fe EQU $01FE
sfr_01ff EQU $01FF
