; EdgeSearchInstaller.nsi
; NSIS script to install Automatic Edge Browser Web Searcher
; Requires NSIS 3.x with nsProcess plugin (for ExecWait) if desired

!include "MUI2.nsh"
!define APPNAME "EdgeSearch"
!define COMPANY "EdgeSearch"
!define VERSION "1.0"
!define BIN_DIR "..\\..\\build\\bin"

;---------------------------------
; General
Name "${APPNAME} ${VERSION}"
OutFile "EdgeSearch_Setup.exe"
InstallDir "$PROGRAMFILES64\\${APPNAME}"
RequestExecutionLevel admin ; need admin for pip/playwright

;---------------------------------
; Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\\..\\LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

;---------------------------------
Section "MainSection" SEC01
    SetOutPath "$INSTDIR"

    ; Copy EXE, checker, and requirements
    File "${BIN_DIR}\\EdgeSearch.exe"
    File "${BIN_DIR}\\python_checker.py"
    File "..\\..\\requirements.txt"

    ; Run python checker
    DetailPrint "Checking Python version..."
    nsExec::ExecToStack '"$SYSDIR\\cmd.exe" /C python_checker.py'
    Pop $0  ; exit code
    IntCmp $0 0 +3
        MessageBox MB_ICONSTOP "Python 3.12+ is required. Please install it and re-run the installer." /SD IDOK
        Abort
    DetailPrint "Python OK"

    ; Create virtual environment and install dependencies
    DetailPrint "Creating virtual environment..."
    nsExec::ExecToLog '"$SYSDIR\\cmd.exe" /C python -m venv .venv'
    DetailPrint "Installing requirements..."
    nsExec::ExecToLog '"$SYSDIR\\cmd.exe" /C .venv\\Scripts\\pip install -r "$EXEDIR\\..\\requirements.txt"'
    nsExec::ExecToLog '"$SYSDIR\\cmd.exe" /C .venv\\Scripts\\playwright install'

    ; Create shortcuts
    CreateShortCut "$DESKTOP\\${APPNAME}.lnk" "$INSTDIR\\EdgeSearch.exe"

    ; Generate uninstaller
    WriteUninstaller "$INSTDIR\\Uninstall.exe"
    CreateShortCut "$DESKTOP\\Uninstall ${APPNAME}.lnk" "$INSTDIR\\Uninstall.exe"

    ; Add entry to Add/Remove Programs
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayName" "${APPNAME}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "UninstallString" "$INSTDIR\\Uninstall.exe"
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoModify" 1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoRepair" 1
SectionEnd

;---------------------------------
Section "Uninstall" SEC02
    ; Remove files & shortcuts
    Delete "$DESKTOP\\${APPNAME}.lnk"
    Delete "$DESKTOP\\Uninstall ${APPNAME}.lnk"
    Delete "$INSTDIR\\EdgeSearch.exe"
    Delete "$INSTDIR\\python_checker.py"
    Delete "$INSTDIR\\Uninstall.exe"
    RMDir /r "$INSTDIR"

    ; Remove registry entries
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"
SectionEnd
