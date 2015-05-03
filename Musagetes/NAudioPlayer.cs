using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Musagetes.DataObjects;
using NAudio.Wave;
using NLog;
using WPFSoundVisualizationLib;
using Musagetes.Annotations;

namespace Musagetes
{
    class NAudioPlayer : IWaveformPlayer
    {
        private WaveOut _waveOutDevice;
        private AudioFileReader _audioFileReader;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public int DeviceNumber { get; set; }
        public bool IncrementsPlayCounter { get; set; }
        public NAudioPlayer(int deviceNumber, bool incrementsPlayCounter)
        {
            DeviceNumber = deviceNumber;
            IncrementsPlayCounter = incrementsPlayCounter;

            _positionTimer.Interval = TimeSpan.FromMilliseconds(100);
            _positionTimer.Tick += positionTimer_Tick;

            //_waveformGenerateWorker.DoWork += waveformGenerateWorker_DoWork;
            //_waveformGenerateWorker.RunWorkerCompleted += waveformGenerateWorker_RunWorkerCompleted;
            //_waveformGenerateWorker.WorkerSupportsCancellation = true;
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
                if(_song != null) LoadAudio();
                OnPropertyChanged();
            }
        }

        private MediaState _playbackState = MediaState.Stop;
        private Song _song;
        private TimeSpan _selectionBegin;

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
        }

        private void StartPlayback()
        {
            if (Song == null) return;

            if (_waveOutDevice != null)
            {
                Logger.Debug(string.Format("Resuming playback of {0}", 
                    Song.SongTitle));
                _waveOutDevice.Play();
                _playbackState = MediaState.Play;
                return;
            }

            Logger.Debug(string.Format("Starting playback of {0}",
                Song.SongTitle));
            _waveOutDevice = new WaveOut { DeviceNumber = DeviceNumber };

            try
            {
                if(!AudioRead) LoadAudio();
                _waveOutDevice.Init(_audioFileReader);
                _waveOutDevice.Play();
                _playbackState = MediaState.Play;
                if (IncrementsPlayCounter)
                    Song.PlayCount++;
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
            }
        }

        private bool AudioRead { get { return _audioFileReader != null;  } }
        private void LoadAudio()
        {
            _audioFileReader = new AudioFileReader(Song.Location);
            ChannelLength = _audioFileReader.TotalTime.TotalSeconds;
            SelectionBegin = TimeSpan.Zero;
            SelectionEnd = TimeSpan.Zero;
            ChannelPosition = 0;
            GenerateWaveformData(Song.Location);
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
                if (_audioFileReader != null)
                {
                    lock (_channelLock)
                        _audioFileReader.Position =
                            (long)((position / _audioFileReader.TotalTime.TotalSeconds) * _audioFileReader.Length);
                }
                _channelPosition = position;
                if (oldVal != _channelPosition)
                    OnPropertyChanged();
                _inChannelSet = false;
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
            }
        }

        public float[] WaveformData
        {
            get { return _waveformData; }
            protected set
            {
                if (value == _waveformData) return;
                _waveformData = value;
                OnPropertyChanged();
            }
        }

        private bool _inRepeatSet;
        private bool _inChannelSet;
        private TimeSpan _selectionEnd;
        private float[] _waveformData;
        private double _channelLength;
        private double _channelPosition;

        public TimeSpan SelectionBegin
        {
            get { return _selectionBegin; }
            set
            {
                if (!_inRepeatSet) return;

                _inRepeatSet = true;
                var oldVal = _selectionBegin;
                _selectionBegin = value;
                if (_selectionBegin != oldVal)
                    OnPropertyChanged();
                _inRepeatSet = false;
            }
        }

        public TimeSpan SelectionEnd
        {
            get { return _selectionEnd; }
            set
            {
                if (_inRepeatSet) return;

                _inRepeatSet = true;
                var oldVal = _selectionEnd;
                _selectionEnd = value;
                if (_selectionBegin != oldVal)
                    OnPropertyChanged();
                _inRepeatSet = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Waveform Generation
        private readonly BackgroundWorker _waveformGenerateWorker = new BackgroundWorker();
        private const int FftDataSize = (int)FFTDataSize.FFT1024;
        private SampleAggregator _waveformAggregator;
        private string _pendingWaveformPath;
        private const int WaveformCompressedPointCount = 2000;
        private readonly DispatcherTimer _positionTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);

        private class WaveformGenerationParams
        {
            public WaveformGenerationParams(int points, string path)
            {
                Points = points;
                Path = path;
            }

            public int Points { get; protected set; }
            public string Path { get; protected set; }
        }

        private void GenerateWaveformData(string path)
        {
            if (_waveformGenerateWorker.IsBusy)
            {
                _pendingWaveformPath = path;
                _waveformGenerateWorker.CancelAsync();
                return;
            }

            if (!_waveformGenerateWorker.IsBusy)
                _waveformGenerateWorker.RunWorkerAsync(new WaveformGenerationParams(WaveformCompressedPointCount, path));
        }

        private void waveformGenerateWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                if (!_waveformGenerateWorker.IsBusy)
                    _waveformGenerateWorker.RunWorkerAsync(new WaveformGenerationParams(WaveformCompressedPointCount, _pendingWaveformPath));
            }
        }        

        private void waveformGenerateWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            WaveformGenerationParams waveformParams = e.Argument as WaveformGenerationParams;
            Mp3FileReader waveformMp3Stream = new Mp3FileReader(waveformParams.Path);
            WaveChannel32 waveformInputStream = new WaveChannel32(waveformMp3Stream);            
            waveformInputStream.Sample += waveStream_Sample;
            
            int frameLength = FftDataSize;
            int frameCount = (int)((double)waveformInputStream.Length / (double)frameLength);
            int waveformLength = frameCount * 2;
            byte[] readBuffer = new byte[frameLength];
            _waveformAggregator = new SampleAggregator(frameLength);
   
            float maxLeftPointLevel = float.MinValue;
            float maxRightPointLevel = float.MinValue;
            int currentPointIndex = 0;
            float[] waveformCompressedPoints = new float[waveformParams.Points];
            List<float> waveformData = new List<float>();
            List<int> waveMaxPointIndexes = new List<int>();
            
            for (int i = 1; i <= waveformParams.Points; i++)
            {
                waveMaxPointIndexes.Add((int)Math.Round(waveformLength * ((double)i / (double)waveformParams.Points), 0));
            }
            int readCount = 0;
            while (currentPointIndex * 2 < waveformParams.Points)
            {
                waveformInputStream.Read(readBuffer, 0, readBuffer.Length);

                waveformData.Add(_waveformAggregator.LeftMaxVolume);
                waveformData.Add(_waveformAggregator.RightMaxVolume);

                if (_waveformAggregator.LeftMaxVolume > maxLeftPointLevel)
                    maxLeftPointLevel = _waveformAggregator.LeftMaxVolume;
                if (_waveformAggregator.RightMaxVolume > maxRightPointLevel)
                    maxRightPointLevel = _waveformAggregator.RightMaxVolume;

                if (readCount > waveMaxPointIndexes[currentPointIndex])
                {
                    waveformCompressedPoints[(currentPointIndex * 2)] = maxLeftPointLevel;
                    waveformCompressedPoints[(currentPointIndex * 2) + 1] = maxRightPointLevel;
                    maxLeftPointLevel = float.MinValue;
                    maxRightPointLevel = float.MinValue;
                    currentPointIndex++;
                }
                if (readCount % 3000 == 0)
                {
                    float[] clonedData = (float[])waveformCompressedPoints.Clone();
                    App.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        WaveformData = clonedData;
                    }));
                }

                if (_waveformGenerateWorker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                readCount++;
            }

            float[] finalClonedData = (float[])waveformCompressedPoints.Clone();
            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                WaveformData = finalClonedData;
            }));
            waveformInputStream.Close();
            waveformInputStream.Dispose();
            waveformMp3Stream.Close();
            waveformMp3Stream.Dispose();
        }

        void waveStream_Sample(object sender, SampleEventArgs e)
        {
            _waveformAggregator.Add(e.Left, e.Right);
        }  

        #endregion

        private readonly object _channelLock = new object();
        void positionTimer_Tick(object sender, EventArgs e)
        {
            lock (_channelLock)
                ChannelPosition = (_audioFileReader.Position / (double)_audioFileReader.Length)
                    * _audioFileReader.TotalTime.TotalSeconds;
        }
    }
}
