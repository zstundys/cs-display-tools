@echo off
REM Publish script for DisplayRefreshRate (Release, single-file, framework-dependent)
REM Output is smaller but requires .NET 8 Runtime on target machine

REM Check if dotnet SDK is available
dotnet --list-sdks >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found.
    echo.
    echo Please install the .NET 8 SDK from:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo Publishing DisplayRefreshRate (Release, single-file, framework-dependent)...
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish-small

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Publish successful!
    echo Output: publish-small\DisplayRefreshRate.exe
    echo.
    echo NOTE: Target machine requires .NET 8 Runtime:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
) else (
    echo.
    echo Publish failed with error code %ERRORLEVEL%
)

pause

