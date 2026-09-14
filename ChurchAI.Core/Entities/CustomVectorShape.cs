using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChurchAI.Core.Entities;

public class CustomVectorShape : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _id = Guid.NewGuid().ToString();
    private string _name = "Shape Layer";
    private ShapeType _shapeType = ShapeType.RoundedRectangle;
    private double _x = 100;
    private double _y = 800;
    private double _width = 600;
    private double _height = 120;
    private double _cornerRadius = 12;
    private string _fillHex = "#1E293B";
    private string _gradientEndHex = string.Empty;
    private string _strokeHex = "#3B82F6";
    private double _strokeThickness = 0;
    private double _opacity = 1.0;
    private double _skewX = 0;
    private int _zIndex = 1;

    public string Id { get => _id; set { _id = value; OnPropertyChanged(); } }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public ShapeType ShapeType { get => _shapeType; set { _shapeType = value; OnPropertyChanged(); } }
    public double X { get => _x; set { _x = value; OnPropertyChanged(); } }
    public double Y { get => _y; set { _y = value; OnPropertyChanged(); } }
    public double Width { get => _width; set { _width = value; OnPropertyChanged(); } }
    public double Height { get => _height; set { _height = value; OnPropertyChanged(); } }
    public double CornerRadius { get => _cornerRadius; set { _cornerRadius = value; OnPropertyChanged(); } }
    public string FillHex { get => _fillHex; set { _fillHex = value; OnPropertyChanged(); } }
    public string GradientEndHex { get => _gradientEndHex; set { _gradientEndHex = value; OnPropertyChanged(); } }
    public string StrokeHex { get => _strokeHex; set { _strokeHex = value; OnPropertyChanged(); } }
    public double StrokeThickness { get => _strokeThickness; set { _strokeThickness = value; OnPropertyChanged(); } }
    public double Opacity { get => _opacity; set { _opacity = value; OnPropertyChanged(); } }
    public double SkewX { get => _skewX; set { _skewX = value; OnPropertyChanged(); } }
    public int ZIndex { get => _zIndex; set { _zIndex = value; OnPropertyChanged(); } }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
