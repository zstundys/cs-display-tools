using System.IO;
using DisplayRefreshRate.Native;

namespace DisplayRefreshRate.Services;

public class SettingsService
{
    private string _configPath = string.Empty;
    private bool _loaded = false;

    public string PrimaryDevice { get; set; } = string.Empty;
    public string SecondaryDevice { get; set; } = string.Empty;

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

        _loaded = true;
    }

    /// <summary>
    /// Saves settings to the INI file.
    /// </summary>
    public void Save()
    {
        NativeMethods.WritePrivateProfileStringW("Audio", "Primary", PrimaryDevice, _configPath);
        NativeMethods.WritePrivateProfileStringW("Audio", "Secondary", SecondaryDevice, _configPath);
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

