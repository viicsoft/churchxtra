using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChurchAI.Core.Entities;

public class CustomTextPlaceholder : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _id = Guid.NewGuid().ToString();
    private string _name = "Text Element";
    private TextBindingField _bindingField = TextBindingField.Title;
    private double _x = 120;
    private double _y = 820;
    private double _width = 560;
    private double _height = 50;
    private string _fontFamily = "Segoe UI";
    private double _fontSize = 28;
    private string _fontWeight = "Bold";
    private string _foregroundHex = "#FFFFFF";
    private string _textAlignment = "Left";
    private double _skewX = 0;
    private int _zIndex = 10;

    public string Id { get => _id; set { _id = value; OnPropertyChanged(); } }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public TextBindingField BindingField { get => _bindingField; set { _bindingField = value; OnPropertyChanged(); } }
    public double X { get => _x; set { _x = value; OnPropertyChanged(); } }
    public double Y { get => _y; set { _y = value; OnPropertyChanged(); } }
    public double Width { get => _width; set { _width = value; OnPropertyChanged(); } }
    public double Height { get => _height; set { _height = value; OnPropertyChanged(); } }
    public string FontFamily { get => _fontFamily; set { _fontFamily = value; OnPropertyChanged(); } }
    public double FontSize { get => _fontSize; set { _fontSize = value; OnPropertyChanged(); } }
    public string FontWeight { get => _fontWeight; set { _fontWeight = value; OnPropertyChanged(); } }
    public string ForegroundHex { get => _foregroundHex; set { _foregroundHex = value; OnPropertyChanged(); } }
    public string TextAlignment { get => _textAlignment; set { _textAlignment = value; OnPropertyChanged(); } }
    public double SkewX { get => _skewX; set { _skewX = value; OnPropertyChanged(); } }
    public int ZIndex { get => _zIndex; set { _zIndex = value; OnPropertyChanged(); } }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
