@echo off
cd /d "%~dp0"
echo [ScreenCrosshair] Building project...

set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if not exist "bin" mkdir bin

"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu /win32icon:"assets\app.ico" /lib:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF" /r:System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Runtime.Serialization.dll,System.Xaml.dll,WindowsBase.dll,PresentationCore.dll,PresentationFramework.dll,Microsoft.VisualBasic.dll /out:"bin\ScreenCrosshair.exe" src\Models\CrosshairLayer.cs src\Models\AppConfig.cs src\Utils\Win32Helper.cs src\Utils\HotKeyManager.cs src\Views\CrosshairRenderer.cs src\Views\OverlayWindow.cs src\Views\MainWindow.cs src\Views\StartupWindow.cs src\Program.cs

if %ERRORLEVEL% equ 0 (
    copy /y "bin\ScreenCrosshair.exe" "bin\CrosshairMaster.exe" >nul
    echo [ScreenCrosshair] Build succeeded: bin\ScreenCrosshair.exe
) else (
    echo [ScreenCrosshair] Build failed.
)
if "%1"=="" pause
