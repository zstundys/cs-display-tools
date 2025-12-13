using System.IO;
using System.Windows.Input;
using DisplayRefreshRate.Native;

namespace DisplayRefreshRate.Services;

public class SettingsService
{
    private string _configPath = string.Empty;
    private bool _loaded = false;

    public string PrimaryDevice { get; set; } = string.Empty;
    public string SecondaryDevice { get; set; } = string.Empty;
    
    /// <summary>
    /// Max refresh rate for primary display (0 = no limit).
    /// </summary>
    public int PrimaryMaxHz { get; set; } = 0;
    
    /// <summary>
    /// Max refresh rate for external display (0 = no limit). Default 119Hz for 12-bit HDR color support.
    /// </summary>
    public int ExternalMaxHz { get; set; } = 119;

    // Default shortcuts (defined out-of-the-box)
    public Key SetMaxRefreshShortcutKey { get; set; } = Key.F12;
    public ModifierKeys SetMaxRefreshShortcutModifiers { get; set; } = ModifierKeys.Control | ModifierKeys.Alt;

    public Key ToggleDisplayShortcutKey { get; set; } = Key.F11;
    public ModifierKeys ToggleDisplayShortcutModifiers { get; set; } = ModifierKeys.Control | ModifierKeys.Alt;

    public SettingsService()
    {
        _configPath = GetConfigPath();
    }

    /// <summary>
    /// Loads settings from the INI file.
    /// </summary>
    public void Load()
    {
        if (_loaded) return;

        char[] buffer = new char[256];

        NativeMethods.GetPrivateProfileStringW("Audio", "Primary", "", buffer, (uint)buffer.Length, _configPath);
        PrimaryDevice = new string(buffer).TrimEnd('\0');

        NativeMethods.GetPrivateProfileStringW("Audio", "Secondary", "", buffer, (uint)buffer.Length, _configPath);
        SecondaryDevice = new string(buffer).TrimEnd('\0');

        NativeMethods.GetPrivateProfileStringW("Display", "PrimaryMaxHz", "0", buffer, (uint)buffer.Length, _configPath);
        string primaryMaxHzStr = new string(buffer).TrimEnd('\0');
        if (int.TryParse(primaryMaxHzStr, out int primaryMaxHz) && primaryMaxHz >= 0)
        {
            PrimaryMaxHz = primaryMaxHz;
        }

        NativeMethods.GetPrivateProfileStringW("Display", "ExternalMaxHz", "119", buffer, (uint)buffer.Length, _configPath);
        string externalMaxHzStr = new string(buffer).TrimEnd('\0');
        if (int.TryParse(externalMaxHzStr, out int externalMaxHz) && externalMaxHz >= 0)
        {
            ExternalMaxHz = externalMaxHz;
        }

        // Load Shortcuts
        LoadShortcut("SetMaxRefresh", k => SetMaxRefreshShortcutKey = k, m => SetMaxRefreshShortcutModifiers = m);
        LoadShortcut("ToggleDisplay", k => ToggleDisplayShortcutKey = k, m => ToggleDisplayShortcutModifiers = m);

        _loaded = true;
    }

    private void LoadShortcut(string prefix, Action<Key> setKey, Action<ModifierKeys> setModifiers)
    {
        char[] buffer = new char[256];
        
        NativeMethods.GetPrivateProfileStringW("Shortcuts", $"{prefix}Key", "", buffer, (uint)buffer.Length, _configPath);
        string keyStr = new string(buffer).TrimEnd('\0');
        if (!string.IsNullOrEmpty(keyStr) && Enum.TryParse<Key>(keyStr, out var key))
        {
            setKey(key);
        }

        NativeMethods.GetPrivateProfileStringW("Shortcuts", $"{prefix}Modifiers", "", buffer, (uint)buffer.Length, _configPath);
        string modStr = new string(buffer).TrimEnd('\0');
        if (!string.IsNullOrEmpty(modStr) && Enum.TryParse<ModifierKeys>(modStr, out var modifiers))
        {
            setModifiers(modifiers);
        }
    }

    /// <summary>
    /// Saves settings to the INI file.
    /// </summary>
    public void Save()
    {
        NativeMethods.WritePrivateProfileStringW("Audio", "Primary", PrimaryDevice, _configPath);
        NativeMethods.WritePrivateProfileStringW("Audio", "Secondary", SecondaryDevice, _configPath);
        NativeMethods.WritePrivateProfileStringW("Display", "PrimaryMaxHz", PrimaryMaxHz.ToString(), _configPath);
        NativeMethods.WritePrivateProfileStringW("Display", "ExternalMaxHz", ExternalMaxHz.ToString(), _configPath);

        // Save Shortcuts
        NativeMethods.WritePrivateProfileStringW("Shortcuts", "SetMaxRefreshKey", SetMaxRefreshShortcutKey.ToString(), _configPath);
        NativeMethods.WritePrivateProfileStringW("Shortcuts", "SetMaxRefreshModifiers", SetMaxRefreshShortcutModifiers.ToString(), _configPath);

        NativeMethods.WritePrivateProfileStringW("Shortcuts", "ToggleDisplayKey", ToggleDisplayShortcutKey.ToString(), _configPath);
        NativeMethods.WritePrivateProfileStringW("Shortcuts", "ToggleDisplayModifiers", ToggleDisplayShortcutModifiers.ToString(), _configPath);
    }

    private static string GetConfigPath()
    {
        char[] modulePath = new char[260]; // MAX_PATH
        NativeMethods.GetModuleFileNameW(IntPtr.Zero, modulePath, (uint)modulePath.Length);

        string path = new string(modulePath).TrimEnd('\0');
        string? directory = Path.GetDirectoryName(path);
        
        return Path.Combine(directory ?? ".", "audio.ini");
    }
}
