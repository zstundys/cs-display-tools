# DisplayRefreshRate

A Windows system tray utility for managing display refresh rates and audio output switching.

![Screenshot](screens/demo.png)

## Features

- **Auto Max Refresh Rate** - Automatically sets your display to the highest available refresh rate on startup
- **Display Toggle** - Switch between primary and external displays with a hotkey
- **Audio Switching** - Automatically switches audio output when changing displays
- **Refresh Rate Limit** - External display can be capped (e.g., 119Hz for 12-bit HDR color)
- **System Tray** - Lives in the tray with current Hz displayed in the icon
- **Windows 11 UI** - Modern Fluent Design settings window

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl + Alt + F12` | Set display to max refresh rate |
| `Ctrl + Alt + F11` | Toggle between primary/external display + switch audio |

## Installation

1. Download the latest release
2. Run `DisplayRefreshRate.exe`
3. (Optional) Enable "Run on Startup" in settings

## Building from Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
# Debug build
dotnet build

# Release build (single-file)
dotnet publish -c Release --self-contained false -p:PublishSingleFile=true -o publish
```

Or use the included batch scripts:
- `build.bat` - Debug build
- `publish.bat` - Release single-file executable

## Configuration

Settings are stored in `audio.ini` next to the executable:

```ini
[Audio]
Primary=Speakers (Realtek Audio)
Secondary=LG TV SSCR2 (NVIDIA High Definition Audio)
```

## How It Works

### Display Switching
Uses Windows' built-in `DisplaySwitch.exe` to toggle between:
- `/internal` - Primary display only
- `/external` - External display only

### Audio Switching
Uses the Windows Core Audio API to:
1. Remember which audio device you use for each display mode
2. Automatically switch when displays change
3. Learn new preferences when you manually change audio output

### Refresh Rate
Enumerates available display modes and selects the highest refresh rate for the current resolution (with optional cap for external displays).

## Requirements

- Windows 10/11
- .NET 8 Runtime (or use self-contained build)

## License

MIT

