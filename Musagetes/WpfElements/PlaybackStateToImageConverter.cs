using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Musagetes.WpfElements
{
    class PlaybackStateToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is MediaState)) return value;
            switch ((MediaState)value)
            {
                case MediaState.Play:
                    return Properties.Resources.PlayButton;
                case MediaState.Pause:
                    return Properties.Resources.PauseButton;
                case MediaState.Stop:
                    return Properties.Resources.StopButton;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
