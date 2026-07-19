Simple modbus master simulator with controls. Based on https://apollo3zehn.github.io/FluentModbus/.


How to use:

Create file Config.txt in .exe folder and add elements:

WINDOW,NAME,POSX,POSY,WIDTH,HEIGTH -sets window position and size, should be used once

DEVICE,NAME,IP:PORT -add slave device, multiple can be used

DATASET,NAME,DEVICE,ADDRESS,LENGTH -create datasets for devices (modbus function 3 - read holding registers), multiple datasets for the same device will be concated to single array

LABEL,NAME,POSX,POSY,FONT,FSIZE,TEXT -creates label

LABEL,NAME,POSX,POSY,FONT,FSIZE,TEXT,DEVICE,ADDRESS,EXPRESSION(X) -creates label with expression of variable linked to dataset array of device, simple math can be used + - * /.

BUTTON,NAME,POSX,POSY,WIDTH,HEIGTH,TEXT,DEVICE,ADDRESS,VALUE -creates button to write register (modbus function 6 - write single register)

LAMP,NAME,POSX,POSY,WIDTH,HEIGTH,DEVICE,ADDRESS,BIT -creates red/green bit lamp linked to dataset array of device



ESC KEY CLOSES APP

VARIABLES ARE 16BIT LONG SIGNED
