using System.Windows.Input;
using System.Windows.Interop;
using DisplayRefreshRate.Native;

namespace DisplayRefreshRate.Services;

public class HotkeyService : IDisposable
{
    private readonly Dictionary<int, Action> _hotkeyCallbacks = new();
    private HwndSource? _hwndSource;
    private IntPtr _hwnd;
    private bool _disposed;

    private const int WM_DISPLAYCHANGE = 0x007E;

    /// <summary>
    /// Called when display settings change (resolution, refresh rate, etc.)
    /// </summary>
    public Action? OnDisplayChanged { get; set; }

    public HotkeyService()
    {
        // Create a hidden window to receive hotkey and display change messages
        var parameters = new HwndSourceParameters("HotkeyWindow")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
        _hwnd = _hwndSource.Handle;
    }

    /// <summary>
    /// Registers a global hotkey.
    /// </summary>
    public bool RegisterHotkey(HotkeyId id, ModifierKeys modifiers, Key key, Action callback)
    {
        int hotkeyId = (int)id;
        int virtualKey = KeyInterop.VirtualKeyFromKey(key);
        
        if (!NativeMethods.RegisterHotKey(_hwnd, hotkeyId, (uint)modifiers, (uint)virtualKey))
        {
            System.Diagnostics.Debug.WriteLine($"Failed to register hotkey {id}");
            return false;
        }

        _hotkeyCallbacks[hotkeyId] = callback;
        return true;
    }

    /// <summary>
    /// Unregisters a hotkey.
    /// </summary>
    public void UnregisterHotkey(HotkeyId id)
    {
        int hotkeyId = (int)id;
        NativeMethods.UnregisterHotKey(_hwnd, hotkeyId);
        _hotkeyCallbacks.Remove(hotkeyId);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();
            if (_hotkeyCallbacks.TryGetValue(hotkeyId, out var callback))
            {
                callback.Invoke();
                handled = true;
            }
        }
        else if (msg == WM_DISPLAYCHANGE)
        {
            // Display settings changed - notify listener
            OnDisplayChanged?.Invoke();
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Unregister all hotkeys
        foreach (var id in _hotkeyCallbacks.Keys.ToList())
        {
            NativeMethods.UnregisterHotKey(_hwnd, id);
        }
        _hotkeyCallbacks.Clear();

        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
        _hwndSource = null;
    }
}
