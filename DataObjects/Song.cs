using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.IO;
using NLog;

namespace Musagetes.DataObjects
{
    public class Song : INotifyPropertyChanged
    {
        public static class SongId
        {
            private static uint _nextSongId;
            private static readonly object _songIdLock = new object();
            public static uint GetNextSongId()
            {
                lock (_songIdLock) _nextSongId++;
                return _nextSongId;
            }

            public static bool UpdateTagId(uint value)
            {
                lock (_songIdLock)
                {
                    if (_nextSongId > value)
                        return false;
                    _nextSongId = value + 1;
                    return true;
                }
            }
        }

        /* used to get around WPF's binding failure */
        public object Self { get { return this;  } }

        public string SongTitle
        {
            get { return _songTitle; }
            set
            {
                _songTitle = value;
                OnPropertyChanged("SongTitle");
            }
        }

        public int Milliseconds
        {
            get { return _milliseconds; }
            set
            {
                _milliseconds = value;
                OnPropertyChanged("Milliseconds");
                OnPropertyChanged("Length");
            }
        }

        public string Location
        {
            get { return _location; }
            set
            {
                _location = value;
                OnPropertyChanged("Location");
                OnPropertyChanged("Art");
            }
        }

        public Bpm Bpm
        {
            get { return _bpm; }
            set
            {
                _bpm = value;
                _bpm.PropertyChanged += (sender, args) =>
                {
                    OnPropertyChanged("Bpm");
                };
                OnPropertyChanged("Bpm");
            }
        }

        public uint PlayCount
        {
            get { return _playCount; }
            set
            {
                _playCount = value;
                OnPropertyChanged("PlayCount");
            }

        }

        public bool IsBadSong
        {
            get { return _isBadSong; }
            set
            {
                _isBadSong = value;
                OnPropertyChanged("IsBadSong");
            }
        }

        public string SongError
        {
            get { return _songError; }
            private set
            {
                _songError = value; 
                OnPropertyChanged("SongError");
            }
        }

        public uint Id { get; private set; }

        public override string ToString()
        {
            return SongTitle;
        }

        public Song(string title, string location, int milliseconds, Bpm bpm, 
            uint playCount, uint? songId = null)
        {
            SongTitle = title;
            Location = location;
            Milliseconds = milliseconds;
            Bpm = bpm;
            PlayCount = playCount;
            if (songId != null) SongId.UpdateTagId(songId.Value);
            Id = songId ?? SongId.GetNextSongId();
            IsBadSong = false;
            SongError = string.Empty;

            if (File.Exists(Location)) return;

            SongError = string.Format("Cannot find file {0}", Location);
            Logger.Error(SongError);
            IsBadSong = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string Length
        {
            get
            {
                return TimeSpan.FromMilliseconds(Milliseconds).ToString(Constants.TimeString);
            }
        }

        public void NotifyTagChanged()
        {
            OnPropertyChanged(null);
        }

        private bool _imageTried;
        private BitmapImage _cachedImage;
        private string _songTitle;
        private int _milliseconds;
        private string _location;
        private Bpm _bpm;
        private uint _playCount;
        private bool _isBadSong;
        private string _songError;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public BitmapImage Art
        {
            get
            {
                if (_imageTried) return _cachedImage;

                BitmapImage retval = null;
                if (!File.Exists(Location))
                {
                    Logger.Error("{0} does not exist -- unable to load art", Location);
                    _cachedImage = null;
                    _imageTried = true;
                    return null;
                }

                using (var file = TagLib.File.Create(Location))
                {
                    if (file.Tag.Pictures.Length > 0)
                    {
                        var albumArt = new BitmapImage();
                        albumArt.BeginInit();
                        albumArt.StreamSource = new MemoryStream(file.Tag.Pictures[0].Data.Data);
                        albumArt.EndInit();
                        retval = albumArt;
                    }
                }

                _cachedImage = retval;
                _imageTried = true;
                return retval;
            }
        }
    }
}
