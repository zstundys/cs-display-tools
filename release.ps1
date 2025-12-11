# DisplayRefreshRate Release Script
# Creates a GitHub release with the built executable
# Requires: GitHub CLI (gh) - https://cli.github.com/

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [switch]$Draft = $false,
    [switch]$SkipBuild = $false
)

$ErrorActionPreference = "Stop"

$ProjectDir = $PSScriptRoot
$ProjectName = "DisplayRefreshRate"
$PublishDir = Join-Path $ProjectDir "publish-release"
$ZipName = "$ProjectName-v$Version.zip"
$ZipPath = Join-Path $ProjectDir $ZipName

Write-Host "=== $ProjectName Release Script ===" -ForegroundColor Cyan
Write-Host "Version: v$Version"
Write-Host ""

# Check for GitHub CLI
if (-not (Get-Command "gh" -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: GitHub CLI (gh) is not installed." -ForegroundColor Red
    Write-Host "Install from: https://cli.github.com/"
    exit 1
}

# Check if logged in to GitHub
$ErrorActionPreference = "Continue"
gh auth status 2>$null | Out-Null
$authResult = $LASTEXITCODE
$ErrorActionPreference = "Stop"

if ($authResult -ne 0) {
    Write-Host "ERROR: Not logged in to GitHub CLI." -ForegroundColor Red
    Write-Host "Run: gh auth login"
    exit 1
}

# Build
if (-not $SkipBuild) {
    Write-Host "Building release..." -ForegroundColor Yellow
    
    Push-Location $ProjectDir
    
    # Clean previous publish
    if (Test-Path $PublishDir) {
        Remove-Item $PublishDir -Recurse -Force
    }
    
    # Build single-file executable
    dotnet publish -c Release --self-contained false -p:PublishSingleFile=true -o $PublishDir
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Build failed!" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    
    Pop-Location
    Write-Host "Build successful!" -ForegroundColor Green
}

# Create ZIP
Write-Host "Creating release archive..." -ForegroundColor Yellow

if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

# Get the exe and create zip
$ExePath = Join-Path $PublishDir "$ProjectName.exe"
if (-not (Test-Path $ExePath)) {
    Write-Host "ERROR: Executable not found at $ExePath" -ForegroundColor Red
    exit 1
}

Compress-Archive -Path $ExePath -DestinationPath $ZipPath -Force
Write-Host "Created: $ZipName" -ForegroundColor Green

# Create GitHub Release
Write-Host ""
Write-Host "Creating GitHub release..." -ForegroundColor Yellow

$ReleaseNotes = @"
## DisplayRefreshRate v$Version

### Features
- Auto max refresh rate on startup
- Display toggle (Ctrl+Alt+F11)
- Audio device switching
- System tray with Hz display
- Windows 11 Fluent UI

### Installation
1. Download ``$ZipName``
2. Extract and run ``$ProjectName.exe``
3. (Optional) Enable "Run on Startup" in settings

### Requirements
- Windows 10/11
- .NET 8 Runtime
"@

$DraftFlag = if ($Draft) { "--draft" } else { "" }

Push-Location $ProjectDir

# Create release and upload asset
if ($Draft) {
    gh release create "v$Version" $ZipPath --title "v$Version" --notes $ReleaseNotes --draft
} else {
    gh release create "v$Version" $ZipPath --title "v$Version" --notes $ReleaseNotes
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to create GitHub release!" -ForegroundColor Red
    Pop-Location
    exit 1
}

Pop-Location

Write-Host ""
Write-Host "=== Release Complete ===" -ForegroundColor Green
Write-Host "Version: v$Version"
Write-Host "Asset: $ZipName"
if ($Draft) {
    Write-Host "Status: DRAFT (publish manually on GitHub)"
} else {
    Write-Host "Status: Published"
}

