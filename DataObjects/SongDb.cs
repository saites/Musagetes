using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Musagetes.DataObjects
{
    public class SongDb
    {
        public ObservableCollection<GridColumn> Columns { get; private set; }
        public ObservableCollection<Category> GroupCategories { get { return _groupCategories; } }
        public ObservableCollection<Category> Categories { get { return _categories; } }
        public ObservableCollection<Song> Songs { get { return _songs; } }
        public ReadOnlyDictionary<uint, Tag> TagIds { get { return _tagIdsReadOnly; } }
        public ManualResetEvent CategoriesRead { get; private set; }

        public Dictionary<Song, HashSet<Tag>> SongTagDictionary 
            = new Dictionary<Song, HashSet<Tag>>();

        public Dictionary<Tag, HashSet<Song>> TagSongDictionary
            = new Dictionary<Tag, HashSet<Song>>();

        private readonly ObservableCollection<Song> _songs;
        private readonly ObservableCollection<Category> _categories;
        private readonly Dictionary<uint, Tag> _tagIds;
        private readonly ReadOnlyDictionary<uint, Tag> _tagIdsReadOnly;
        private readonly ObservableCollection<Category> _groupCategories;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public Category ArtistCategory
        {
            get { return Categories.FirstOrDefault(c => c.CategoryName == Constants.Artist); }
        }
        public Category AlbumCategory
        {
            get { return Categories.FirstOrDefault(c => c.CategoryName == Constants.Album); }
        }
        public Category GenreCategory
        {
            get { return Categories.FirstOrDefault(c => c.CategoryName == Constants.Genre); }
        }
        public Category UncategorizedCategory
        {
            get { return Categories.FirstOrDefault(c => c.CategoryName == Constants.Uncategorized); }
        }

        private readonly IDbReaderWriter _dataAccess;
        public SongDb(IDbReaderWriter dataAccess)
        {
            Columns = new ObservableCollection<GridColumn>();

            _dataAccess = dataAccess;
            _categories = new ObservableCollection<Category>();
            _songs = new ObservableCollection<Song>();
            _tagIds = new Dictionary<uint, Tag>();
            _tagIdsReadOnly = new ReadOnlyDictionary<uint, Tag>(_tagIds);
            _groupCategories = new ObservableCollection<Category>();

            CategoriesRead = new ManualResetEvent(false);
            new Task(AddDefaultCategories).Start();
        }

        public bool AddSong(Song s)
        {
            if (s == null || _songs.Contains(s))
                return false;
            lock ((_songs as ICollection).SyncRoot)
                _songs.Add(s);
            lock ((SongTagDictionary as ICollection).SyncRoot)
                SongTagDictionary.Add(s, new HashSet<Tag>());
            return true;
        }

        public void RemoveSong(Song s)
        {
            if (s == null || !_songs.Contains(s))
                return;
            lock ((_songs as ICollection).SyncRoot)
                _songs.Remove(s);
            lock ((SongTagDictionary as ICollection).SyncRoot)
                SongTagDictionary.Remove(s);
        }

        public bool AddCategory(Category cat)
        {
            if (cat == null
             || _categories.Contains(cat)
             || (_categories.Any(c => c.CategoryName == cat.CategoryName)))
                return false;
            lock ((_categories as ICollection).SyncRoot)
            {
                _categories.Add(cat);
            }

            //TODO: add a Column for this category here
            return true;
        }


        public void RemoveCategory(Category cat)
        {
            if (cat == null
                || !_categories.Contains(cat)
                || IsDefaultCategory(cat)) return;
            lock ((_categories as ICollection).SyncRoot)
            {
                _categories.Remove(cat);
            }
            lock (((ICollection)Columns).SyncRoot)
            {
                Columns.Remove(Columns.First(c => c.Category == cat));
            }
            lock (((ICollection) GroupCategories).SyncRoot)
            {
                if (GroupCategories.Contains(cat)) GroupCategories.Remove(cat);
            }
        }

        public bool AddTag(Tag tag)
        {
            if (tag == null || _tagIds.ContainsKey(tag.Id))
                return false;
            lock ((_tagIds as ICollection).SyncRoot)
                _tagIds.Add(tag.Id, tag);
            lock ((TagSongDictionary as ICollection).SyncRoot)
                TagSongDictionary.Add(tag, new HashSet<Song>());
            return true;
        }

        public void RemoveTag(Tag tag)
        {
            if (tag == null || !_tagIds.ContainsKey(tag.Id))
                return;
            
            lock ((_tagIds as ICollection).SyncRoot)
                _tagIds.Remove(tag.Id);
            lock ((TagSongDictionary as ICollection).SyncRoot)
                TagSongDictionary.Remove(tag);
        }

        public void TagSong(Song song, Tag tag)
        {
            if (!_tagIds.ContainsKey(tag.Id))
                AddTag(tag);
            if (!_songs.Contains(song))
                AddSong(song);
            lock ((SongTagDictionary as ICollection).SyncRoot)
                SongTagDictionary[song].Add(tag);
            lock ((TagSongDictionary as ICollection).SyncRoot)
                TagSongDictionary[tag].Add(song);
            song.NotifyTagChanged();
        }

        public void UntagSong(Song song, Tag tag)
        {
            lock ((SongTagDictionary as ICollection).SyncRoot)
                SongTagDictionary[song].Remove(tag);
            lock ((TagSongDictionary as ICollection).SyncRoot)
                TagSongDictionary[tag].Remove(song);
            song.NotifyTagChanged();
        }

        public interface IDbReaderWriter
        {
            Task WriteDbAsync(string filename, SongDb songDb);
            Task ReadDbAsync(string filename, SongDb songDb);
        }

        public async Task SaveDbAsync(string filename)
        {
            await _dataAccess.WriteDbAsync(filename, this);
        }

        public async Task ReadDbAsync(string filename)
        {
            await _dataAccess.ReadDbAsync(filename, this);
        }

        public bool IsDefaultCategory(Category cat)
        {
            return cat == AlbumCategory
                   || cat == ArtistCategory
                   || cat == GenreCategory
                   || cat == UncategorizedCategory;
        }

        public void AddDefaultCategories()
        {
            CategoriesRead.WaitOne();
            lock (((ICollection) Columns).SyncRoot)
            {
                if (AddCategory(new Category(Constants.Artist)))
                    Columns.Add(new GridColumn(
                        GridColumn.ColumnTypeEnum.Category, ArtistCategory, 
                        isVisible: false));
                if (AddCategory(new Category(Constants.Album)))
                    Columns.Add(new GridColumn(
                        GridColumn.ColumnTypeEnum.Category, AlbumCategory, 
                        isVisible: false));
                if (AddCategory(new Category(Constants.Genre)))
                    Columns.Add(new GridColumn(
                        GridColumn.ColumnTypeEnum.Category, GenreCategory, 
                        isVisible: false));
                if (AddCategory(new Category(Constants.Uncategorized)))
                    Columns.Add(new GridColumn(
                        GridColumn.ColumnTypeEnum.Category, UncategorizedCategory, 
                        isVisible: false));
            }
        }

        public void InsertFromFile(string filename)
        {
            CategoriesRead.WaitOne();
            try
            {
                Logger.Debug("Attempting to add {0}", filename);
                if (!File.Exists(filename))
                {
                    Logger.Error("File {0} does not exist", filename);
                    return;
                }

                using (var file = TagLib.File.Create(filename))
                {
                    var song = new Song(file.Tag.Title, filename,
                        (int)file.Properties.Duration.TotalMilliseconds, new Bpm(0, true), 0);
                    AddBaseTags(song, ArtistCategory, file.Tag.AlbumArtists);
                    AddBaseTags(song, GenreCategory, file.Tag.Genres);
                    if (file.Tag.Album != null)
                    {
                        var albumTag = AlbumCategory[file.Tag.Album]
                               ?? new Tag(file.Tag.Album, AlbumCategory); 
                        TagSong(song, albumTag);
                    }
                    if(file.Tag.BeatsPerMinute > 0 && file.Tag.BeatsPerMinute < int.MaxValue)
                        song.Bpm = new Bpm((int)file.Tag.BeatsPerMinute, false);
                    AddSong(song);
                    //SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error("Couldn't open file with TagLib: {0}\n{1}",
                    e.Message, e.StackTrace);
            }
        }

        private void AddBaseTags(Song song, Category category, IEnumerable<string> tagList)
        {
            foreach (var tag in tagList
                .Select(tagName =>
                    category[tagName] ?? new Tag(tagName, category)))
            {
                TagSong(song, tag);
            }
        }


        public bool IsFiletypeSupported(string filename)
        {
            var fileExt = Path.GetExtension(filename);
            return Constants.SupportedFileTypes
                .Any(ext => ext.Equals(fileExt));
        }
    }
}
