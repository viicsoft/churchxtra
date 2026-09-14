using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChurchAI.App.ViewModels;

namespace ChurchAI.App.Views;

public partial class ProjectionView : UserControl
{
    public ProjectionView()
    {
        InitializeComponent();
        Loaded += ProjectionView_Loaded;
        DataContextChanged += ProjectionView_DataContextChanged;
    }

    private void ProjectionView_Loaded(object sender, RoutedEventArgs e)
    {
        AttachLiveMonitorVisualBrush();
    }

    private void ProjectionView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        AttachLiveMonitorVisualBrush();
    }

    private void AttachLiveMonitorVisualBrush()
    {
        if (DataContext is ViewModels.ProjectionViewModel vm)
        {
            var projectionService = vm.ProjectionService;
            if (projectionService != null)
            {
                projectionService.ProjectionWindowChanged -= OnProjectionWindowChanged;
                projectionService.ProjectionWindowChanged += OnProjectionWindowChanged;

                projectionService.CurrentlyProjectedItemChanged -= OnCurrentlyProjectedItemChanged;
                projectionService.CurrentlyProjectedItemChanged += OnCurrentlyProjectedItemChanged;

                UpdateVisualBrush(projectionService);
            }
        }
    }

    private void OnCurrentlyProjectedItemChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is ViewModels.ProjectionViewModel vm && vm.ProjectionService != null)
        {
            Dispatcher.Invoke(() => UpdateVisualBrush(vm.ProjectionService));
        }
    }

    private void OnProjectionWindowChanged(object? sender, System.EventArgs e)
    {
        if (sender is Services.Interfaces.IProjectionService service)
        {
            Dispatcher.Invoke(() => UpdateVisualBrush(service));
        }
    }

    private void UpdateVisualBrush(Services.Interfaces.IProjectionService service)
    {
        if (service.CurrentProjectionWindow is ProjectionWindow win)
        {
            var mainGrid = win.GetMainGrid();
            LiveMonitorVisualBrush.Visual = mainGrid;
            if (FullViewVisualBrush != null)
            {
                FullViewVisualBrush.Visual = mainGrid;
            }
        }
        else
        {
            LiveMonitorVisualBrush.Visual = null;
            if (FullViewVisualBrush != null)
            {
                FullViewVisualBrush.Visual = null;
            }
        }
    }

    private ProjectionWindow? GetProjectionWindow()
    {
        if (DataContext is ViewModels.ProjectionViewModel vm)
        {
            return vm.ProjectionService?.CurrentProjectionWindow as ProjectionWindow;
        }
        return null;
    }

    private void FullViewLtScaleXSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        var win = GetProjectionWindow();
        if (win != null)
        {
            win.SetLtScaleXValue(e.NewValue);
        }
    }

    private void FullViewLtScaleYSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        var win = GetProjectionWindow();
        if (win != null)
        {
            win.SetLtScaleYValue(e.NewValue);
        }
    }

    private void FullViewLtLockAspectCheck_Click(object sender, RoutedEventArgs e)
    {
        var win = GetProjectionWindow();
        if (win != null && FullViewLtLockAspectCheck != null)
        {
            win.SetLtLockAspect(FullViewLtLockAspectCheck.IsChecked == true);
        }
    }

    private void FullViewCommitLtPosBtn_Click(object sender, RoutedEventArgs e)
    {
        var win = GetProjectionWindow();
        if (win != null)
        {
            win.TriggerCommitLtPos();
            if (FullViewCommitLtPosBtn != null)
            {
                FullViewCommitLtPosBtn.Content = "✓ Position Set!";
            }
        }
    }

    private bool _isDraggingLtOnLiveCard;
    private Point _dragStartMousePointOnCard;
    private double _dragStartLtX;
    private double _dragStartLtY;

    private FrameworkElement? _activeDragViewport;

    private void LiveCardViewport_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var win = GetProjectionWindow();
        if (win != null && win.IsLowerThirdsActive() && sender is FrameworkElement element)
        {
            _isDraggingLtOnLiveCard = true;
            _activeDragViewport = element;
            _dragStartMousePointOnCard = e.GetPosition(element);
            _dragStartLtX = win.GetLtTranslateX();
            _dragStartLtY = win.GetLtTranslateY();
            element.CaptureMouse();
            e.Handled = true;
        }
    }

    private void LiveCardViewport_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isDraggingLtOnLiveCard && _activeDragViewport != null)
        {
            var win = GetProjectionWindow();
            if (win != null)
            {
                var currentMousePoint = e.GetPosition(_activeDragViewport);
                double scaleX = 1920.0 / Math.Max(1.0, _activeDragViewport.ActualWidth);
                double scaleY = 1080.0 / Math.Max(1.0, _activeDragViewport.ActualHeight);

                double deltaX = (currentMousePoint.X - _dragStartMousePointOnCard.X) * scaleX;
                double deltaY = (currentMousePoint.Y - _dragStartMousePointOnCard.Y) * scaleY;

                win.SetLtTranslate(
                    _dragStartLtX + deltaX,
                    _dragStartLtY + deltaY
                );

                if (FullViewCommitLtPosBtn != null)
                {
                    FullViewCommitLtPosBtn.Visibility = Visibility.Visible;
                    FullViewCommitLtPosBtn.Content = "✓ Set Position";
                    FullViewCommitLtPosBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                }
            }
            e.Handled = true;
        }
    }

    private void LiveCardViewport_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_isDraggingLtOnLiveCard)
        {
            _isDraggingLtOnLiveCard = false;
            _activeDragViewport?.ReleaseMouseCapture();
            _activeDragViewport = null;
            e.Handled = true;
        }
    }

    private void ScrollScripturesUp_Click(object sender, RoutedEventArgs e)
    {
        var scrollViewer = FindScrollViewer(ScripturesListBox);
        scrollViewer?.LineUp();
    }

    private void ScrollScripturesDown_Click(object sender, RoutedEventArgs e)
    {
        var scrollViewer = FindScrollViewer(ScripturesListBox);
        scrollViewer?.LineDown();
    }

    private void ScrollHymnsUp_Click(object sender, RoutedEventArgs e)
    {
        var scrollViewer = FindScrollViewer(HymnsListBox);
        scrollViewer?.LineUp();
    }

    private void ScrollHymnsDown_Click(object sender, RoutedEventArgs e)
    {
        var scrollViewer = FindScrollViewer(HymnsListBox);
        scrollViewer?.LineDown();
    }

    private ScrollViewer FindScrollViewer(DependencyObject parent)
    {
        if (parent == null) return null;
        if (parent is ScrollViewer sv) return sv;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            var result = FindScrollViewer(child);
            if (result != null) return result;
        }
        return null;
    }

    private bool _isDrawerOpen = false;
    private int _currentDrawerTabIndex = -1;

    private void UpdateNavButtonSelectionStyles()
    {
        var grayBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94A3B8"));
        var cyanBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00E5FF"));

        if (BtnNavSongs == null || RingNavSongs == null) return;

        BtnNavSongs.BorderBrush = (_isDrawerOpen && _currentDrawerTabIndex == 0) ? cyanBrush : grayBrush;
        RingNavSongs.BorderBrush = (_isDrawerOpen && _currentDrawerTabIndex == 0) ? cyanBrush : grayBrush;

        BtnNavLowerThirds.BorderBrush = (_isDrawerOpen && _currentDrawerTabIndex == 1) ? cyanBrush : grayBrush;
        RingNavLowerThirds.BorderBrush = (_isDrawerOpen && _currentDrawerTabIndex == 1) ? cyanBrush : grayBrush;

        BtnNavMedia.BorderBrush = (_isDrawerOpen && _currentDrawerTabIndex == 2) ? cyanBrush : grayBrush;
        RingNavMedia.BorderBrush = (_isDrawerOpen && _currentDrawerTabIndex == 2) ? cyanBrush : grayBrush;

        BtnNavSettings.BorderBrush = (_isDrawerOpen && _currentDrawerTabIndex == 3) ? cyanBrush : grayBrush;
        RingNavSettings.BorderBrush = (_isDrawerOpen && _currentDrawerTabIndex == 3) ? cyanBrush : grayBrush;
    }

    private void RecentSearchesButton_Click(object sender, RoutedEventArgs e)
    {
        if (RecentSearchesPopup != null)
        {
            RecentSearchesPopup.IsOpen = !RecentSearchesPopup.IsOpen;
        }
    }

    private void RecentSearchItem_Click(object sender, RoutedEventArgs e)
    {
        if (RecentSearchesPopup != null)
        {
            RecentSearchesPopup.IsOpen = false;
        }
        if (DataContext is ProjectionViewModel vm)
        {
            vm.SelectedLeftCardSubTabIndex = 1; // Switch to Verses tab
            vm.SelectedVersesInnerSubTabIndex = 0; // Switch to Search sub-tab
            vm.BibleSearch.SearchCommand.Execute(null);
        }
    }

    private void RecentSearchesScrollUp_Click(object sender, RoutedEventArgs e)
    {
        RecentSearchesScrollViewer?.LineUp();
    }

    private void RecentSearchesScrollDown_Click(object sender, RoutedEventArgs e)
    {
        RecentSearchesScrollViewer?.LineDown();
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tagStr)
        {
            var parts = tagStr.Split('|');
            if (parts.Length == 2 && int.TryParse(parts[0], out int tabIndex))
            {
                string title = parts[1];

                if (_isDrawerOpen && _currentDrawerTabIndex == tabIndex)
                {
                    // Clicked same active tab -> Collapse drawer
                    SlideDownDrawer();
                }
                else
                {
                    // Switch tab & slide UP
                    ResourcesTabControl.SelectedIndex = tabIndex;
                    DrawerTitleTextBlock.Text = title;
                    _currentDrawerTabIndex = tabIndex;

                    if (!_isDrawerOpen)
                    {
                        SlideUpDrawer();
                    }
                    else
                    {
                        UpdateNavButtonSelectionStyles();
                    }
                }
            }
        }
    }

    private void CollapseDrawer_Click(object sender, RoutedEventArgs e)
    {
        SlideDownDrawer();
    }

    private void SlideUpDrawer()
    {
        _isDrawerOpen = true;
        UpdateNavButtonSelectionStyles();
        SlideUpDrawerGrid.Visibility = Visibility.Visible;
        var anim = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 700,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
        };
        DrawerTranslateTransform.BeginAnimation(TranslateTransform.YProperty, anim);
    }

    private void SlideDownDrawer()
    {
        _isDrawerOpen = false;
        _currentDrawerTabIndex = -1;
        UpdateNavButtonSelectionStyles();
        var anim = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0,
            To = 700,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
        };
        anim.Completed += (s, e) =>
        {
            if (!_isDrawerOpen)
            {
                SlideUpDrawerGrid.Visibility = Visibility.Collapsed;
            }
        };
        DrawerTranslateTransform.BeginAnimation(TranslateTransform.YProperty, anim);
    }

    private void ScrollDetailedUp_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ProjectionViewModel vm && vm.DetailedContentItems.Count > 0 && vm.CurrentlyProjectedItem != null)
        {
            vm.MoveLiveProjectionUpCommand.Execute(null);
        }
        else
        {
            var scrollViewer = FindScrollViewer(DetailedListBox);
            scrollViewer?.LineUp();
        }
    }

    private void ScrollDetailedDown_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ProjectionViewModel vm && vm.DetailedContentItems.Count > 0 && vm.CurrentlyProjectedItem != null)
        {
            vm.MoveLiveProjectionDownCommand.Execute(null);
        }
        else
        {
            var scrollViewer = FindScrollViewer(DetailedListBox);
            scrollViewer?.LineDown();
        }
    }

    private void ScheduleBorder_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("ChurchAIItem"))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void ScheduleBorder_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("ChurchAIItem"))
        {
            var item = e.Data.GetData("ChurchAIItem");
            if (item != null && DataContext is ViewModels.ProjectionViewModel vm)
            {
                vm.AddDroppedItemCommand.Execute(item);
            }
        }
        e.Handled = true;
    }

    private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (DataContext is ViewModels.ProjectionViewModel vm && vm.IsFullViewMode)
        {
            if (e.Key == System.Windows.Input.Key.C)
            {
                vm.ToggleFullViewControlsCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private double _leftCardDragDistance = 0;
    private void LeftCardDragThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {
        if (DataContext is ProjectionViewModel vm)
        {
            _leftCardDragDistance += e.VerticalChange;
            if (_leftCardDragDistance > 5 && !vm.IsLeftCardExpanded)
            {
                vm.ToggleLeftCardExpandCommand.Execute(null);
                _leftCardDragDistance = 0;
            }
            else if (_leftCardDragDistance < -5 && vm.IsLeftCardExpanded)
            {
                vm.ToggleLeftCardExpandCommand.Execute(null);
                _leftCardDragDistance = 0;
            }
        }
    }

    private double _centerCardDragDistance = 0;
    private void CenterCardDragThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {
        if (DataContext is ProjectionViewModel vm)
        {
            _centerCardDragDistance += e.VerticalChange;
            if (_centerCardDragDistance > 5 && !vm.IsCenterCardExpanded)
            {
                vm.ToggleCenterCardExpandCommand.Execute(null);
                _centerCardDragDistance = 0;
            }
            else if (_centerCardDragDistance < -5 && vm.IsCenterCardExpanded)
            {
                vm.ToggleCenterCardExpandCommand.Execute(null);
                _centerCardDragDistance = 0;
            }
        }
    }

    private double _rightCardDragDistance = 0;
    private void RightCardDragThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {
        if (DataContext is ProjectionViewModel vm)
        {
            _rightCardDragDistance += e.VerticalChange;
            if (_rightCardDragDistance > 5 && !vm.IsRightCardExpanded)
            {
                vm.ToggleRightCardExpandCommand.Execute(null);
                _rightCardDragDistance = 0;
            }
            else if (_rightCardDragDistance < -5 && vm.IsRightCardExpanded)
            {
                vm.ToggleRightCardExpandCommand.Execute(null);
                _rightCardDragDistance = 0;
            }
        }
    }
}
