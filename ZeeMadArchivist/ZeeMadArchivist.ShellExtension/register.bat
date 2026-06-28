@echo off
setlocal

set "DLL=ZeeMadArchivist.ShellExtension.dll"
set "REGASM=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\regasm.exe"

if not exist "%DLL%" (
    echo ERROR: %DLL% not found in the current directory.
    echo Build the project first, then copy the DLL or run this script from the output folder.
    exit /b 1
)

echo Registering %DLL% using %REGASM%...
"%REGASM%" "%DLL%" /codebase
if errorlevel 1 (
    echo Registration failed.
    exit /b 1
)

echo Registration succeeded.
echo Restart Explorer or open a new Properties dialog to see the Tags tab.
exit /b 0
