using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;

namespace DisplayRefreshRate.Services;

public class TrayService : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private readonly ContextMenu _contextMenu;
    private bool _disposed;
    private Icon? _currentIcon;

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
        var menuItem = new MenuItem { Header = text };
        menuItem.Click += (s, e) => onClick();
        _contextMenu.Items.Add(menuItem);
    }

    /// <summary>
    /// Adds a separator to the tray context menu.
    /// </summary>
    public void AddSeparator()
    {
        _contextMenu.Items.Add(new Separator());
    }

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

    private static Icon CreateCustomIcon(int hz = 0)
    {
        // Create a 32x32 icon showing the current Hz
        const int size = 32;
        using var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Transparent);

        // Colors - cyan/teal accent
        var accentColor = Color.FromArgb(0, 210, 210);
        var darkColor = Color.FromArgb(30, 30, 40);
        
        using var accentBrush = new SolidBrush(accentColor);
        using var darkBrush = new SolidBrush(darkColor);
        using var accentPen = new Pen(accentColor, 2f);

        // Background circle
        g.FillEllipse(darkBrush, 1, 1, 30, 30);
        g.DrawEllipse(accentPen, 1, 1, 30, 30);

        // Hz value text - adjust size based on digit count
        string text = hz > 0 ? hz.ToString() : "Hz";
        float fontSize = text.Length >= 3 ? 7f : (text.Length == 2 ? 9f : 11f);
        
        // Use monospace font for consistent character width
        using var font = new Font("Consolas", fontSize, System.Drawing.FontStyle.Bold);
        
        // Measure with no padding
        var textSize = TextRenderer.MeasureText(text, font, new System.Drawing.Size(32, 32), TextFormatFlags.NoPadding);
        
        // Calculate center position
        int x = (32 - textSize.Width) / 2;
        int y = (32 - textSize.Height) / 2;
        
        // Draw using TextRenderer for accurate positioning
        TextRenderer.DrawText(g, text, font, new System.Drawing.Point(x, y), accentColor, TextFormatFlags.NoPadding);

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

