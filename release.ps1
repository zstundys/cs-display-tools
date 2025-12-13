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

# Get the previous release tag
$PreviousTag = git describe --tags --abbrev=0 2>$null
if ($PreviousTag) {
    Write-Host "Previous release: $PreviousTag" -ForegroundColor Gray
    $CommitRange = "$PreviousTag..HEAD"
} else {
    Write-Host "No previous release found, including all commits" -ForegroundColor Gray
    $CommitRange = "HEAD"
}

# Get commit messages since last release
$CommitLines = git log $CommitRange --pretty=format:"- %s" --no-merges 2>$null
if ($CommitLines) {
    $Commits = $CommitLines -join "`n"
} else {
    $Commits = "- Various improvements and bug fixes"
}

# Build release notes
$ReleaseNotes = @"
## DisplayRefreshRate v$Version

### Changes
$Commits

### Installation
1. Download ``$ZipName``
2. Extract and run ``$ProjectName.exe``
3. (Optional) Enable "Run on Startup" in settings

### Requirements
- Windows 10/11
- .NET 8 Runtime
"@

# Find git repo root
$RepoRoot = git rev-parse --show-toplevel 2>$null
if (-not $RepoRoot) {
    Write-Host "ERROR: Not in a git repository!" -ForegroundColor Red
    exit 1
}

# Verify zip exists before proceeding
if (-not (Test-Path $ZipPath)) {
    Write-Host "ERROR: Zip file not found at: $ZipPath" -ForegroundColor Red
    exit 1
}

Write-Host "Repo root: $RepoRoot"
Write-Host "Uploading: $ZipPath"

Push-Location $ProjectDir

# Write release notes to temp file (avoids issues with multi-line strings in gh CLI)
$NotesFile = Join-Path $ProjectDir "release-notes-temp.md"
$ReleaseNotes | Out-File -FilePath $NotesFile -Encoding utf8

# Create release and upload asset
$ErrorActionPreference = "Continue"
if ($Draft) {
    gh release create "v$Version" $ZipName --title "v$Version" --notes-file $NotesFile --draft
} else {
    gh release create "v$Version" $ZipName --title "v$Version" --notes-file $NotesFile
}
$releaseResult = $LASTEXITCODE
$ErrorActionPreference = "Stop"

# Clean up temp notes file
if (Test-Path $NotesFile) {
    Remove-Item $NotesFile -Force
}

Pop-Location

if ($releaseResult -ne 0) {
    Write-Host "ERROR: Failed to create GitHub release!" -ForegroundColor Red
    Write-Host "Make sure you have pushed your commits and the repository exists on GitHub." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "=== Release Complete ===" -ForegroundColor Green
Write-Host "Version: v$Version"
Write-Host "Asset: $ZipName"
if ($Draft) {
    Write-Host "Status: DRAFT (publish manually on GitHub)"
} else {
    Write-Host "Status: Published"
}

