using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Musagetes
{
    public class VisibilityToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Visibility)) return value;
            return (Visibility) value == Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool)) return value;
            return (bool) value ? Visibility.Visible : Visibility.Hidden;
        }
    }
}