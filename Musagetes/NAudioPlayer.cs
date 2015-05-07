using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Threading;
using Musagetes.DataObjects;
using NAudio.Wave;
using NLog;
using Musagetes.Annotations;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Musagetes
{
    class NAudioPlayer : INotifyPropertyChanged
    {
        private WaveOut _waveOutDevice;
        private AudioFileReader _audioFileReader;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private bool _inChannelSet;
        private double _channelLength;
        private double _channelPosition;
        private Song _song;
        private MediaState _playbackState = MediaState.Stop;
        private readonly DispatcherTimer _positionTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
        private bool _updatingTimer;
        private float _volume;

        public delegate void SongCompleted(object sender, EventArgs songCompletedEventArgs);
        public event SongCompleted SongCompletedEvent;

        public int DeviceNumber { get; set; }
        public bool IncrementsPlayCounter { get; set; }

        public float Volume
        {
            get { return _volume; }
            set
            {
                if (value < 0) value = 0;
                if (value > 1) value = 1;
                _volume = value;
                if (_waveOutDevice != null)
                    _waveOutDevice.Volume = _volume;
                OnPropertyChanged();
            }
        }

        public NAudioPlayer(int deviceNumber, bool incrementsPlayCounter)
        {
            DeviceNumber = deviceNumber;
            IncrementsPlayCounter = incrementsPlayCounter;

            _positionTimer.Interval = TimeSpan.FromMilliseconds(100);
            _positionTimer.Tick += positionTimer_Tick;
            _positionTimer.Start();
            _positionTimer.IsEnabled = false;
        }

        public string Length
        {
            get
            {
                //return string.Format("{0:00}:{1:00}", ChannelLength / 60, ChannelLength % 60);
                return TimeSpan.FromSeconds(ChannelLength).ToString(@"mm\:ss");
            }
        }

        public string Position
        {
            get
            {
                return TimeSpan.FromSeconds(ChannelPosition).ToString(@"mm\:ss");
                //return string.Format("{0:00}:{1:00}", ChannelPosition / 60, ChannelPosition % 60);
            }
        }

        public Song Song
        {
            get { return _song; }
            set
            {
                if (value == _song) return;
                PlaybackState = MediaState.Stop;
                UnloadAudio();
                _song = value;
                if (_song != null) LoadAudio();
                OnPropertyChanged();
            }
        }

        public MediaState PlaybackState
        {
            get { return _playbackState; }
            set
            {
                _playbackState = MediaState.Stop;
                switch (value)
                {
                    case MediaState.Play:
                        StartPlayback();
                        break;
                    case MediaState.Pause:
                        PausePlayback();
                        break;
                    case MediaState.Stop:
                        StopPlayback();
                        break;
                }

                OnPropertyChanged();
                OnPropertyChanged("IsPlaying");
            }
        }

        private void PausePlayback()
        {
            if (_waveOutDevice == null)
            {
                Logger.Debug(string.Format("Failed to pause {0}. "
                    + "WaveOut not initialized.", Song.SongTitle));
                return;
            }
            Logger.Debug(string.Format("Pausing playback of {0}",
                Song.SongTitle));
            _waveOutDevice.Pause();
            _playbackState = MediaState.Pause;
            _positionTimer.IsEnabled = false;
        }

        private void StopPlayback()
        {
            if (_waveOutDevice != null)
            {
                Logger.Debug(string.Format("Stopping playback of {0}",
                    Song.SongTitle));
                _waveOutDevice.Stop();
                _waveOutDevice.Dispose();
            }

            _waveOutDevice = null;
            _playbackState = MediaState.Stop;
            _positionTimer.IsEnabled = false;
            ChannelPosition = 0;
        }

        private void StartPlayback()
        {
            if (Song == null) return;

            if (_waveOutDevice != null)
            {
                Logger.Debug(string.Format("Resuming playback of {0}",
                    Song.SongTitle));
                _waveOutDevice.Volume = Volume;
                _waveOutDevice.Play();
                _playbackState = MediaState.Play;
                _positionTimer.IsEnabled = true;
                return;
            }

            Logger.Debug(string.Format("Starting playback of {0}",
                Song.SongTitle));
            _waveOutDevice = new WaveOut { DeviceNumber = DeviceNumber };

            try
            {
                if (!AudioRead) LoadAudio();
                _waveOutDevice.Init(_audioFileReader);
                _waveOutDevice.Volume = Volume;
                _waveOutDevice.Play();
                _waveOutDevice.PlaybackStopped += (sender, args) =>
                {
                    PlaybackState = MediaState.Stop;
                    if(SongCompletedEvent != null)
                        SongCompletedEvent(this, args);
                };
                _playbackState = MediaState.Play;
                if (IncrementsPlayCounter)
                    Song.PlayCount++;
                _positionTimer.IsEnabled = true;
            }
            catch (Exception e)
            {
                Logger.ErrorException(
                    string.Format("Unable to start playback of {0}", Song.SongTitle), e);
                _playbackState = MediaState.Stop;
                if (_audioFileReader != null)
                    _audioFileReader.Dispose();
                if (_waveOutDevice != null)
                    _waveOutDevice.Dispose();
                _audioFileReader = null;
                _waveOutDevice = null;
                _positionTimer.IsEnabled = false;
            }
        }

        private bool AudioRead { get { return _audioFileReader != null; } }
        private void LoadAudio()
        {
            _audioFileReader = new AudioFileReader(Song.Location);
            ChannelLength = _audioFileReader.TotalTime.TotalSeconds;
            ChannelPosition = 0;
        }

        private void UnloadAudio()
        {
            if (_audioFileReader != null)
                _audioFileReader.Dispose();
            _audioFileReader = null;
        }

        public bool IsPlaying
        {
            get { return _playbackState == MediaState.Play; }
        }

        public double ChannelPosition
        {
            get { return _channelPosition; }
            set
            {
                if (_inChannelSet) return;

                _inChannelSet = true;
                var oldVal = _channelPosition;
                var position = Math.Max(0, Math.Min(value, _channelLength));
                if (_audioFileReader != null && !_updatingTimer)
                {
                    _audioFileReader.Position =
                        (long)((position / _audioFileReader.TotalTime.TotalSeconds)
                        * _audioFileReader.Length);
                }
                _channelPosition = position;
                if (oldVal != _channelPosition)
                    OnPropertyChanged();
                _inChannelSet = false;
                OnPropertyChanged("Position");
            }
        }

        public double ChannelLength
        {
            get { return _channelLength; }
            protected set
            {
                if (_channelLength == value) return;
                _channelLength = value;
                OnPropertyChanged();
                OnPropertyChanged("Length");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        void positionTimer_Tick(object sender, EventArgs e)
        {
            if (_audioFileReader == null) return;
            _updatingTimer = true;
            ChannelPosition = (_audioFileReader.Position / (double)_audioFileReader.Length)
                * _audioFileReader.TotalTime.TotalSeconds;
            _updatingTimer = false;
        }
    }
}
