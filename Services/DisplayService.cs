using System.Runtime.InteropServices;
using DisplayRefreshRate.Native;

namespace DisplayRefreshRate.Services;

public class DisplayService
{
    private bool _isPrimaryDisplayOnly = true;
    private volatile DisplayMode _currentMode = DisplayMode.Primary;

    /// <summary>
    /// Sets the display to the maximum available refresh rate for the current resolution.
    /// </summary>
    public void SetMaxRefreshRate(string? deviceName = null)
    {
        var devMode = new NativeMethods.DEVMODE();
        devMode.dmSize = (ushort)Marshal.SizeOf<NativeMethods.DEVMODE>();

        // Get current display settings
        var current = new NativeMethods.DEVMODE();
        current.dmSize = (ushort)Marshal.SizeOf<NativeMethods.DEVMODE>();
        NativeMethods.EnumDisplaySettingsW(deviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref current);

        uint targetWidth = current.dmPelsWidth;
        uint targetHeight = current.dmPelsHeight;

        int modeNum = 0;
        int maxHz = 0;
        NativeMethods.DEVMODE bestMode = default;
        bestMode.dmSize = (ushort)Marshal.SizeOf<NativeMethods.DEVMODE>();

        // Enumerate all display modes to find highest refresh rate
        while (NativeMethods.EnumDisplaySettingsW(deviceName, modeNum++, ref devMode))
        {
            if (devMode.dmPelsWidth == targetWidth && devMode.dmPelsHeight == targetHeight)
            {
                // Cap at 360Hz to avoid invalid modes
                if (devMode.dmDisplayFrequency > maxHz && devMode.dmDisplayFrequency < 361)
                {
                    maxHz = (int)devMode.dmDisplayFrequency;
                    bestMode = devMode;
                }
            }
        }

        if (maxHz == 0)
            return;

        int result = NativeMethods.ChangeDisplaySettingsExW(deviceName, ref bestMode, IntPtr.Zero, 0, IntPtr.Zero);
        if (result != NativeMethods.DISP_CHANGE_SUCCESSFUL)
        {
            System.Windows.MessageBox.Show("Failed to apply refresh rate.", "DisplayRefreshRate", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Toggles between primary and external display using DisplaySwitch.exe.
    /// </summary>
    public void TogglePrimarySecondaryDisplay()
    {
        bool switchingToExternal = _isPrimaryDisplayOnly;
        string args = switchingToExternal ? "/external" : "/internal";

        IntPtr result = NativeMethods.ShellExecuteW(IntPtr.Zero, "open", "DisplaySwitch.exe", args, null, NativeMethods.SW_HIDE);
        
        if ((long)result <= 32)
        {
            System.Windows.MessageBox.Show("Failed to launch DisplaySwitch.exe.", "DisplayRefreshRate",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        _currentMode = switchingToExternal ? DisplayMode.Secondary : DisplayMode.Primary;
        _isPrimaryDisplayOnly = !_isPrimaryDisplayOnly;
    }

    /// <summary>
    /// Gets the current display mode (Primary or Secondary).
    /// </summary>
    public DisplayMode GetCurrentMode() => _currentMode;

    /// <summary>
    /// Returns true if currently showing only the primary display.
    /// </summary>
    public bool IsPrimaryDisplayOnly() => _isPrimaryDisplayOnly;
}

