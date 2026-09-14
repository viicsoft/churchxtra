using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChurchAI.App.Helpers;

public static class TextBoxSelectAllBehavior
{
    public static readonly DependencyProperty AutoSelectAllOnFocusProperty =
        DependencyProperty.RegisterAttached(
            "AutoSelectAllOnFocus",
            typeof(bool),
            typeof(TextBoxSelectAllBehavior),
            new FrameworkPropertyMetadata(false, OnAutoSelectAllOnFocusChanged));

    public static bool GetAutoSelectAllOnFocus(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoSelectAllOnFocusProperty);
    }

    public static void SetAutoSelectAllOnFocus(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoSelectAllOnFocusProperty, value);
    }

    private static void OnAutoSelectAllOnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            if ((bool)e.NewValue)
            {
                textBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
                textBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
            }
            else
            {
                textBox.GotKeyboardFocus -= TextBox_GotKeyboardFocus;
                textBox.PreviewMouseLeftButtonDown -= TextBox_PreviewMouseLeftButtonDown;
            }
        }
    }

    private static void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
        {
            textBox.SelectAll();
        }
    }

    private static void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox textBox && !textBox.IsKeyboardFocusWithin)
        {
            textBox.Focus();
            if (!string.IsNullOrEmpty(textBox.Text))
            {
                textBox.SelectAll();
            }
            e.Handled = true;
        }
    }
}
