@echo off
setlocal

set "DLL=ZeeMadArchivist.ShellExtension.dll"
set "REGASM=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\regasm.exe"

if not exist "%DLL%" (
    echo ERROR: %DLL% not found in the current directory.
    exit /b 1
)

echo Unregistering %DLL% using %REGASM%...
"%REGASM%" "%DLL%" /unregister
if errorlevel 1 (
    echo Unregistration failed.
    exit /b 1
)

echo Unregistration succeeded.
exit /b 0
