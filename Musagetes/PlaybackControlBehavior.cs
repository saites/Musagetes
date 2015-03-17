using System;
using System.Windows;
using System.Windows.Controls;

namespace Musagetes
{
    public class PlaybackControlBehavior
    {
        public enum Playback
        {
            Play,
            Pause,
            Stop
        }

        public static readonly DependencyProperty PlaybackStateProperty =
            DependencyProperty.RegisterAttached("PlaybackState",
                typeof(Playback),
                typeof(PlaybackControlBehavior),
                new UIPropertyMetadata(Playback.Stop, PlaybackPropertyChanged));

        private static void PlaybackPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var media = d as MediaElement;
            if (!(e.NewValue is Playback)) return;
            if (media == null) return;
            var state = (Playback)e.NewValue;

            Console.WriteLine(media.Source);
            switch (state)
            {
                case Playback.Play:
                    media.Play();
                    break;
                case Playback.Pause:
                    media.Pause();
                    break;
                case Playback.Stop:
                    media.Stop();
                    break;
            }
        }

        public static void SetPlaybackState(DependencyObject d, Playback value)
        {
            d.SetValue(PlaybackStateProperty, value);
        }

        public static Playback GetPlaybackState(DependencyObject d)
        {
            return (Playback)d.GetValue(PlaybackStateProperty);
        }
    }
}
