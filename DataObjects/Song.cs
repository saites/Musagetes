using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.IO;
using System.Linq;
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
                    if (_nextSongId >= value)
                        return false;
                    _nextSongId = value + 1;
                    return true;
                }
            }
        }

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

        public CategoryTag CategoryTags
        {
            get { return _categoryTags; }
            set
            {
                _categoryTags = value;
                OnPropertyChanged("CategoryTags");
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

        public SongDb SongDb { get; private set; }
        public uint Id { get; private set; }

        public IEnumerable<Tag> Tags
        {
            get { return _categoryToTag.Values.SelectMany(tagSet => tagSet); }
        }

        public override string ToString()
        {
            return SongTitle;
        }

        private readonly HashSet<Tag> _tags = new HashSet<Tag>();
        private readonly Dictionary<Category, HashSet<Tag>> _categoryToTag
            = new Dictionary<Category, HashSet<Tag>>();

        public class CategoryTag : INotifyPropertyChanged
        {
            private readonly Song _song;

            public CategoryTag(Song song)
            {
                _song = song;
            }

            public string this[Category cat]
            {
                get
                {
                    lock (_song._categoryToTag)
                    {
                        return _song._categoryToTag.ContainsKey(cat)
                            ? string.Join(", ",
                                _song._categoryToTag[cat].Select(t => t.TagName))
                            : null;
                    }
                }
            }

            public string this[string s]
            {
                get
                {
                    lock (_song._categoryToTag)
                    {
                        var c = _song._categoryToTag.FirstOrDefault(pair => pair.Key.CategoryName.Equals(s));
                        return c.Key != null ? string.Join(", ", c.Value.Select(t => t.TagName)) : null;
                    }
                }
            }

            public void TagsChanged()
            {
                OnPropertyChanged_CatTags("Item[]");
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged_CatTags(string propertyName = null)
            {
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public Song(string title, string location, int milliseconds, Bpm bpm, SongDb songDb,
            uint playCount, uint? songId = null)
        {
            SongTitle = title;
            Location = location;
            Milliseconds = milliseconds;
            Bpm = bpm;
            SongDb = songDb;
            PlayCount = playCount;
            CategoryTags = new CategoryTag(this);
            if (songId != null) SongId.UpdateTagId(songId.Value);
            Id = songId ?? SongId.GetNextSongId();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void TagSong(Tag tag)
        {
            if (!_tags.Contains(tag))
                _tags.Add(tag);
            tag.Songs.Add(this);

            HashSet<Tag> tagSet;
            lock (_categoryToTag)
            {
                if (_categoryToTag.ContainsKey(tag.Category))
                    tagSet = _categoryToTag[tag.Category];
                else
                {
                    tagSet = new HashSet<Tag>();
                    _categoryToTag.Add(tag.Category, tagSet);
                }
            }
            tagSet.Add(tag);

            _songTags = null;
            OnPropertyChanged(Constants.SongTags);
            CategoryTags.TagsChanged();
        }

        public void RemoveTag(Tag tag)
        {
            if (!_tags.Contains(tag)) return;
            _tags.Remove(tag);
            tag.Songs.Remove(this);
            lock (_categoryToTag)
            {
                _categoryToTag[tag.Category].Remove(tag);
                if (!_categoryToTag[tag.Category].Any())
                    _categoryToTag.Remove(tag.Category);
            }
            _songTags = null;
            OnPropertyChanged(Constants.SongTags);
            CategoryTags.TagsChanged();
        }

        private string _songTags;
        public string SongTags
        {
            get
            {
                _songTags = _songTags ?? string.Join(", ", _tags);
                return _songTags;
            }
        }

        public string Length
        {
            get
            {
                return TimeSpan.FromMilliseconds(Milliseconds).ToString(@"mm\:ss");
            }
        }

        private bool _imageTried;
        private BitmapImage _cachedImage;
        private string _songTitle;
        private int _milliseconds;
        private string _location;
        private Bpm _bpm;
        private CategoryTag _categoryTags;
        private uint _playCount;

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
