using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChurchAI.App.Views;

public partial class HymnSearchView : UserControl
{
    public HymnSearchView()
    {
        InitializeComponent();
    }

    private void ListBoxItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem item && item.DataContext is ChurchAI.Core.Entities.Hymn hymn)
        {
            if (DataContext is ViewModels.HymnSearchViewModel vm)
            {
                vm.ProjectHymnCommand.Execute(hymn);
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
            
            if (DataContext is ViewModels.HymnSearchViewModel vm)
            {
                vm.SelectHymnCommand.Execute(selected);
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
        var scrollViewer = GetScrollViewer(HymnsListBox);
        scrollViewer?.LineUp();
    }

    private void ScrollDown_Click(object sender, RoutedEventArgs e)
    {
        var scrollViewer = GetScrollViewer(HymnsListBox);
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
}
