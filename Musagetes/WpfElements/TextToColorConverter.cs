using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace Musagetes.WpfElements
{
    class TextToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, 
            object parameter, CultureInfo culture)
        {
            var name = value as string ?? value.ToString();
             return new SolidColorBrush(
                 ColorFromHsv(HashString(name) % 360, .70, .50)); 
        }

        public object ConvertBack(object value, Type targetType, 
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static int HashString(string input)
        {
            return 1 + input.Sum(c => ((int) c));
        }

        private static Color ColorFromHsv(double hue, double saturation, double value)
        {
            var hi = System.Convert.ToByte(Math.Floor(hue / 60)) % 6;
            var f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            var v = System.Convert.ToByte(value);
            var p = System.Convert.ToByte(value * (1 - saturation));
            var q = System.Convert.ToByte(value * (1 - f * saturation));
            var t = System.Convert.ToByte(value * (1 - (1 - f) * saturation));

            switch (hi)
            {
                case 0:
                    return Color.FromArgb(255, v, t, p);
                case 1:
                    return Color.FromArgb(255, q, v, p);
                case 2:
                    return Color.FromArgb(255, p, v, t);
                case 3:
                    return Color.FromArgb(255, p, q, v);
                case 4:
                    return Color.FromArgb(255, t, p, v);
                default:
                    return Color.FromArgb(255, v, p, q);
            }
        }
    }
}
