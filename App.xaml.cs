using System.Windows;
using DisplayRefreshRate.Services;

namespace DisplayRefreshRate;

public partial class App : System.Windows.Application
{
    private TrayService? _trayService;
    private HotkeyService? _hotkeyService;
    private DisplayService? _displayService;
    private AudioService? _audioService;
    private SettingsService? _settingsService;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Initialize services
            _settingsService = new SettingsService();
            _settingsService.Load();

            _displayService = new DisplayService();
            _audioService = new AudioService(_settingsService);
            
            // Set initial display mode for audio service
            _audioService.SetCurrentDisplayMode(_displayService.GetCurrentMode() == DisplayMode.Secondary);
            _audioService.Initialize();

            _hotkeyService = new HotkeyService();
            
            // Register hotkeys
            _hotkeyService.RegisterHotkey(HotkeyId.SetMaxRefresh, ModifierKeys.Control | ModifierKeys.Alt, Key.F12, OnSetMaxRefreshRate);
            _hotkeyService.RegisterHotkey(HotkeyId.ToggleDisplay, ModifierKeys.Control | ModifierKeys.Alt, Key.F11, OnToggleDisplay);

            // Create main window (hidden by default)
            _mainWindow = new MainWindow(_displayService, _audioService, _settingsService);

            // Initialize tray
            _trayService = new TrayService();
            _trayService.AddMenuItem("Open Settings", ShowSettings);
            _trayService.AddSeparator();
            _trayService.AddMenuItem("Set Max Refresh Rate (Ctrl+Alt+F12)", OnSetMaxRefreshRate);
            _trayService.AddMenuItem("Toggle Display+Audio (Ctrl+Alt+F11)", OnToggleDisplay);
            _trayService.AddSeparator();
            _trayService.AddMenuItem("Exit", () => Shutdown());
            _trayService.Initialize();
            _trayService.OnDoubleClick = ShowSettings;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to initialize: {ex.Message}\n\n{ex.StackTrace}", 
                "DisplayRefreshRate Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void ShowSettings()
    {
        if (_mainWindow == null) return;
        
        _mainWindow.UpdateStatus();
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void OnSetMaxRefreshRate()
    {
        _displayService?.SetMaxRefreshRate();
    }

    private void OnToggleDisplay()
    {
        if (_displayService == null || _audioService == null) return;

        bool switchingToExternal = _displayService.IsPrimaryDisplayOnly();
        _displayService.TogglePrimarySecondaryDisplay();
        _audioService.SetCurrentDisplayMode(switchingToExternal);
        _audioService.SetDefaultAudioForMode(switchingToExternal);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Dispose();
        _trayService?.Dispose();
        _audioService?.Shutdown();
        
        base.OnExit(e);
    }
}

// Enums used across the application
public enum HotkeyId
{
    SetMaxRefresh = 1,
    ToggleDisplay = 2
}

public enum ModifierKeys
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}

public enum Key
{
    F11 = 0x7A,
    F12 = 0x7B
}

public enum DisplayMode
{
    Primary = 0,
    Secondary = 1
}

