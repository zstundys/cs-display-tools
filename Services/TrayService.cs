using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;

namespace DisplayRefreshRate.Services;

public class TrayService : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private readonly ContextMenu _contextMenu;
    private bool _disposed;

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
    public void Initialize()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "DisplayRefreshRate - Double-click for settings",
            ContextMenu = _contextMenu,
            Icon = LoadDefaultIcon(),
            Visibility = Visibility.Visible
        };

        _trayIcon.TrayMouseDoubleClick += (s, e) => OnDoubleClick?.Invoke();
        
        // Force the icon to be created immediately
        _trayIcon.ForceCreate();
    }

    private static System.Drawing.Icon LoadDefaultIcon()
    {
        // Use system information icon as default
        // You can replace this with a custom .ico file
        try
        {
            return System.Drawing.SystemIcons.Application;
        }
        catch
        {
            return System.Drawing.SystemIcons.Information;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _trayIcon?.Dispose();
        _trayIcon = null;
    }
}

