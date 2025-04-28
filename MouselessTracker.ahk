; AutoHotkey Version: 1.x
; Language:       English
; Platform:       Win9x/NT
; Author:         A.N.Other <myemail@nowhere.com>
;
; Script Function:
;	Template script (you can customize this template by editing "ShellNew\Template.ahk" in your Windows folder)
;

#NoEnv  ; Recommended for performance and compatibility with future AutoHotkey releases.
SendMode Input  ; Recommended for new scripts due to its superior speed and reliability.
#SingleInstance force
#Persistent ;Script nicht beenden nach der Auto-Execution-Section
#InstallMouseHook
CoordMode, Mouse, Screen

SetWorkingDir %A_ScriptDir%
SetTitleMatchMode, 2


Menu, tray, NoStandard
Menu, tray, add  ; Creates a separator line.
Menu, tray, add, Reload  
Menu, tray, add, Exit

specialWinWasActive := 0
start := A_TickCount
MouseGetPos , lastX, lastY

GoSub, SetupGui
SetTimer, CheckMouseMovement, 100

return

SetupGui:	
	targetX := A_ScreenWidth - 300
	Gui, main:Font, s12 cFFFFFF
	;Gui, main:add,text, h50 w250 x5 y5, Time mouse not moved	
	Gui, main:add,text, h20 w100 x5 y0 vgMouseNotMovedTime, 00:00:00
	Gui, main:Color, 222222	
	Gui, main:+AlwaysOnTop -Caption -Border
	Gui, main:Margin, 0, 0
	Gui, main:show, x%targetX% y0 w72 h21, Mouseless tracker
return

CheckMouseMovement:

	; is ctrl down
	GetKeyState, state, Ctrl
	if (state = "D")
	{
		specialWinWasActive := 1
		return
	}

	; up down left right are used in several programs to move the mosue
	GetKeyState, state, UP, P
	if (state = "D")
	{		
		specialWinWasActive := 1
		return
	}

	GetKeyState, state, DOWN, P
	if (state = "D")
	{
		specialWinWasActive := 1
		return
	}

	GetKeyState, state, LEFT, P
	if (state = "D")
	{
		specialWinWasActive := 1
		return
	}	

	GetKeyState, state, RIGHT, P
	if (state = "D")
	{
		specialWinWasActive := 1
		return
	}	

	; iSwitch
	; h j kl are used for neat mouse
	GetKeyState, state, h, P
	if (state = "D")
	{		
		specialWinWasActive := 1
		return
	}

	GetKeyState, state, j, P
	if (state = "D")
	{
		specialWinWasActive := 1
		return
	}

	GetKeyState, state, k, P
	if (state = "D")
	{
		specialWinWasActive := 1
		return
	}	

	GetKeyState, state, l, P
	if (state = "D")
	{
		specialWinWasActive := 1
		return
	}	

	; iSwitch
	IfWinActive, fast keyboard window switcher
	{	
		specialWinWasActive := 1
		return
	}

	IfWinActive, KeyboardMouser
	{	
		specialWinWasActive := 1
	}

	IfWinActive, Fluent Search - Screen Search
	{	
		specialWinWasActive := 1
	}
	/*
	IfWinActive, Quicksearch
	{	
		specialWinWasActive := 1
	}

	IfWinActive, ahk_class Qt5QWindowIcon
	{	
		specialWinWasActive := 1
	}	
	*/

	if(specialWinWasActive = 1) {
		specialWinWasActive := 0
		SetTimer, CheckMouseMovement, Off
		Sleep, 5000
		SetTimer, CheckMouseMovement, 100
		MouseGetPos , currentX, currentY
		lastX := currentX
		lastY := currentY
		return
	}
		
	diff := A_TickCount - start
	MouseGetPos , currentX, currentY

	if (Abs(currentX - lastX) > 20 or Abs(currentY - lastY) > 20) {
		if(diff > 5000) {			
			;MsgBox, %currentX% %lastX%

			Loop 6
			{				
				Gui, main:Color, 222222	
				Sleep 100 				
				Gui, main:Color, FF0000
				Sleep 100 
			}	
			Gui, main:Color, 222222	
			SetTimer, WinMoveMsgBox, 10
			start := A_TickCount	
			/*
			MsgBox, 4, Mouse moved,Mousemovement detected! Was it correct? 
			IfMsgBox Yes
				start := A_TickCount	
			IfMsgBox No
				MouseGetPos , currentX, currentY
				lastX := currentX
				lastY := currentY				
			*/
		}
		
	}
	;If (A_TimeIdleMouse < A_TickCount - start) {				
		
	;}
	
	mouseNotMovedTime := MillisecToTime(diff)
	GuiControl,main:,gMouseNotMovedTime, %mouseNotMovedTime%

	lastX := currentX
	lastY := currentY
return

WinMoveMsgBox:
	SetTimer, WinMoveMsgBox, OFF
	ID := WinExist("Mouse moved")
	WinGetPos, X, Y, Width, Height, A
	
	MouseGetPos, currentX, currentY
	windowX := currentX - (Width / 2)	
	;windowY := currentY - (Height / 2)
	WinMove, ahk_id %ID%, , %windowX%, %Y%
Return

MillisecToTime(msec) {
	secs := Floor(Mod(msec / 1000, 60))
	mins := Floor(Mod(msec / (1000 * 60), 60))
	hour := Floor(Mod(msec / (1000 * 60 * 60), 24))	
	Return, Format("{:02}:{:02}:{:02}", hour, mins, secs)
}

Reload:
	Reload
return 

Exit:
	ExitApp
return

if(!A_IsCompiled) {
	#y::
		;ControlGetText, output , SysListView321, 
		;ControlGet, output, Line, 1, SysListView321, - Notepad++
		Send ^s
		reload
	return
}