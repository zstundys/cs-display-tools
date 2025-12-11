@echo off
REM Publish script for DisplayRefreshRate (Release, single-file, self-contained)
REM Requires .NET 8 SDK to be installed

REM Check if dotnet SDK is available
dotnet --list-sdks >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found.
    echo.
    echo Please install the .NET 8 SDK from:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    echo Download the SDK installer, not just the runtime.
    pause
    exit /b 1
)

echo Publishing DisplayRefreshRate (Release, single-file)...
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Publish successful!
    echo Output: publish\DisplayRefreshRate.exe
    echo.
    echo This is a self-contained executable - no .NET runtime required on target machine.
) else (
    echo.
    echo Publish failed with error code %ERRORLEVEL%
)

pause

