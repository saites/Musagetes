using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Musagetes.DataObjects
{
    public class SongDb
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public ObservableCollection<GridColumn> Columns { get; private set; }
        public OrderedObservableCollection<Category> GroupCategories { get { return _groupCategories; } }
        public ReadOnlyObservableCollection<Category> Categories { get { return _categoriesReadOnly; } }
        public ReadOnlyObservableCollection<Song> Songs { get { return _songsReadOnly; } }
        public ReadOnlyDictionary<string, Category> CategoryDictionary { get { return _categoryDictionaryReadOnly; } }
        public ReadOnlyDictionary<int, Tag> TagIds { get { return _tagIdsReadOnly; } }
        public ManualResetEvent CategoriesRead { get; private set; }

        private readonly ObservableCollection<Song> _songs;
        private readonly ReadOnlyObservableCollection<Song> _songsReadOnly;
        private readonly ObservableCollection<Category> _categories;
        private readonly ReadOnlyObservableCollection<Category> _categoriesReadOnly;
        private readonly Dictionary<string, Category> _categoryDictionary;
        private readonly ReadOnlyDictionary<string, Category> _categoryDictionaryReadOnly;
        private readonly Dictionary<int, Tag> _tagIds;
        private readonly ReadOnlyDictionary<int, Tag> _tagIdsReadOnly;
        private readonly OrderedObservableCollection<Category> _groupCategories;

        public Category ArtistCategory
        {
            get { return CategoryDictionary[Constants.Artist]; }
        }
        public Category AlbumCategory
        {
            get { return CategoryDictionary[Constants.Album]; }
        }
        public Category GenreCategory
        {
            get { return CategoryDictionary[Constants.Genre]; }
        }
        public Category UncategorizedCategory
        {
            get { return CategoryDictionary[Constants.Uncategorized]; }
        }

        private readonly IDbReaderWriter _dataAccess;
        public SongDb(IDbReaderWriter dataAccess)
        {
            Columns = new ObservableCollection<GridColumn>();

            _dataAccess = dataAccess;
            _categories = new ObservableCollection<Category>();
            _categoriesReadOnly = new ReadOnlyObservableCollection<Category>(_categories);
            _songs = new ObservableCollection<Song>();
            _songsReadOnly = new ReadOnlyObservableCollection<Song>(_songs);
            _categoryDictionary = new Dictionary<string, Category>();
            _categoryDictionaryReadOnly =
                new ReadOnlyDictionary<string, Category>(_categoryDictionary);
            _tagIds = new Dictionary<int, Tag>();
            _tagIdsReadOnly = new ReadOnlyDictionary<int, Tag>(_tagIds);
            _groupCategories = new OrderedObservableCollection<Category>();

            CategoriesRead = new ManualResetEvent(false);
            new Task(AddDefaultCategories).Start();
        }

        public bool AddSong(Song s)
        {
            if (s == null || _songs.Contains(s))
                return false;
            lock ((_songs as ICollection).SyncRoot)
            {
                _songs.Add(s);
            }
            return true;
        }

        public bool AddCategory(Category cat)
        {
            if (cat == null
             || _categories.Contains(cat)
             || (_categoryDictionary.ContainsKey(cat.CategoryName)))
                return false;
            lock ((_categories as ICollection).SyncRoot)
            {
                _categories.Add(cat);
                _categoryDictionary.Add(cat.CategoryName, cat);
            }
            return true;
        }

        public bool AddTag(Tag tag)
        {
            if (tag == null || _tagIds.ContainsKey(tag.TagId))
                return false;
            lock ((_tagIds as ICollection).SyncRoot)
            {
                _tagIds.Add(tag.TagId, tag);
            }
            lock (_tagIdLock)
            {
                if (tag.TagId >= _nextTagId)
                    _nextTagId = tag.TagId + 1;
            }
            return true;
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

        public void AddDefaultCategories()
        {
            CategoriesRead.WaitOne();
            if(AddCategory(new Category(Constants.Artist)))
                Columns.Add(new GridColumn(
                    GridColumn.ColumnTypeEnum.Category, ArtistCategory, isVisible: false));
            if(AddCategory(new Category(Constants.Album)))
                Columns.Add(new GridColumn(
                    GridColumn.ColumnTypeEnum.Category, AlbumCategory, isVisible: false));
            if(AddCategory(new Category(Constants.Genre)))
                Columns.Add(new GridColumn(
                    GridColumn.ColumnTypeEnum.Category, GenreCategory, isVisible: false));
            if(AddCategory(new Category(Constants.Uncategorized)))
                Columns.Add(new GridColumn(
                    GridColumn.ColumnTypeEnum.Category, UncategorizedCategory, isVisible: false));
        }

        private readonly Object _tagIdLock = new object();
        private int _nextTagId = 1;
        public int GetNextTagId()
        {
            lock (_tagIdLock)
            {
                _nextTagId++;
                return _nextTagId;
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
                        (int)file.Properties.Duration.TotalMilliseconds, new BPM(0, false), 
                        this, 0);
                    AddBaseTags(song, ArtistCategory, file.Tag.AlbumArtists);
                    AddBaseTags(song, GenreCategory, file.Tag.Genres);
                    if (file.Tag.Album != null)
                    {
                        var albumTag = AlbumCategory[file.Tag.Album]
                            ?? new Tag(file.Tag.Album, AlbumCategory, GetNextTagId());
                        song.TagSong(albumTag);
                    }
                    if(file.Tag.BeatsPerMinute > 0 && file.Tag.BeatsPerMinute < int.MaxValue)
                        song.Bpm = new BPM((int)file.Tag.BeatsPerMinute, true);
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
                    category[tagName]
                    ?? new Tag(tagName, category, GetNextTagId())))
            {
                song.TagSong(tag);
            }
        }

        public bool IsFiletypeSupported(string filename)
        {
            return Constants.SupportedFileTypes
                .Any(ext => filename.EndsWith(ext, 
                    StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
