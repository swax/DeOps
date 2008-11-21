
XPStyle on


; The name of the installer
Name "RiseOp"

; The file to write
OutFile "RiseOpInstall_1.1.1.exe"

; The default installation directory
InstallDir $PROGRAMFILES\RiseOp

; Registry key to check for directory (so if you install again, it will 
; overwrite the old one automatically)
InstallDirRegKey HKLM "Software\RiseOp" "Install_Dir"

; Request application privileges for Windows Vista
RequestExecutionLevel admin


;--------------------------------

; Pages

Page components
Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

;--------------------------------

; The stuff to install
Section "RiseOp"

  SectionIn RO
  
  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  

  ; Put file there
  File "..\bin\Protected\RiseOp.exe"
  File "..\Update\bin\Release\UpdateOp.exe"
  File "..\bin\Protected\bootstrap.dat"
  File "..\bin\Debug\libspeex.dll"

  ; Write the installation path into the registry
  WriteRegStr HKLM SOFTWARE\RiseOp "Install_Dir" "$INSTDIR"
  

  ; Write the uninstall keys for Windows
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\RiseOp" "DisplayName" "RiseOp"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\RiseOp" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\RiseOp" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\RiseOp" "NoRepair" 1
  WriteUninstaller "uninstall.exe"
  

  ; start menu entries
  CreateDirectory "$SMPROGRAMS\RiseOp"
  CreateShortCut "$SMPROGRAMS\RiseOp\Uninstall.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0
  CreateShortCut "$SMPROGRAMS\RiseOp\RiseOp.lnk" "$INSTDIR\RiseOp.exe" "" "$INSTDIR\RiseOp.exe" 0


  ; register .rop with riseop
  WriteRegStr HKCR ".rop" "" "rop"
  WriteRegStr HKCR "rop"  "" "RiseOp Identity"
  WriteRegStr HKCR "rop\DefaultIcon" "" "$\"$INSTDIR\RiseOp.exe$\""
  WriteRegStr HKCR "rop\shell\open\command" "" "$\"$INSTDIR\RiseOp.exe$\" $\"%1$\""


  ; register riseop:// with riseop
  WriteRegStr HKCR "riseop" "" "URL:riseop Protocol"
  WriteRegStr HKCR "riseop" "URL Protocol" ""
  WriteRegStr HKCR "riseop\DefaultIcon" "" "$\"$INSTDIR\RiseOp.exe$\""
  WriteRegStr HKCR "riseop\shell\open\command" "" "$\"$INSTDIR\RiseOp.exe$\" $\"%1$\""
            

  DetailPrint "Optimizing..."
  nsExec::Exec '"$WINDIR\Microsoft.NET\Framework\v2.0.50727\ngen.exe" install "$INSTDIR\RiseOp.exe" /queue'


SectionEnd

; Optional section (can be disabled by the user)
Section "Desktop Shortcut"

	CreateShortCut "$DESKTOP\RiseOp.lnk" "$INSTDIR\RiseOp.exe" "" "$INSTDIR\RiseOp.exe" 0
  
SectionEnd

;--------------------------------

; Uninstaller

Section "Uninstall"
  
  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\RiseOp"
  DeleteRegKey HKLM SOFTWARE\RiseOp

  nsExec::Exec '"$WINDIR\Microsoft.NET\Framework\v2.0.50727\ngen.exe" uninstall "$INSTDIR\RiseOp.exe"'

  ; Remove files and uninstaller
  Delete $INSTDIR\*.exe

  ; Remove shortcuts, if any
  Delete $DESKTOP\RiseOp.lnk
  Delete "$SMPROGRAMS\RiseOp\*.*"

  ; Remove directories used
  RMDir "$SMPROGRAMS\RiseOp"
  RMDir "$INSTDIR"

SectionEnd
