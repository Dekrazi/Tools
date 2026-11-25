@echo off
setlocal
cd /d "%~dp0"
title Building PC Repair Toolkit

rem Uses the C# compiler that is already part of Windows (.NET Framework 4.x, C# 5). Nothing to install.
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    echo.
    echo  Could not find csc.exe in %WINDIR%\Microsoft.NET\Framework64\v4.0.30319
    echo  Enable ".NET Framework 4.8" in "Turn Windows features on or off" and try again.
    echo.
    pause
    exit /b 1
)

rem A running copy locks the exe and makes the build fail.
taskkill /f /im PCRepairToolkit.exe >nul 2>&1

echo Compiling with: %CSC%
echo.
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /warn:3 /codepage:65001 ^
    /out:PCRepairToolkit.exe /win32manifest:app.manifest /win32icon:app.ico ^
    /resource:Catalog.xml,Catalog.xml ^
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll /r:System.Xml.Linq.dll ^
    Program.cs Model.cs Loc.cs Runner.cs Workspace.cs Navigation.cs

if errorlevel 1 (
    echo.
    echo  ==== BUILD FAILED ====
    echo  Copy the error lines above and send them to Claude - it will fix the code.
    echo.
    pause
    exit /b 1
)

echo.
echo  Built: %CD%\PCRepairToolkit.exe
echo  Double-click it. Commands that need administrator rights ask for UAC,
echo  or use "Restart as administrator" at the bottom right of the window.
echo  The exe is standalone: copy only PCRepairToolkit.exe to other PCs (no other files needed).
echo.
pause
