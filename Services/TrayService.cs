using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using Microsoft.Win32;

namespace DisplayRefreshRate.Services;

public class TrayService : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private ContextMenu _contextMenu;
    private bool _disposed;
    private Icon? _currentIcon;
    
    // Store menu item definitions for recreation
    // TextFunc allows dynamic text (e.g., for checkmark state)
    private readonly List<(Func<string> TextFunc, Action? OnClick, bool IsSeparator)> _menuItems = new();

    public Action? OnDoubleClick { get; set; }

    public TrayService()
    {
        _contextMenu = new ContextMenu();
    }

    /// <summary>
    /// Adds a menu item to the tray context menu.
    /// </summary>
    public void AddMenuItem(string text, Action onClick)
    {
        AddMenuItem(() => text, onClick);
    }

    /// <summary>
    /// Adds a menu item with dynamic text (evaluated each time menu is shown/refreshed).
    /// </summary>
    public void AddMenuItem(Func<string> textFunc, Action onClick)
    {
        _menuItems.Add((textFunc, onClick, false));
        var menuItem = new MenuItem { Header = textFunc() };
        menuItem.Click += (s, e) => onClick();
        _contextMenu.Items.Add(menuItem);
    }

    /// <summary>
    /// Adds a separator to the tray context menu.
    /// </summary>
    public void AddSeparator()
    {
        _menuItems.Add((() => "", null, true));
        _contextMenu.Items.Add(new Separator());
    }

    #region Startup Management
    
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "DisplayRefreshRate";

    /// <summary>
    /// Returns true if the app is set to run on Windows startup.
    /// </summary>
    public static bool IsRunOnStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Enables or disables running on Windows startup.
    /// </summary>
    public static void SetRunOnStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            if (key == null) return;

            if (enable)
            {
                string exePath = Environment.ProcessPath ?? "";
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SetRunOnStartup error: {ex.Message}");
        }
    }

    #endregion

    /// <summary>
    /// Initializes the tray icon and shows it.
    /// </summary>
    public void Initialize(int currentHz = 0)
    {
        _currentIcon = CreateCustomIcon(currentHz);
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = $"DisplayRefreshRate - {currentHz}Hz\nDouble-click for settings",
            ContextMenu = _contextMenu,
            Icon = _currentIcon,
            Visibility = Visibility.Visible
        };

        _trayIcon.TrayMouseDoubleClick += (s, e) => OnDoubleClick?.Invoke();
        
        // Force the icon to be created immediately
        _trayIcon.ForceCreate();
    }

    /// <summary>
    /// Updates the tray icon to show the current refresh rate.
    /// </summary>
    public void UpdateRefreshRate(int hz)
    {
        if (_trayIcon == null || _disposed) return;

        try
        {
            // Dispose old icon to prevent handle leak
            var oldIcon = _currentIcon;
            _currentIcon = CreateCustomIcon(hz);
            _trayIcon.Icon = _currentIcon;
            _trayIcon.ToolTipText = $"DisplayRefreshRate - {hz}Hz\nDouble-click for settings";
            oldIcon?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateRefreshRate error: {ex.Message}");
        }
    }

    /// <summary>
    /// Rebuilds the context menu to fix DPI scaling issues after display switch.
    /// </summary>
    public void RefreshContextMenu()
    {
        if (_trayIcon == null || _disposed) return;

        try
        {
            // Create a completely new ContextMenu to force DPI refresh
            var newMenu = new ContextMenu();
            
            foreach (var (textFunc, onClick, isSeparator) in _menuItems)
            {
                if (isSeparator)
                {
                    newMenu.Items.Add(new Separator());
                }
                else if (onClick != null)
                {
                    var menuItem = new MenuItem { Header = textFunc() };  // Evaluate text dynamically
                    var action = onClick; // Capture for closure
                    menuItem.Click += (s, e) => action();
                    newMenu.Items.Add(menuItem);
                }
            }
            
            _trayIcon.ContextMenu = newMenu;
            _contextMenu = newMenu;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RefreshContextMenu error: {ex.Message}");
        }
    }

    private static Icon CreateCustomIcon(int hz = 0)
    {
        // Create a 256x256 icon for crisp display on high DPI screens (4K)
        // Windows will scale it down as needed for lower DPI
        const int size = 256;
        using var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.Clear(Color.Transparent);

        // Colors - cyan/teal accent
        var accentColor = Color.FromArgb(0, 210, 210);
        var darkColor = Color.FromArgb(30, 30, 40);
        
        using var accentBrush = new SolidBrush(accentColor);
        using var darkBrush = new SolidBrush(darkColor);
        using var accentPen = new Pen(accentColor, 12f);  // Scaled up stroke

        // Background circle
        g.FillEllipse(darkBrush, 8, 8, 240, 240);
        g.DrawEllipse(accentPen, 8, 8, 240, 240);

        // Hz value text - adjust size based on digit count
        string text = hz > 0 ? hz.ToString() : "Hz";
        float fontSize = text.Length >= 3 ? 56f : (text.Length == 2 ? 72f : 80f);
        
        // Use monospace font for consistent character width
        using var font = new Font("Consolas", fontSize, System.Drawing.FontStyle.Bold);
        
        // Measure and center
        var textSize = g.MeasureString(text, font);
        float x = (size - textSize.Width) / 2f;
        float y = (size - textSize.Height) / 2f;
        
        g.DrawString(text, font, accentBrush, x, y);

        // Convert to icon
        IntPtr hIcon = bitmap.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        
        return path;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _trayIcon?.Dispose();
        _trayIcon = null;
        _currentIcon?.Dispose();
        _currentIcon = null;
    }
}

