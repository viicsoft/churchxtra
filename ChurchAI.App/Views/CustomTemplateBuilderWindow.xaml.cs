using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ChurchAI.App.ViewModels;
using ChurchAI.Core.Entities;

namespace ChurchAI.App.Views;

public partial class CustomTemplateBuilderWindow : Window
{
    private CustomTemplateBuilderViewModel? ViewModel => DataContext as CustomTemplateBuilderViewModel;

    private bool _isDraggingStudioItem;
    private Point _studioDragStartMousePoint;
    private double _studioDragStartItemX;
    private double _studioDragStartItemY;
    private object? _activeStudioDragItem;

    public CustomTemplateBuilderWindow()
    {
        InitializeComponent();
        DataContextChanged += CustomTemplateBuilderWindow_DataContextChanged;
    }

    private void CustomTemplateBuilderWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is CustomTemplateBuilderViewModel oldVm)
        {
            oldVm.Shapes.CollectionChanged -= Shapes_CollectionChanged;
            oldVm.TextPlaceholders.CollectionChanged -= TextPlaceholders_CollectionChanged;
            oldVm.PropertyChanged -= ViewModel_PropertyChanged;
        }

        if (e.NewValue is CustomTemplateBuilderViewModel newVm)
        {
            newVm.Shapes.CollectionChanged += Shapes_CollectionChanged;
            newVm.TextPlaceholders.CollectionChanged += TextPlaceholders_CollectionChanged;
            newVm.PropertyChanged += ViewModel_PropertyChanged;
            newVm.RequestClose = Close;
            SubscribeShapeEvents();
            SubscribeTextEvents();
            RenderCanvas();
        }
    }

    private void Shapes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SubscribeShapeEvents();
        if (!_isDraggingStudioItem) RenderCanvas();
    }

    private void TextPlaceholders_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SubscribeTextEvents();
        if (!_isDraggingStudioItem) RenderCanvas();
    }

    private void SubscribeShapeEvents()
    {
        if (ViewModel == null) return;
        foreach (var s in ViewModel.Shapes)
        {
            s.PropertyChanged -= Shape_PropertyChanged;
            s.PropertyChanged += Shape_PropertyChanged;
        }
    }

    private void SubscribeTextEvents()
    {
        if (ViewModel == null) return;
        foreach (var t in ViewModel.TextPlaceholders)
        {
            t.PropertyChanged -= Text_PropertyChanged;
            t.PropertyChanged += Text_PropertyChanged;
        }
    }

    private void Shape_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_isDraggingStudioItem)
        {
            RenderCanvas();
        }
    }

    private void Text_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_isDraggingStudioItem)
        {
            RenderCanvas();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_isDraggingStudioItem && (
            e.PropertyName == nameof(CustomTemplateBuilderViewModel.Category) ||
            e.PropertyName == nameof(CustomTemplateBuilderViewModel.SelectedShape) ||
            e.PropertyName == nameof(CustomTemplateBuilderViewModel.SelectedTextPlaceholder)))
        {
            RenderCanvas();
        }
    }

    private void RenderCanvas()
    {
        if (BuilderCanvas == null || ViewModel == null) return;

        BuilderCanvas.Children.Clear();

        var titleBlock = new TextBlock
        {
            Text = "1080p Broadcast Stage Canvas (Click and drag elements to move anywhere)",
            Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            FontSize = 36,
            FontWeight = FontWeights.Bold
        };
        Canvas.SetLeft(titleBlock, 50);
        Canvas.SetTop(titleBlock, 50);
        BuilderCanvas.Children.Add(titleBlock);

        // Render Shapes
        foreach (var shape in ViewModel.Shapes)
        {
            FrameworkElement elem;
            var fillBrush = ParseColor(shape.FillHex, Color.FromRgb(15, 23, 42));
            var strokeBrush = ParseColor(shape.StrokeHex, Colors.Transparent);

            bool isSelected = ViewModel.SelectedShape == shape;
            if (isSelected)
            {
                strokeBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)); // Neon cyan border selection highlight
            }

            if (shape.ShapeType == ShapeType.Ellipse)
            {
                elem = new Ellipse
                {
                    Width = Math.Max(10, shape.Width),
                    Height = Math.Max(10, shape.Height),
                    Fill = fillBrush,
                    Stroke = strokeBrush,
                    StrokeThickness = isSelected ? Math.Max(3, shape.StrokeThickness) : shape.StrokeThickness,
                    Opacity = shape.Opacity
                };
            }
            else
            {
                var border = new Border
                {
                    Width = Math.Max(10, shape.Width),
                    Height = Math.Max(10, shape.Height),
                    Background = fillBrush,
                    BorderBrush = strokeBrush,
                    BorderThickness = new Thickness(isSelected ? Math.Max(3, shape.StrokeThickness) : shape.StrokeThickness),
                    CornerRadius = shape.ShapeType == ShapeType.PillCapsule ? new CornerRadius(shape.Height / 2) : new CornerRadius(shape.CornerRadius),
                    Opacity = shape.Opacity
                };

                if (Math.Abs(shape.SkewX) > 0.1)
                {
                    border.LayoutTransform = new SkewTransform(shape.SkewX, 0);
                }
                elem = border;
            }

            AttachShapeDragHandlers(elem, shape);

            Canvas.SetLeft(elem, shape.X);
            Canvas.SetTop(elem, shape.Y);
            Panel.SetZIndex(elem, shape.ZIndex);
            BuilderCanvas.Children.Add(elem);
        }

        // Render Text Placeholders
        foreach (var txt in ViewModel.TextPlaceholders)
        {
            string sampleText = txt.BindingField switch
            {
                TextBindingField.TagText => "SPEAKER BADGE",
                TextBindingField.Title => "Pastor John Doe",
                TextBindingField.Subtitle => "Lead Pastor, ChurchXtra",
                TextBindingField.Reference => "JOHN 3:16",
                TextBindingField.VerseText => "For God so loved the world that He gave His only Son...",
                _ => "Sample Text Placeholder"
            };

            bool isSelected = ViewModel.SelectedTextPlaceholder == txt;

            var tb = new TextBlock
            {
                Text = sampleText,
                Width = Math.Max(10, txt.Width),
                Height = Math.Max(10, txt.Height),
                FontSize = txt.FontSize,
                FontFamily = new FontFamily(string.IsNullOrWhiteSpace(txt.FontFamily) ? "Segoe UI" : txt.FontFamily),
                FontWeight = txt.FontWeight.Equals("Bold", StringComparison.OrdinalIgnoreCase) ? FontWeights.Bold : FontWeights.Normal,
                Foreground = ParseColor(txt.ForegroundHex, Colors.White),
                TextWrapping = TextWrapping.Wrap
            };

            if (txt.TextAlignment.Equals("Center", StringComparison.OrdinalIgnoreCase)) tb.TextAlignment = TextAlignment.Center;
            else if (txt.TextAlignment.Equals("Right", StringComparison.OrdinalIgnoreCase)) tb.TextAlignment = TextAlignment.Right;
            else tb.TextAlignment = TextAlignment.Left;

            if (Math.Abs(txt.SkewX) > 0.1)
            {
                tb.LayoutTransform = new SkewTransform(txt.SkewX, 0);
            }

            var container = new Border
            {
                Child = tb,
                BorderBrush = isSelected ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) : Brushes.Transparent,
                BorderThickness = new Thickness(isSelected ? 2 : 0),
                Padding = new Thickness(2)
            };

            AttachTextDragHandlers(container, txt);

            Canvas.SetLeft(container, txt.X);
            Canvas.SetTop(container, txt.Y);
            Panel.SetZIndex(container, txt.ZIndex);
            BuilderCanvas.Children.Add(container);
        }
    }

    private void AttachShapeDragHandlers(FrameworkElement elem, CustomVectorShape shape)
    {
        elem.Cursor = Cursors.SizeAll;
        elem.PreviewMouseLeftButtonDown += (s, e) =>
        {
            if (ViewModel != null)
            {
                ViewModel.SelectedShape = shape;
                ViewModel.SelectedTextPlaceholder = null;
            }
            _isDraggingStudioItem = true;
            _activeStudioDragItem = shape;
            _studioDragStartMousePoint = e.GetPosition(BuilderCanvas);
            _studioDragStartItemX = shape.X;
            _studioDragStartItemY = shape.Y;
            elem.CaptureMouse();
            e.Handled = true;
        };

        elem.PreviewMouseMove += (s, e) =>
        {
            if (_isDraggingStudioItem && _activeStudioDragItem == shape && elem.IsMouseCaptured)
            {
                var curPoint = e.GetPosition(BuilderCanvas);
                double deltaX = curPoint.X - _studioDragStartMousePoint.X;
                double deltaY = curPoint.Y - _studioDragStartMousePoint.Y;
                shape.X = Math.Max(0, _studioDragStartItemX + deltaX);
                shape.Y = Math.Max(0, _studioDragStartItemY + deltaY);

                // Update WPF Element Position directly without tearing down canvas
                Canvas.SetLeft(elem, shape.X);
                Canvas.SetTop(elem, shape.Y);
                e.Handled = true;
            }
        };

        elem.PreviewMouseLeftButtonUp += (s, e) =>
        {
            if (_isDraggingStudioItem && _activeStudioDragItem == shape)
            {
                _isDraggingStudioItem = false;
                _activeStudioDragItem = null;
                elem.ReleaseMouseCapture();
                RenderCanvas();
                e.Handled = true;
            }
        };
    }

    private void AttachTextDragHandlers(FrameworkElement elem, CustomTextPlaceholder txt)
    {
        elem.Cursor = Cursors.SizeAll;
        elem.PreviewMouseLeftButtonDown += (s, e) =>
        {
            if (ViewModel != null)
            {
                ViewModel.SelectedTextPlaceholder = txt;
                ViewModel.SelectedShape = null;
            }
            _isDraggingStudioItem = true;
            _activeStudioDragItem = txt;
            _studioDragStartMousePoint = e.GetPosition(BuilderCanvas);
            _studioDragStartItemX = txt.X;
            _studioDragStartItemY = txt.Y;
            elem.CaptureMouse();
            e.Handled = true;
        };

        elem.PreviewMouseMove += (s, e) =>
        {
            if (_isDraggingStudioItem && _activeStudioDragItem == txt && elem.IsMouseCaptured)
            {
                var curPoint = e.GetPosition(BuilderCanvas);
                double deltaX = curPoint.X - _studioDragStartMousePoint.X;
                double deltaY = curPoint.Y - _studioDragStartMousePoint.Y;
                txt.X = Math.Max(0, _studioDragStartItemX + deltaX);
                txt.Y = Math.Max(0, _studioDragStartItemY + deltaY);

                // Update WPF Element Position directly without tearing down canvas
                Canvas.SetLeft(elem, txt.X);
                Canvas.SetTop(elem, txt.Y);
                e.Handled = true;
            }
        };

        elem.PreviewMouseLeftButtonUp += (s, e) =>
        {
            if (_isDraggingStudioItem && _activeStudioDragItem == txt)
            {
                _isDraggingStudioItem = false;
                _activeStudioDragItem = null;
                elem.ReleaseMouseCapture();
                RenderCanvas();
                e.Handled = true;
            }
        };
    }

    private static SolidColorBrush ParseColor(string hex, Color fallback)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(hex))
            {
                var converted = ColorConverter.ConvertFromString(hex);
                if (converted is Color c) return new SolidColorBrush(c);
            }
        }
        catch { }
        return new SolidColorBrush(fallback);
    }
}
