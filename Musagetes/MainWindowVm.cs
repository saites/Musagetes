using System.Collections.Specialized;
using System.Linq;
using MvvmFoundation.Wpf;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Data;
using Forms = System.Windows.Forms;
using System.Windows.Input;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using Musagetes.Annotations;
using Musagetes.DataObjects;
using Pcb = Musagetes.PlaybackControlBehavior;

namespace Musagetes
{
    class MainWindowVm : INotifyPropertyChanged
    {
        private long _currentTime;

        public long CurrentTime
        {
            get { return _currentTime; }
            set
            {
                _currentTime = value;
                OnPropertyChanged();
            }
        }
        ObservableCollection<Song> _songQueue;
        ListCollectionView _displayedSongs;
        private Song _currentSong;
        public Song CurrentSong
        {
            get { return _currentSong; }
            set
            {
                _currentSong = value;
                OnPropertyChanged();
            }
        }

        public ICommand PlayCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    PlaybackState = PlaybackState == Pcb.Playback.Play
                        ? Pcb.Playback.Pause
                        : Pcb.Playback.Play;
                });
            }
        }

        private PlaybackControlBehavior.Playback _playbackState = PlaybackControlBehavior.Playback.Stop;

        public PlaybackControlBehavior.Playback PlaybackState
        {
            get { return _playbackState; }
            set
            {
                _playbackState = value;
                OnPropertyChanged();
            }
        }

        private readonly Dictionary<Category, PropertyGroupDescription>
            _groupDescriptionDictionary = new Dictionary<Category, PropertyGroupDescription>();

        public ColumnManager ColumnManager
        {
            get { return _columnManager; }
            set
            {
                _columnManager = value;
                OnPropertyChanged();
            }
        }

        public MainWindowVm()
        {
            TagEditorVm = new TagEditorVm();
            _columnManager = new ColumnManager();

            BindingOperations.EnableCollectionSynchronization(
                App.SongDb.Songs,
                (App.SongDb.Songs as ICollection).SyncRoot);
            BindingOperations.EnableCollectionSynchronization(
                App.SongDb.GroupCategories,
                (App.SongDb.GroupCategories as ICollection).SyncRoot);

            lock ((App.SongDb.Songs as ICollection).SyncRoot)
            {
                DisplayedSongs = new ListCollectionView(App.SongDb.Songs);
            }

            lock ((App.SongDb.GroupCategories as ICollection).SyncRoot)
            {
                AddGroupDescriptions(App.SongDb.GroupCategories);
                App.SongDb.GroupCategories.CollectionChanged +=
                    GroupCategoriesCollectionChanged;
                lock (_displayedSongs) DisplayedSongs.Refresh();
            }

            lock ((App.SongDb.Columns as ICollection).SyncRoot)
            {
                ((App.SongDb.Columns) as INotifyCollectionChanged).CollectionChanged
                    += OnCategoryChange;
                foreach (var col in App.SongDb.Columns) AddColumn(col);
            }
        }

        private void AddColumn(GridColumn column)
        {
            switch (column.ColumnType)
            {
                case GridColumn.ColumnTypeEnum.BasicText:
                    var width = column.Header.Equals("Title") ? 2.0 : 1.0;
                    ColumnManager.AddNewTextColumn(column.Header,
                        column.Binding, column.IsVisible, width: width);
                    break;
                case GridColumn.ColumnTypeEnum.Bpm:
                    ColumnManager.AddBpmColumn();
                    break;
                case GridColumn.ColumnTypeEnum.Category:
                    ColumnManager.AddNewTextColumn(column.Category.CategoryName,
                        string.Format(Constants.CategoryTagsBinding,
                        column.Category.CategoryName), column.IsVisible);
                    break;
            }
        }

        public void OnCategoryChange(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems == null) return;
                    Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        foreach (GridColumn col in e.NewItems) AddColumn(col);
                    }));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems == null) return;
                    Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        foreach (GridColumn col in e.OldItems)
                        {
                            var colName = col.ColumnType == GridColumn.ColumnTypeEnum.Category
                                ? col.Category.CategoryName
                                : col.Header;
                            ColumnManager.RemoveColumn(colName);
                        }
                    }));
                    break;
            }
        }

        private void GroupCategoriesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (DisplayedSongs == null || DisplayedSongs.GroupDescriptions == null) return;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems == null) return;
                    AddGroupDescriptions(e.NewItems.Cast<Category>());
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.NewItems == null || e.OldItems == null) return;
                    lock (_displayedSongs)
                        DisplayedSongs.GroupDescriptions.
                            Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.NewItems == null || e.OldItems == null) return;
                    lock (_displayedSongs)
                    {
                        foreach (Category cat in e.OldItems)
                            DisplayedSongs.GroupDescriptions
                                .Remove(_groupDescriptionDictionary[cat]);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    lock (_displayedSongs)
                        DisplayedSongs.GroupDescriptions.Clear();
                    if (e.NewItems == null) return;
                    AddGroupDescriptions(e.NewItems.Cast<Category>());
                    break;
            }
            lock (_displayedSongs) DisplayedSongs.Refresh();
        }

        private void AddGroupDescriptions(IEnumerable<Category> categories)
        {
            lock (_displayedSongs)
            {
                if (DisplayedSongs == null || DisplayedSongs.GroupDescriptions == null) return;
                foreach (var cat in categories)
                {
                    var groupDesc = new PropertyGroupDescription(
                        string.Format(Constants.CategoryTagsBinding, cat.CategoryName));
                    DisplayedSongs.GroupDescriptions.Add(groupDesc);
                    lock (_groupDescriptionDictionary)
                    {
                        _groupDescriptionDictionary.Add(cat, groupDesc);
                    }
                }
            }
        }

        public List<string> BigTagList
        {
            get { return App.SongDb.TagIds.Values.Select(t => t.TagName).ToList(); }
        }
        public ListCollectionView DisplayedSongs
        {
            get { return _displayedSongs; }
            set
            {
                if (value == _displayedSongs)
                    return;
                _displayedSongs = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Song> SongQueue
        {
            get { return _songQueue; }

            set
            {
                if (value == _songQueue)
                    return;

                _songQueue = value;
                OnPropertyChanged();
            }
        }

        public ICommand QuitCmd
        {
            get
            {
                return new RelayCommand(() => Environment.Exit(0));
            }
        }
        public ICommand AddDirCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    var fbd = new Forms.FolderBrowserDialog
                    {
                        ShowNewFolderButton = false,
                        //RootFolder = Environment.SpecialFolder.MyMusic
                    };

                    if (fbd.ShowDialog() == Forms.DialogResult.OK)
                        Task.Run(() => AddDirAndFiles(fbd.SelectedPath));
                });
            }
        }

        public ICommand ChangeGroupingCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    var categoryOptions = new CategoryDisplayOptions();
                    var categoryOptionsVm = new CategoryDisplayOptionsVm(ColumnManager.Columns)
                    {
                        CloseAction = categoryOptions.Close
                    };
                    categoryOptions.DataContext = categoryOptionsVm;
                    categoryOptions.ShowDialog();
                });
            }
        }

        public ICommand SaveCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    Task.Run(() => App.SongDb.SaveDbAsync(Constants.SaveLoc));
                });
            }
        }

        public ICommand UpdateCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    App.SongDb.GroupCategories.Add(App.SongDb.CategoryDictionary["Artist"]);
                });
            }
        }

        private Song _selectedSong;
        public Song SelectedSong
        {
            get { return _selectedSong; }
            set
            {
                _selectedSong = value;
                OnPropertyChanged();
            }
        }

        private IList _selectedSongs;
        public IList SelectedSongs
        {
            get { return _selectedSongs; }
            set
            {
                _selectedSongs = value;
                OnPropertyChanged();
            }
        }

        private TagEditorVm _tagEditorVm;
        private ColumnManager _columnManager;

        public TagEditorVm TagEditorVm
        {
            get { return _tagEditorVm; }
            set
            {
                _tagEditorVm = value;
                OnPropertyChanged();
            }
        }
        public ICommand OpenContextMenu
        {
            get
            {
                return new RelayCommand(() =>
                {
                    var tagEditor = new TagEditorWindow
                        {
                            DataContext = TagEditorVm
                        };
                    tagEditor.ShowDialog();
                }
                );
            }
        }

        public ICommand RefreshTagsCmd
        {
            get { return new RelayCommand(() => DisplayedSongs.Refresh()); }
        }

        private async Task AddDirAndFiles(string dir)
        {
            Console.WriteLine(dir);
            foreach (var filename in Directory.GetFiles(dir)
                .Where(filename => App.SongDb.IsFiletypeSupported(filename)))
            {
                Console.WriteLine(filename);
                App.SongDb.InsertFromFile(filename);
            }

            foreach (var subdir in Directory.EnumerateDirectories(dir))
            {
                await AddDirAndFiles(subdir);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
