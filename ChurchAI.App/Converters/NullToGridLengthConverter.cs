using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ChurchAI.App.Converters;

public class NullToGridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        if (parameter != null && parameter.ToString() == "QueueHeight")
        {
            // If active verse is null, Queue Playlist takes all remaining space (3*).
            // Otherwise, it takes 2* so they share the height proportionally.
            return isNull ? new GridLength(3, GridUnitType.Star) : new GridLength(2, GridUnitType.Star);
        }

        // For Active Verse Preview: if null, height is 0 (collapsed).
        // Otherwise, it takes 3* to give it more space than the playlist.
        return isNull ? new GridLength(0) : new GridLength(3, GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
