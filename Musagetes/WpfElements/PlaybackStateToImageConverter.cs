using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Musagetes.WpfElements
{
    class PlaybackStateToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is PlaybackControlBehavior.Playback)) return value;
            switch ((PlaybackControlBehavior.Playback)value)
            {
                case PlaybackControlBehavior.Playback.Play:
                    return Properties.Resources.PlayButton;
                case PlaybackControlBehavior.Playback.Pause:
                    return Properties.Resources.PauseButton;
                case PlaybackControlBehavior.Playback.Stop:
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
