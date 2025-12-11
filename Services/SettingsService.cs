using System.IO;
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

        _loaded = true;
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

