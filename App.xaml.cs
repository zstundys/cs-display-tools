using System.Windows;
using DisplayRefreshRate.Services;
using Wpf.Ui.Appearance;

namespace DisplayRefreshRate;

public partial class App : System.Windows.Application
{
    private TrayService? _trayService;
    private HotkeyService? _hotkeyService;
    private DisplayService? _displayService;
    private AudioService? _audioService;
    private SettingsService? _settingsService;
    private MainWindow? _mainWindow;  // Created on-demand, destroyed when closed
    private int _lastKnownHz;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Apply system theme (light/dark) on startup
        var systemTheme = ApplicationThemeManager.GetSystemTheme();
        var appTheme = systemTheme == SystemTheme.Dark || systemTheme == SystemTheme.HC1 || systemTheme == SystemTheme.HC2 
            ? ApplicationTheme.Dark 
            : ApplicationTheme.Light;
        ApplicationThemeManager.Apply(appTheme);

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
            
            // Listen for display changes (resolution, Hz changes from Windows or other apps)
            _hotkeyService.OnDisplayChanged = OnDisplayChanged;

            // Initialize tray (window created on-demand to save GPU memory)
            _trayService = new TrayService();
            _trayService.AddMenuItem("Open Settings", ShowSettings);
            _trayService.AddSeparator();
            _trayService.AddMenuItem("Set Max Refresh Rate (Ctrl+Alt+F12)", OnSetMaxRefreshRate);
            _trayService.AddMenuItem("Toggle Display+Audio (Ctrl+Alt+F11)", OnToggleDisplay);
            _trayService.AddSeparator();
            _trayService.AddMenuItem("Exit", () => Shutdown());
            // Set max refresh rate on startup
            _displayService.SetMaxRefreshRate();
            
            _lastKnownHz = _displayService.GetCurrentRefreshRate();
            _trayService.Initialize(_lastKnownHz);
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
        // Create window on-demand if it doesn't exist or was closed
        if (_mainWindow == null || !_mainWindow.IsLoaded)
        {
            _mainWindow = new MainWindow(_displayService!, _audioService!, _settingsService!);
            _mainWindow.Closed += (s, e) => _mainWindow = null;  // Clear reference when closed
            
            // Watch for system theme changes on this window
            SystemThemeWatcher.Watch(_mainWindow);
        }
        
        _mainWindow.UpdateStatus();
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void OnSetMaxRefreshRate()
    {
        _displayService?.SetMaxRefreshRate();
        UpdateTrayIcon();
    }

    private void UpdateTrayIcon()
    {
        if (_displayService != null && _trayService != null)
        {
            _lastKnownHz = _displayService.GetCurrentRefreshRate();
            _trayService.UpdateRefreshRate(_lastKnownHz);
        }
    }

    private void OnDisplayChanged()
    {
        // Called when Windows notifies us of display setting changes
        // Add a small delay as the display might not be fully initialized yet
        Task.Run(async () =>
        {
            try
            {
                // Wait for display to settle
                await Task.Delay(500);
                
                Dispatcher.Invoke(() =>
                {
                    if (_displayService == null || _trayService == null) return;

                    int currentHz = _displayService.GetCurrentRefreshRate();
                    if (currentHz > 0)
                    {
                        _lastKnownHz = currentHz;
                        _trayService.UpdateRefreshRate(currentHz);
                    }
                    
                    // Refresh context menu to fix DPI scaling after display switch
                    _trayService.RefreshContextMenu();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnDisplayChanged error: {ex.Message}");
            }
        });
    }

    private void OnToggleDisplay()
    {
        if (_displayService == null || _audioService == null || _settingsService == null) return;

        bool switchingToExternal = _displayService.IsPrimaryDisplayOnly();
        _displayService.TogglePrimarySecondaryDisplay();
        _audioService.SetCurrentDisplayMode(switchingToExternal);
        _audioService.SetDefaultAudioForMode(switchingToExternal);

        // Set max refresh rate after display switch (with delay for display to settle)
        // Use configured max Hz for each display mode (0 = no limit)
        int maxHz = switchingToExternal ? _settingsService.ExternalMaxHz : _settingsService.PrimaryMaxHz;
        Task.Run(async () =>
        {
            await Task.Delay(3000);  // Wait for display switch to complete
            _displayService.SetMaxRefreshRate(null, maxHz);
            
            // Wait a bit more for the Hz change to apply, then update icon
            await Task.Delay(1000);
            Dispatcher.Invoke(() =>
            {
                int currentHz = _displayService.GetCurrentRefreshRate();
                if (currentHz > 0)
                {
                    _lastKnownHz = currentHz;
                    _trayService?.UpdateRefreshRate(currentHz);
                }
            });
        });
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

