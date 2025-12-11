using System.Windows;
using System.Windows.Controls;
using DisplayRefreshRate.Services;
using NAudio.CoreAudioApi;
using Wpf.Ui.Controls;

namespace DisplayRefreshRate;

public partial class MainWindow : FluentWindow
{
    private readonly DisplayService _displayService;
    private readonly AudioService _audioService;
    private readonly SettingsService _settingsService;
    private bool _isLoadingDevices;

    public MainWindow(DisplayService displayService, AudioService audioService, SettingsService settingsService)
    {
        InitializeComponent();
        _displayService = displayService;
        _audioService = audioService;
        _settingsService = settingsService;

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateStatus();
        LoadAudioDevices();
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Load startup toggle state
        StartupToggle.IsChecked = TrayService.IsRunOnStartupEnabled();
    }

    public void UpdateStatus()
    {
        // Update display mode
        var mode = _displayService.GetCurrentMode();
        CurrentModeText.Text = mode == DisplayMode.Primary ? "Primary" : "External";
        CurrentModeBadge.Appearance = mode == DisplayMode.Primary 
            ? ControlAppearance.Success 
            : ControlAppearance.Caution;

        // Update refresh rate
        int hz = _displayService.GetCurrentRefreshRate();
        CurrentHzText.Text = $"{hz} Hz";
    }

    private void LoadAudioDevices()
    {
        _isLoadingDevices = true;
        
        try
        {
            PrimaryAudioCombo.Items.Clear();
            SecondaryAudioCombo.Items.Clear();

            // Get currently active devices
            var activeDevices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in devices)
            {
                PrimaryAudioCombo.Items.Add(device.FriendlyName);
                SecondaryAudioCombo.Items.Add(device.FriendlyName);
                activeDevices.Add(device.FriendlyName);
            }

            // Add saved devices from config if not already in list (they may be offline)
            if (!string.IsNullOrEmpty(_settingsService.PrimaryDevice) && 
                !activeDevices.Contains(_settingsService.PrimaryDevice))
            {
                PrimaryAudioCombo.Items.Add($"{_settingsService.PrimaryDevice} (offline)");
            }
            
            if (!string.IsNullOrEmpty(_settingsService.SecondaryDevice) && 
                !activeDevices.Contains(_settingsService.SecondaryDevice))
            {
                SecondaryAudioCombo.Items.Add($"{_settingsService.SecondaryDevice} (offline)");
            }

            // Select current settings
            SelectDeviceInCombo(PrimaryAudioCombo, _settingsService.PrimaryDevice);
            SelectDeviceInCombo(SecondaryAudioCombo, _settingsService.SecondaryDevice);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load audio devices: {ex.Message}");
        }
        finally
        {
            _isLoadingDevices = false;
        }
    }

    private static void SelectDeviceInCombo(System.Windows.Controls.ComboBox combo, string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName)) return;

        for (int i = 0; i < combo.Items.Count; i++)
        {
            string? item = combo.Items[i]?.ToString();
            if (item == null) continue;
            
            // Match exact, contains, or offline version
            if (item.Equals(deviceName, StringComparison.OrdinalIgnoreCase) ||
                item.Contains(deviceName, StringComparison.OrdinalIgnoreCase) ||
                deviceName.Contains(item.Replace(" (offline)", ""), StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedIndex = i;
                break;
            }
        }
    }

    private void PrimaryAudioCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingDevices || PrimaryAudioCombo.SelectedItem == null) return;
        
        // Strip "(offline)" suffix if present
        string device = PrimaryAudioCombo.SelectedItem.ToString() ?? "";
        device = device.Replace(" (offline)", "");
        _settingsService.PrimaryDevice = device;
        _settingsService.Save();
    }

    private void SecondaryAudioCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingDevices || SecondaryAudioCombo.SelectedItem == null) return;
        
        // Strip "(offline)" suffix if present
        string device = SecondaryAudioCombo.SelectedItem.ToString() ?? "";
        device = device.Replace(" (offline)", "");
        _settingsService.SecondaryDevice = device;
        _settingsService.Save();
    }

    private void SetMaxHz_Click(object sender, RoutedEventArgs e)
    {
        _displayService.SetMaxRefreshRate();
        UpdateStatus();  // Refresh the Hz display
    }

    private void ToggleDisplay_Click(object sender, RoutedEventArgs e)
    {
        bool switchingToExternal = _displayService.IsPrimaryDisplayOnly();
        _displayService.TogglePrimarySecondaryDisplay();
        _audioService.SetCurrentDisplayMode(switchingToExternal);
        
        // Run audio switch in background to not block UI
        Task.Run(() => _audioService.SetDefaultAudioForMode(switchingToExternal));
        
        // Update UI after a short delay
        Task.Delay(500).ContinueWith(_ => Dispatcher.Invoke(UpdateStatus));
    }

    private void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        TrayService.SetRunOnStartup(StartupToggle.IsChecked == true);
    }

    // Window now closes normally to free GPU memory
    // App.xaml.cs will recreate it when needed
}

