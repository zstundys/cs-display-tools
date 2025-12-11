@echo off
REM Build script for DisplayRefreshRate (Debug)
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

echo Building DisplayRefreshRate (Debug)...
dotnet restore
dotnet build -c Debug

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful!
    echo Output: bin\Debug\net8.0-windows\DisplayRefreshRate.exe
) else (
    echo.
    echo Build failed with error code %ERRORLEVEL%
)

pause

