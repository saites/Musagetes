using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NLog;

namespace Musagetes.WpfElements
{
    public class WpfMediaElement : MediaElement
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public WpfMediaElement()
        {
            MediaEnded += OnEnded;
            MediaFailed += OnFailed;
            MediaOpened += OnOpened;
        }

        private void OnOpened(object sender, RoutedEventArgs args)
        {
            PlaybackState = MediaState.Play;
        }

        private void OnFailed(object sender, ExceptionRoutedEventArgs args)
        {
            PlaybackState = MediaState.Stop;
        }

        private void OnEnded(object sender, RoutedEventArgs args)
        {
            PlaybackState = MediaState.Stop;
        }

        public static readonly DependencyProperty MillisecondsProperty =
            DependencyProperty.RegisterAttached("Milliseconds",
                typeof(int), typeof(WpfMediaElement),
                new UIPropertyMetadata(0, MillisecondsPropertyChanged));

        private static void MillisecondsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var media = d as MediaElement;
            if (media == null) return;
            if (!(e.NewValue is int)) return;
            var milliseconds = (int)e.NewValue;
            media.Position = new TimeSpan(0, 0, 0, 0, milliseconds);
        }

        public static void SetMilliseconds(DependencyObject d, int value)
        {
            d.SetValue(MillisecondsProperty, value);
        }

        public static int GetMilliseconds(DependencyObject d)
        {
            return (int)d.GetValue(MillisecondsProperty);
        }

        public static readonly DependencyProperty PlaybackStateProperty =
            DependencyProperty.Register("PlaybackState",
                typeof(MediaState),
                typeof(WpfMediaElement),
                new FrameworkPropertyMetadata
                    (MediaState.Play, 
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    PlaybackPropertyChanged));

        private static void PlaybackPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is MediaElement && e.NewValue is MediaState))
                return;
            var media = (MediaElement)d;
            var state = (MediaState)e.NewValue;

            Logger.Info("Switching {0} to {1}", media.Source, state);
            switch (state)
            {
                case MediaState.Play:
                    media.Play();
                    break;
                case MediaState.Pause:
                    media.Pause();
                    break;
                case MediaState.Stop:
                    media.Stop();
                    break;
            }
        }

        public MediaState PlaybackState
        {
            get { return (MediaState)GetValue(PlaybackStateProperty); }
            set { SetValue(PlaybackStateProperty, value); }
        }
    }
}
