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
    }

    public void UpdateStatus()
    {
        var mode = _displayService.GetCurrentMode();
        CurrentModeText.Text = mode == DisplayMode.Primary ? "Primary" : "External";
        CurrentModeBadge.Appearance = mode == DisplayMode.Primary 
            ? ControlAppearance.Success 
            : ControlAppearance.Caution;
    }

    private void LoadAudioDevices()
    {
        _isLoadingDevices = true;
        
        try
        {
            PrimaryAudioCombo.Items.Clear();
            SecondaryAudioCombo.Items.Clear();

            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in devices)
            {
                PrimaryAudioCombo.Items.Add(device.FriendlyName);
                SecondaryAudioCombo.Items.Add(device.FriendlyName);
            }

            // Select current settings
            if (!string.IsNullOrEmpty(_settingsService.PrimaryDevice))
            {
                for (int i = 0; i < PrimaryAudioCombo.Items.Count; i++)
                {
                    if (PrimaryAudioCombo.Items[i]?.ToString()?.Contains(_settingsService.PrimaryDevice, StringComparison.OrdinalIgnoreCase) == true ||
                        _settingsService.PrimaryDevice.Contains(PrimaryAudioCombo.Items[i]?.ToString() ?? "", StringComparison.OrdinalIgnoreCase))
                    {
                        PrimaryAudioCombo.SelectedIndex = i;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(_settingsService.SecondaryDevice))
            {
                for (int i = 0; i < SecondaryAudioCombo.Items.Count; i++)
                {
                    if (SecondaryAudioCombo.Items[i]?.ToString()?.Contains(_settingsService.SecondaryDevice, StringComparison.OrdinalIgnoreCase) == true ||
                        _settingsService.SecondaryDevice.Contains(SecondaryAudioCombo.Items[i]?.ToString() ?? "", StringComparison.OrdinalIgnoreCase))
                    {
                        SecondaryAudioCombo.SelectedIndex = i;
                        break;
                    }
                }
            }
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

    private void PrimaryAudioCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingDevices || PrimaryAudioCombo.SelectedItem == null) return;
        
        _settingsService.PrimaryDevice = PrimaryAudioCombo.SelectedItem.ToString() ?? "";
        _settingsService.Save();
    }

    private void SecondaryAudioCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingDevices || SecondaryAudioCombo.SelectedItem == null) return;
        
        _settingsService.SecondaryDevice = SecondaryAudioCombo.SelectedItem.ToString() ?? "";
        _settingsService.Save();
    }

    private void SetMaxHz_Click(object sender, RoutedEventArgs e)
    {
        _displayService.SetMaxRefreshRate();
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

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Hide instead of close
        e.Cancel = true;
        Hide();
    }
}

