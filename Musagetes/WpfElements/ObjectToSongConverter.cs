using System;
using System.Globalization;
using System.Windows.Data;
using Musagetes.DataObjects;

namespace Musagetes.WpfElements
{
    class ObjectToSongConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value as Song;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
