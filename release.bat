@echo off
REM DisplayRefreshRate Release Script
REM Usage: release.bat <version> [--draft]
REM Example: release.bat 1.0.0
REM Example: release.bat 1.0.0 --draft

if "%1"=="" (
    echo Usage: release.bat ^<version^> [--draft]
    echo Example: release.bat 1.0.0
    echo Example: release.bat 1.0.0 --draft
    exit /b 1
)

set VERSION=%1
set DRAFT_FLAG=

if "%2"=="--draft" (
    set DRAFT_FLAG=-Draft
)

powershell -ExecutionPolicy Bypass -File "%~dp0release.ps1" -Version %VERSION% %DRAFT_FLAG%

