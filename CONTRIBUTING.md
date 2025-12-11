# Contributing to DisplayRefreshRate

Thank you for your interest in contributing!

## Development Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11
- (Optional) Visual Studio 2022 or VS Code with C# extension

### Building

```bash
# Clone the repository
git clone https://github.com/zstundys/cs-display-tools.git
cd cs-display-tools

# Build debug version
dotnet build

# Or use the batch script
build.bat
```

### Running

```bash
# Run debug build
dotnet run

# Or run the built executable
.\bin\Debug\net8.0-windows\win-x64\DisplayRefreshRate.exe
```

## Project Structure

```
DisplayRefreshRate.CSharp/
├── App.xaml / App.xaml.cs      # Application entry point and tray setup
├── MainWindow.xaml / .cs       # Settings window UI
├── Services/
│   ├── DisplayService.cs       # Display enumeration and refresh rate control
│   ├── AudioService.cs         # Audio device switching via CoreAudio
│   ├── TrayService.cs          # System tray icon and context menu
│   ├── HotkeyService.cs        # Global hotkey registration
│   └── SettingsService.cs      # INI file persistence
├── Native/
│   └── NativeMethods.cs        # P/Invoke declarations
├── app.ico                     # Application icon
├── build.bat                   # Debug build script
├── publish.bat                 # Self-contained release (~150MB)
├── publish-small.bat           # Framework-dependent release (~7MB)
├── release.bat                 # GitHub release script
├── release.ps1                 # GitHub release PowerShell script
└── create-icon.ps1             # Icon generation script
```

## Creating a Release

Releases are created using GitHub CLI.

### Prerequisites

1. Install [GitHub CLI](https://cli.github.com/)
2. Authenticate: `gh auth login`

### Creating a Release

```bash
# Create a release (e.g., version 1.0.0)
release.bat 1.0.0

# Create a draft release (for review before publishing)
release.bat 1.0.0 --draft
```

Or using PowerShell directly:

```powershell
# Create release
.\release.ps1 -Version "1.0.0"

# Create draft release
.\release.ps1 -Version "1.0.0" -Draft

# Skip build (use existing build)
.\release.ps1 -Version "1.0.0" -SkipBuild
```

### What the release script does:

1. Builds the release single-file executable
2. Creates `DisplayRefreshRate-v{version}.zip`
3. Creates a GitHub release with auto-generated release notes
4. Uploads the zip as a release asset

## Code Style

- Use C# 12 features where appropriate
- Follow standard .NET naming conventions
- Keep services focused and single-purpose
- Use `async/await` for I/O operations
- Handle exceptions gracefully (especially for COM/P/Invoke calls)

## Pull Requests

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Make your changes
4. Test thoroughly (especially display/audio switching)
5. Commit with clear messages
6. Push to your fork
7. Open a Pull Request

## Reporting Issues

When reporting issues, please include:

- Windows version
- Display configuration (resolution, refresh rates)
- Audio devices involved
- Steps to reproduce
- Any error messages

