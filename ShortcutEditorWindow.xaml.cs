using System.Text;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace DisplayRefreshRate;

public partial class ShortcutEditorWindow : FluentWindow
{
    public Key ResultKey { get; private set; } = Key.None;
    public ModifierKeys ResultModifiers { get; private set; } = ModifierKeys.None;

    private Key _currentKey = Key.None;
    private ModifierKeys _currentModifiers = ModifierKeys.None;
    
    // Track if we have a valid combination locked in
    private bool _isLocked = false;

    public ShortcutEditorWindow(Key initialKey, ModifierKeys initialModifiers)
    {
        InitializeComponent();
        _currentKey = initialKey;
        _currentModifiers = initialModifiers;
        _isLocked = initialKey != Key.None && initialModifiers != ModifierKeys.None;
        UpdateDisplay();
        Validate();
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;

        // Get key
        Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

        // If the key is a modifier key, show current modifiers being held
        if (IsModifierKey(key))
        {
            // If we're starting a new combination (pressing modifiers), unlock
            if (_isLocked)
            {
                _isLocked = false;
                _currentKey = Key.None;
            }
            _currentModifiers = Keyboard.Modifiers;
        }
        else
        {
            // Non-modifier key pressed - capture the full combination and lock it
            _currentModifiers = Keyboard.Modifiers;
            _currentKey = key;
            _isLocked = true;
        }

        UpdateDisplay();
        Validate();
    }

    private void Window_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;
        
        // If we have a locked combination, don't change anything on key release
        if (_isLocked)
            return;
        
        // Otherwise update to show current modifiers being held
        _currentModifiers = Keyboard.Modifiers;
        UpdateDisplay();
        Validate();
    }

    private static bool IsModifierKey(Key key)
    {
        return key == Key.LeftCtrl || key == Key.RightCtrl ||
               key == Key.LeftAlt || key == Key.RightAlt ||
               key == Key.LeftShift || key == Key.RightShift ||
               key == Key.LWin || key == Key.RWin;
    }

    private void UpdateDisplay()
    {
        var sb = new StringBuilder();
        
        if (_currentModifiers.HasFlag(ModifierKeys.Control)) sb.Append("Ctrl + ");
        if (_currentModifiers.HasFlag(ModifierKeys.Alt)) sb.Append("Alt + ");
        if (_currentModifiers.HasFlag(ModifierKeys.Shift)) sb.Append("Shift + ");
        if (_currentModifiers.HasFlag(ModifierKeys.Windows)) sb.Append("Win + ");

        if (_currentKey != Key.None)
        {
            sb.Append(GetFriendlyKeyName(_currentKey));
        }
        else if (sb.Length > 3) 
        {
            // Remove trailing " + "
            sb.Length -= 3;
        }
        else
        {
            sb.Append("Press a key combination...");
        }

        ShortcutText.Text = sb.ToString();
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

    private void Validate()
    {
        // Valid if we have a key AND modifiers
        // User requirement: "shouldn't be possible to define a single-key shortcut -- it should always be a combination"
        
        bool hasModifiers = _currentModifiers != ModifierKeys.None;
        bool hasKey = _currentKey != Key.None;
        
        bool isValid = hasModifiers && hasKey;
        
        SaveButton.IsEnabled = isValid;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ResultKey = _currentKey;
        ResultModifiers = _currentModifiers;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
