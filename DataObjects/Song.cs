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
        public string SongTitle { get; set; }
        public long Milliseconds { get; set; }
        public string Location { get; set; }
        public BPM Bpm { get; set; }
        public CategoryTag CategoryTags { get; set; }
        public SongDb SongDb { get; private set; }
        public uint PlayCount { get; set; }
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

        public Song(string title, string location, long milliseconds, BPM bpm, SongDb songDb, uint playCount)
        {
            SongTitle = title;
            Location = location;
            Milliseconds = milliseconds;
            Bpm = bpm;
            SongDb = songDb;
            PlayCount = playCount;
            CategoryTags = new CategoryTag(this);
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
                var ts = new TimeSpan(milliseconds: Milliseconds);
                return ts.ToString();
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
                            BitmapImage AlbumArt = new BitmapImage();
                            AlbumArt.BeginInit();
                            AlbumArt.StreamSource = new MemoryStream(file.Tag.Pictures[0].Data.Data);
                            AlbumArt.EndInit();
                            retval = AlbumArt;
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
