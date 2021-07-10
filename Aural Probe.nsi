; AuralProbe.nsi installer script
; Authored and tested with NSIS installer v3.0a1
;--------------------------------

!include LogicLib.nsh

; The name of the installer
Name "Aural Probe"

Icon install.ico
UninstallIcon uninstall.ico


; The file to write
OutFile "installer\AuralProbeInstaller.exe"

; The default installation directory
InstallDir "$PROGRAMFILES\Aural Probe"

; Registry key to check for directory (so if you install again, it will 
; overwrite the old one automatically)
InstallDirRegKey HKLM "Software\Aural Probe" "Install_Dir"

LicenseData "license.txt"

; Make sure we're installing as an administrator
RequestExecutionLevel admin

;--------------------------------

; Pages

Page license
Page components
Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

;--------------------------------

; Check for .NET Framework 2.0 or greater
Function .onInit
 
  !define MIN_FRA_MAJOR "2"
  !define MIN_FRA_MINOR "0"
  !define MIN_FRA_BUILD "*"
  
  ;Save the variables in case something else is using them
  Push $0
  Push $1
  Push $2
  Push $3
  Push $4
  Push $R1
  Push $R2
  Push $R3
  Push $R4
  Push $R5
  Push $R6
  Push $R7
  Push $R8
 
  StrCpy $R5 "0"
  StrCpy $R6 "0"
  StrCpy $R7 "0"
  StrCpy $R8 "0.0.0"
  StrCpy $0 0
 
  loop:
 
  ;Get each sub key under "SOFTWARE\Microsoft\NET Framework Setup\NDP"
  EnumRegKey $1 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP" $0
  StrCmp $1 "" done ;jump to end if no more registry keys
  IntOp $0 $0 + 1
  StrCpy $2 $1 1 ;Cut off the first character
  StrCpy $3 $1 "" 1 ;Remainder of string
 
  ;Loop if first character is not a 'v'
  StrCmpS $2 "v" start_parse loop
 
  ;Parse the string
  start_parse:
  StrCpy $R1 ""
  StrCpy $R2 ""
  StrCpy $R3 ""
  StrCpy $R4 $3
 
  StrCpy $4 1
 
  parse:
  StrCmp $3 "" parse_done ;If string is empty, we are finished
  StrCpy $2 $3 1 ;Cut off the first character
  StrCpy $3 $3 "" 1 ;Remainder of string
  StrCmp $2 "." is_dot not_dot ;Move to next part if it's a dot
 
  is_dot:
  IntOp $4 $4 + 1 ; Move to the next section
  goto parse ;Carry on parsing
 
  not_dot:
  IntCmp $4 1 major_ver
  IntCmp $4 2 minor_ver
  IntCmp $4 3 build_ver
  IntCmp $4 4 parse_done
 
  major_ver:
  StrCpy $R1 $R1$2
  goto parse ;Carry on parsing
 
  minor_ver:
  StrCpy $R2 $R2$2
  goto parse ;Carry on parsing
 
  build_ver:
  StrCpy $R3 $R3$2
  goto parse ;Carry on parsing
 
  parse_done:
 
  IntCmp $R1 $R5 this_major_same loop this_major_more
  this_major_more:
  StrCpy $R5 $R1
  StrCpy $R6 $R2
  StrCpy $R7 $R3
  StrCpy $R8 $R4
 
  goto loop
 
  this_major_same:
  IntCmp $R2 $R6 this_minor_same loop this_minor_more
  this_minor_more:
  StrCpy $R6 $R2
  StrCpy $R7 R3
  StrCpy $R8 $R4
  goto loop
 
  this_minor_same:
  IntCmp R3 $R7 loop loop this_build_more
  this_build_more:
  StrCpy $R7 $R3
  StrCpy $R8 $R4
  goto loop
 
  done:
 
  ;Have we got the framework we need?
  IntCmp $R5 ${MIN_FRA_MAJOR} max_major_same fail end
  max_major_same:
  IntCmp $R6 ${MIN_FRA_MINOR} max_minor_same fail end
  max_minor_same:
  IntCmp $R7 ${MIN_FRA_BUILD} end fail end
 
  fail:
  StrCmp $R8 "0.0.0" no_framework
  goto wrong_framework
 
  no_framework:
  MessageBox MB_OK|MB_ICONSTOP "Installation failed.$\n$\n\
         This software requires Microsoft .NET Framework version \
         ${MIN_FRA_MAJOR}.${MIN_FRA_MINOR}.${MIN_FRA_BUILD} or higher.$\n$\n\
         No version of Windows Framework is installed.$\n$\n\
         Please update your computer at http://windowsupdate.microsoft.com/."
  abort
 
  wrong_framework:
  MessageBox MB_OK|MB_ICONSTOP "Installation failed!$\n$\n\
         This software requires Microsoft .NET Framework version \
         ${MIN_FRA_MAJOR}.${MIN_FRA_MINOR}.${MIN_FRA_BUILD} or higher.$\n$\n\
         The highest version on this computer is $R8.$\n$\n\
         Please update your computer at http://windowsupdate.microsoft.com/."
  abort
 
  end:
 
  ;Pop the variables we pushed earlier
  Pop $R8
  Pop $R7
  Pop $R6
  Pop $R5
  Pop $R4
  Pop $R3
  Pop $R2
  Pop $R1
  Pop $4
  Pop $3
  Pop $2
  Pop $1
  Pop $0
 
FunctionEnd

; The stuff to install
Section "Aural Probe (required)"

  SectionIn RO
  
  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  
  ; Put file there
  File "Aural Probe.exe"
  File "fmodex.dll"
  File "favoritesfile.ico"
  File "Default.cfg"
  File "Aural Probe documentation.chm"
  File "Aural Probe Logo.png"
  File "Release Notes.txt"
  
  ; Write the installation path into the registry
  WriteRegStr HKLM "SOFTWARE\Aural Probe" "Install_Dir" '"$INSTDIR"'

  ; Associate the APF file type with Aural Probe
  ReadRegStr $R0 HKCR ".apf" ""
  StrCmp $R0 "APFFile" 0 +2
    DeleteRegKey HKCR "APFFile"

  WriteRegStr HKCR ".apf" "" "AuralProbe.Favorites"
  WriteRegStr HKCR "AuralProbe.Favorites" "" "Aural Probe Favorites"
  WriteRegStr HKCR "AuralProbe.Favorites\DefaultIcon" "" '"$INSTDIR\favoritesfile.ico"'
  ReadRegStr $R0 HKCR "AuralProbe.Favorites\shell\open\command" ""
  StrCmp $R0 "" 0 no_apfopen
    WriteRegStr HKCR "AuralProbe.Favorites\shell" "" "open"
    WriteRegStr HKCR "AuralProbe.Favorites\shell\open\command" "" '"$INSTDIR\Aural Probe.exe" "%1" "$INSTDIR"'
  no_apfopen:
  System::Call 'Shell32::SHChangeNotify(i 0x80000000, i 0, i 0, i 0)' ; refresh the desktop
  
  ; Write the uninstall keys for Windows
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Aural Probe" "DisplayName" "Aural Probe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Aural Probe" "DisplayIcon" '"$INSTDIR\Aural Probe.exe"'
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Aural Probe" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Aural Probe" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Aural Probe" "NoRepair" 1
  WriteUninstaller "uninstall.exe"
  
SectionEnd

; Optional section (can be disabled by the user)
Section "Source Code"
	
	CreateDirectory "$INSTDIR\Source Code"
	SetOutPath "$INSTDIR\Source Code"
	
  ; Put source files there
  File "*.ico"
  File "*.cs"
  File "*.resx"
  File "*.png"
  File "*.hhp"
  File "*.hhc"
  File "*.htm"
  File "*.css"
  File "Default.cfg"
  File "*.sln"
  File "*.csproj"
  File "*.nsi"
  File "license.txt"
  File "readme.txt"
  File "Release Notes.txt"

	CreateDirectory "$SMPROGRAMS\Aural Probe"
	CreateShortCut "$SMPROGRAMS\Aural Probe\Aural Probe VS Solution.lnk" "$INSTDIR\Source Code\Aural Probe.sln" "" "$INSTDIR\Source Code\Aural Probe.sln" 0
  
SectionEnd

; Optional section (can be disabled by the user)
Section "Start Menu Shortcuts"

    SetOutPath "$INSTDIR" ; Reset out path - this is used for the working folder of shortcuts
	
  CreateDirectory "$SMPROGRAMS\Aural Probe"
  CreateShortCut "$SMPROGRAMS\Aural Probe\Aural Probe.lnk" "$INSTDIR\Aural Probe.exe" "" "$INSTDIR\Aural Probe.exe" 0
  CreateShortCut "$SMPROGRAMS\Aural Probe\Aural Probe documentation.lnk" "$INSTDIR\Aural Probe documentation.chm" "" "$INSTDIR\Aural Probe documentation.chm" 0
  CreateShortCut "$SMPROGRAMS\Aural Probe\Uninstall Aural Probe.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0

  Delete "$SMPROGRAMS\Aural Probe\Aural Probe Help.lnk" ; Try and delete this from older versions on install
  
SectionEnd

; Optional section (can be disabled by the user)
Section "Desktop Shortcut"

    SetOutPath "$INSTDIR" ; Reset out path - this is used for the working folder of shortcuts

  CreateShortCut "$DESKTOP\Aural Probe.lnk" "$INSTDIR\Aural Probe.exe" "" "$INSTDIR\Aural Probe.exe" 0

SectionEnd

;--------------------------------

; Uninstaller

Section "Uninstall"
  
  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Aural Probe"
  DeleteRegKey HKLM "SOFTWARE\Aural Probe"

  ReadRegStr $R0 HKCR ".apf" ""
  StrCmp $R0 "AuralProbe.Favorites" 0 +2
    DeleteRegKey HKCR ".apf"

  DeleteRegKey HKCR "AuralProbe.Favorites"

	System::Call 'Shell32::SHChangeNotify(i 0x80000000, i 0, i 0, i 0)' ; refresh the desktop

  ; Remove files and uninstaller
  Delete "$INSTDIR\Release Notes.txt"
  Delete "$INSTDIR\Aural Probe Help.html" ; try and delete this from older versions on uninstall
  Delete "$INSTDIR\Aural Probe documentation.chm"
  Delete "$INSTDIR\Aural Probe Logo.png"
  Delete "$INSTDIR\Aural Probe.exe"
  Delete $INSTDIR\Default.cfg
  Delete $INSTDIR\fmodex.dll
  Delete $INSTDIR\favoritesfile.ico
  Delete $INSTDIR\uninstall.exe

  Delete "$INSTDIR\Source Code\*.*"
  
  ; Remove shortcuts, if any
  Delete "$SMPROGRAMS\Aural Probe\*.*"

  ; Remove shortcuts, if any
  Delete "$DESKTOP\Aural Probe.lnk"

  ; Remove directories used
  RMDir "$SMPROGRAMS\Aural Probe"
  RMDir "$INSTDIR\Source Code"
  RMDir "$INSTDIR"

SectionEnd

; Show release notes
 Function .onInstSuccess
    MessageBox MB_YESNO "View release notes?" IDNO NoReadme
      Exec "notepad.exe $INSTDIR\Release Notes.txt"
    NoReadme:
  FunctionEnd
