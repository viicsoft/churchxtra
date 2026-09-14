using System;
using System.Globalization;
using System.Windows.Data;

namespace ChurchAI.App.Converters;

public class AudioLevelToHeightConverter : IValueConverter
{
    public double BaseHeight { get; set; } = 10;
    public double Multiplier { get; set; } = 20;
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double level)
        {
            return BaseHeight + (level * Multiplier);
        }
        return BaseHeight;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
