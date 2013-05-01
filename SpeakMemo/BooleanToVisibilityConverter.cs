using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SpeakMemo
{
    // Normally true --> Visible but if parameter is set to "1",
    //  true --> Collapsed
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
                              object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)value;

            if (parameter != null && parameter is string && 
                parameter as string == "1")
            {
                boolValue ^= true;
            }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, CultureInfo culture)
        {
            if (parameter != null && parameter is string && 
                parameter as string == "1")
            {
                return (Visibility)value != Visibility.Visible;
            }

            return (Visibility)value == Visibility.Visible;
        }
    }
}
