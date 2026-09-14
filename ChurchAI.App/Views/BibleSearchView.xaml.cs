using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChurchAI.App.Views;

public partial class BibleSearchView : UserControl
{
    public BibleSearchView()
    {
        InitializeComponent();
    }

    private void ListBoxItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem item && item.DataContext is ChurchAI.Core.Entities.Verse verse)
        {
            if (DataContext is ViewModels.BibleSearchViewModel vm)
            {
                vm.ProjectVerseCommand.Execute(verse);
            }
        }
    }

    private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem != null)
        {
            var selected = listBox.SelectedItem;
            
            listBox.SelectionChanged -= ListBox_SelectionChanged;
            listBox.SelectedItem = null;
            listBox.SelectionChanged += ListBox_SelectionChanged;
            
            if (DataContext is ViewModels.BibleSearchViewModel vm)
            {
                vm.SelectVerseCommand.Execute(selected);
            }
        }
    }

    private System.Windows.Point _startPoint;
    private object _draggedItem;

    private void ListBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(null);
        _draggedItem = null;

        if (sender is ListBox listBox)
        {
            var element = e.OriginalSource as DependencyObject;
            if (element != null)
            {
                var listBoxItem = FindAncestor<ListBoxItem>(element);
                if (listBoxItem != null)
                {
                    _draggedItem = listBoxItem.DataContext;
                }
            }
        }
    }

    private void ListBox_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && _draggedItem != null)
        {
            System.Windows.Point mousePos = e.GetPosition(null);
            System.Windows.Vector diff = _startPoint - mousePos;

            if (Math.Abs(diff.X) > System.Windows.SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > System.Windows.SystemParameters.MinimumVerticalDragDistance)
            {
                var dragData = new System.Windows.DataObject("ChurchAIItem", _draggedItem);
                System.Windows.DragDrop.DoDragDrop((DependencyObject)sender, dragData, System.Windows.DragDropEffects.Copy);
                _draggedItem = null;
            }
        }
    }

    private void ScrollUp_Click(object sender, RoutedEventArgs e)
    {
        var scrollViewer = GetScrollViewer(VersesListBox);
        scrollViewer?.LineUp();
    }

    private void ScrollDown_Click(object sender, RoutedEventArgs e)
    {
        var scrollViewer = GetScrollViewer(VersesListBox);
        scrollViewer?.LineDown();
    }

    private static ScrollViewer GetScrollViewer(DependencyObject depObj)
    {
        if (depObj is ScrollViewer sv) return sv;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            var child = VisualTreeHelper.GetChild(depObj, i);
            var result = GetScrollViewer(child);
            if (result != null) return result;
        }
        return null;
    }

    private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        do
        {
            if (current is T ancestor)
            {
                return ancestor;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        while (current != null);
        return null;
    }

    private static readonly string[] BibleBooks = new[]
    {
        "Genesis", "Exodus", "Leviticus", "Numbers", "Deuteronomy", "Joshua", "Judges", "Ruth",
        "1 Samuel", "2 Samuel", "1 Kings", "2 Kings", "1 Chronicles", "2 Chronicles", "Ezra", "Nehemiah",
        "Esther", "Job", "Psalms", "Proverbs", "Ecclesiastes", "Song of Solomon", "Isaiah", "Jeremiah",
        "Lamentations", "Ezekiel", "Daniel", "Hosea", "Joel", "Amos", "Obadiah", "Jonah", "Micah",
        "Nahum", "Habakkuk", "Zephaniah", "Haggai", "Zechariah", "Malachi",
        "Matthew", "Mark", "Luke", "John", "Acts", "Romans", "1 Corinthians", "2 Corinthians", "Galatians",
        "Ephesians", "Philippians", "Colossians", "1 Thessalonians", "2 Thessalonians", "1 Timothy",
        "2 Timothy", "Titus", "Philemon", "Hebrews", "James", "1 Peter", "2 Peter", "1 John", "2 John",
        "3 John", "Jude", "Revelation"
    };

    private bool _isUpdatingText = false;

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingText) return;

        if (sender is TextBox textBox)
        {
            var text = textBox.Text;
            if (string.IsNullOrEmpty(text)) return;

            // Only autocomplete if the user is typing at the end of the text
            var change = e.Changes.FirstOrDefault();
            if (change == null || change.AddedLength <= 0 || textBox.SelectionStart < text.Length)
            {
                return;
            }

            var match = BibleBooks.FirstOrDefault(b => b.StartsWith(text, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                _isUpdatingText = true;
                
                var selectionStart = text.Length;
                var selectionLength = match.Length - selectionStart;

                textBox.Text = match;
                textBox.SelectionStart = selectionStart;
                textBox.SelectionLength = selectionLength;

                _isUpdatingText = false;
            }
        }
    }

    private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is TextBox textBox && textBox.SelectionLength > 0)
        {
            if (e.Key == System.Windows.Input.Key.Space || 
                e.Key == System.Windows.Input.Key.Tab || 
                e.Key == System.Windows.Input.Key.Right ||
                e.Key == System.Windows.Input.Key.Enter ||
                (e.Key >= System.Windows.Input.Key.D0 && e.Key <= System.Windows.Input.Key.D9) ||
                (e.Key >= System.Windows.Input.Key.NumPad0 && e.Key <= System.Windows.Input.Key.NumPad9))
            {
                // Accept the completion
                textBox.SelectionStart = textBox.Text.Length;
                textBox.SelectionLength = 0;

                if (e.Key == System.Windows.Input.Key.Tab)
                {
                    e.Handled = true;
                }
            }
        }
    }
}
