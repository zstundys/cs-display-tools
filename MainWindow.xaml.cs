using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DisplayRefreshRate.Services;
using NAudio.CoreAudioApi;
using Wpf.Ui.Controls;

namespace DisplayRefreshRate;

public partial class MainWindow : FluentWindow
{
    private readonly DisplayService _displayService;
    private readonly AudioService _audioService;
    private readonly SettingsService _settingsService;
    private readonly HotkeyService _hotkeyService;
    private bool _isLoadingDevices;

    public MainWindow(DisplayService displayService, AudioService audioService, SettingsService settingsService, HotkeyService hotkeyService)
    {
        InitializeComponent();
        _displayService = displayService;
        _audioService = audioService;
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;

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
        
        // Load max Hz settings
        PrimaryMaxHzBox.Value = _settingsService.PrimaryMaxHz;
        ExternalMaxHzBox.Value = _settingsService.ExternalMaxHz;
        
        // Load shortcuts
        UpdateShortcutButtons();
    }
    
    private void UpdateShortcutButtons()
    {
        SetMaxRefreshShortcutButton.Content = GetShortcutString(_settingsService.SetMaxRefreshShortcutModifiers, _settingsService.SetMaxRefreshShortcutKey);
        ToggleDisplayShortcutButton.Content = GetShortcutString(_settingsService.ToggleDisplayShortcutModifiers, _settingsService.ToggleDisplayShortcutKey);
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

    private void PrimaryMaxHzBox_ValueChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoadingDevices) return;
        
        int value = (int)(PrimaryMaxHzBox.Value ?? 0);
        _settingsService.PrimaryMaxHz = value;
        _settingsService.Save();
    }

    private void ExternalMaxHzBox_ValueChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoadingDevices) return;
        
        int value = (int)(ExternalMaxHzBox.Value ?? 119);
        _settingsService.ExternalMaxHz = value;
        _settingsService.Save();
    }

    private void SetMaxRefreshShortcut_Click(object sender, RoutedEventArgs e)
    {
        EditShortcut("Set Max Refresh Rate", 
            _settingsService.SetMaxRefreshShortcutKey, 
            _settingsService.SetMaxRefreshShortcutModifiers,
            (key, modifiers) => 
            {
                _settingsService.SetMaxRefreshShortcutKey = key;
                _settingsService.SetMaxRefreshShortcutModifiers = modifiers;
                _settingsService.Save();
                UpdateShortcutButtons();
            });
    }

    private void ToggleDisplayShortcut_Click(object sender, RoutedEventArgs e)
    {
        EditShortcut("Toggle Display + Audio", 
            _settingsService.ToggleDisplayShortcutKey, 
            _settingsService.ToggleDisplayShortcutModifiers,
            (key, modifiers) => 
            {
                _settingsService.ToggleDisplayShortcutKey = key;
                _settingsService.ToggleDisplayShortcutModifiers = modifiers;
                _settingsService.Save();
                UpdateShortcutButtons();
            });
    }

    private void RestoreSetMaxRefreshShortcut_Click(object sender, RoutedEventArgs e)
    {
        _settingsService.SetMaxRefreshShortcutKey = Key.F12;
        _settingsService.SetMaxRefreshShortcutModifiers = ModifierKeys.Control | ModifierKeys.Alt;
        _settingsService.Save();
        UpdateShortcutButtons();
        
        var app = (App)System.Windows.Application.Current;
        app.RefreshHotkeys();
    }

    private void RestoreToggleDisplayShortcut_Click(object sender, RoutedEventArgs e)
    {
        _settingsService.ToggleDisplayShortcutKey = Key.F11;
        _settingsService.ToggleDisplayShortcutModifiers = ModifierKeys.Control | ModifierKeys.Alt;
        _settingsService.Save();
        UpdateShortcutButtons();
        
        var app = (App)System.Windows.Application.Current;
        app.RefreshHotkeys();
    }

    private void EditShortcut(string title, Key currentKey, ModifierKeys currentModifiers, Action<Key, ModifierKeys> onSave)
    {
        var app = (App)System.Windows.Application.Current;
        app.UnregisterHotkeys();
        
        try
        {
            var editor = new ShortcutEditorWindow(currentKey, currentModifiers)
            {
                Owner = this,
                Title = $"Edit Shortcut: {title}"
            };
            
            if (editor.ShowDialog() == true)
            {
                onSave(editor.ResultKey, editor.ResultModifiers);
            }
        }
        finally
        {
            app.RefreshHotkeys();
        }
    }
    
    private string GetShortcutString(ModifierKeys modifiers, Key key)
    {
        var sb = new System.Text.StringBuilder();
        if ((modifiers & ModifierKeys.Control) != 0) sb.Append("Ctrl+");
        if ((modifiers & ModifierKeys.Alt) != 0) sb.Append("Alt+");
        if ((modifiers & ModifierKeys.Shift) != 0) sb.Append("Shift+");
        if ((modifiers & ModifierKeys.Windows) != 0) sb.Append("Win+");
        sb.Append(GetFriendlyKeyName(key));
        return sb.ToString();
    }

    private static string GetFriendlyKeyName(Key key)
    {
        return key switch
        {
            Key.Next => "PageDown",
            Key.Prior => "PageUp",
            Key.Back => "Backspace",
            Key.Capital => "CapsLock",
            Key.Escape => "Esc",
            Key.Return => "Enter",
            Key.Snapshot => "PrintScreen",
            Key.Scroll => "ScrollLock",
            _ => key.ToString()
        };
    }

    // Window now closes normally to free GPU memory
    // App.xaml.cs will recreate it when needed
}
