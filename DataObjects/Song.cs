using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.IO;
using System.Linq;

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

        public string SongTitle { get; set; }
        public int Milliseconds { get; set; }
        public string Location { get; set; }
        public Bpm Bpm { get; set; }
        public CategoryTag CategoryTags { get; set; }
        public SongDb SongDb { get; private set; }
        public uint PlayCount { get; set; }
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
        public BitmapImage Art
        {
            get
            {
                if (_imageTried) return _cachedImage;

                BitmapImage retval = null;
                try
                {
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
                }
                catch (Exception ex)
                {
                    if (!(ex is FileNotFoundException || ex is DirectoryNotFoundException))
                        throw;
                }

                _cachedImage = retval;
                _imageTried = true;
                return retval;
            }
        }
    }
}
