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
using System.Windows.Controls;
using Musagetes.Annotations;
using Musagetes.DataObjects;
using Musagetes.WpfElements;
using NAudio.Wave;
using NLog;

namespace Musagetes
{
    class MainWindowVm : INotifyPropertyChanged
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private ObservableCollection<Song> _songQueue;
        private ListCollectionView _displayedSongs;
        private readonly Dictionary<Category, PropertyGroupDescription>
            _groupDescriptionDictionary = new Dictionary<Category, PropertyGroupDescription>();

        private IList _selectedSongs;
        private TagEditorVm _tagEditorVm;
        private ColumnManager _columnManager;
        private Song _selectedInQueue;
        private Song _selectedInGrid;

        public NAudioPlayer MainPlayer { get; private set; }
        public NAudioPlayer PreviewPlayer { get; private set; }

        public MainWindowVm()
        {
            TagEditorVm = new TagEditorVm();
            MainPlayer = new NAudioPlayer(-1, true);
            PreviewPlayer = DeviceCount > 1 
                ? new NAudioPlayer(-1, false) 
                : new NAudioPlayer(-1, false);

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
                    += OnColumnsChange;
                foreach (var col in App.SongDb.Columns) AddColumn(col);
            }

            SongQueue = new OrderedObservableCollection<Song>();
        }

        #region Column Management
        public ColumnManager ColumnManager
        {
            get { return _columnManager; }
            set
            {
                _columnManager = value;
                OnPropertyChanged();
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

        public void OnColumnsChange(object sender, NotifyCollectionChangedEventArgs e)
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
            if (DisplayedSongs == null) return;
            lock (_displayedSongs)
                if (DisplayedSongs.GroupDescriptions == null)
                    return;
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
                if (DisplayedSongs == null
                    || DisplayedSongs.GroupDescriptions == null) return;
                lock (_groupDescriptionDictionary)
                {
                    foreach (var cat in categories)
                    {
                        if (_groupDescriptionDictionary.ContainsKey(cat))
                            continue;
                        var groupDesc = new PropertyGroupDescription(
                            string.Format(Constants.CategoryTagsBinding, cat.CategoryName));
                        DisplayedSongs.GroupDescriptions.Add(groupDesc);
                        _groupDescriptionDictionary.Add(cat, groupDesc);
                    }
                }
            }
        }
        #endregion

        public int MainDeviceNumber
        {
            get { return MainPlayer.DeviceNumber; }
            set
            {
                MainPlayer.DeviceNumber = value;
                OnPropertyChanged();
            }
        }

        public int PreviewDeviceNumber
        {
            get { return PreviewPlayer.DeviceNumber; }
            set
            {
                PreviewPlayer.DeviceNumber = value;
                OnPropertyChanged();
            }
        }

        public int DeviceCount { get { return WaveOut.DeviceCount - 1; } }

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

        #region Menu Commands
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

        public ICommand RefreshTagsCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    lock (_displayedSongs) DisplayedSongs.Refresh();
                });
            }
        }
        #endregion

        #region Queue Commands
        public ICommand QueueDropCmd
        {
            get
            {
                return new DropListCommand<Song>(SongQueue);
            }
        }

        public ICommand EnqueueSongCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (SelectedInGrid != null)
                        SongQueue.Add(SelectedInGrid);
                });
            }
        }

        public ICommand RemoveFromQueueCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    SongQueue.Remove(SelectedInQueue);
                });
            }
        }
        #endregion

        #region Playback Commands
        public ICommand TogglePlayCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (MainPlayer.Song == null) return;

                    MainPlayer.PlaybackState = 
                        MainPlayer.IsPlaying
                            ? MediaState.Pause
                            : MediaState.Play;
                });
            }
        }

        public ICommand StopCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    MainPlayer.PlaybackState = MediaState.Stop;
                });
            }
        }

        bool _restartMainPlayer; 
        public ICommand TogglePreviewCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (PreviewPlayer.Song == null) return;

                    if (_restartMainPlayer)
                    {
                        TogglePlayCmd.Execute(null);
                        _restartMainPlayer = false;
                    }
                    else if (PreviewPlayer.DeviceNumber == MainPlayer.DeviceNumber
                        && MainPlayer.IsPlaying)
                    {
                        _restartMainPlayer = true;
                        TogglePlayCmd.Execute(null);
                    }

                    PreviewPlayer.PlaybackState =
                        PreviewPlayer.IsPlaying
                            ? MediaState.Stop
                            : MediaState.Play;
                });
            }
        }

        public ICommand NextCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    var songIdx = SongQueue.IndexOf(MainPlayer.Song);
                    if (songIdx >= SongQueue.Count-1 || songIdx < 0)
                    {
                        StopCmd.Execute(null);
                    }
                    else
                    {
                        SelectedInQueue = SongQueue.ElementAt(songIdx + 1);
                        SwitchToSongCmd.Execute(null);
                    }
                });
            }
        }

        public ICommand PrevCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    var songIdx = SongQueue.IndexOf(MainPlayer.Song);
                    if (songIdx > SongQueue.Count || songIdx <= 0)
                    {
                        StopCmd.Execute(null);
                    }
                    else
                    {
                        SelectedInQueue = SongQueue.ElementAt(songIdx - 1);
                        SwitchToSongCmd.Execute(null);
                    }
                });
            }
        }

        public ICommand SwitchToSongCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    MainPlayer.Song = SelectedInQueue;
                    MainPlayer.PlaybackState = MediaState.Play;
                });
            }
        }
        #endregion

        public IList SelectedSongs
        {
            get { return _selectedSongs; }
            set
            {
                _selectedSongs = value;
                OnPropertyChanged();
            }
        }

        public TagEditorVm TagEditorVm
        {
            get { return _tagEditorVm; }
            set
            {
                _tagEditorVm = value;
                OnPropertyChanged();
            }
        }

        public Song SelectedInGrid
        {
            get { return _selectedInGrid; }
            set
            {
                _selectedInGrid = value;
                OnPropertyChanged();
            }
        }

        public Song SelectedInQueue
        {
            get { return _selectedInQueue; }
            set
            {
                _selectedInQueue = value;
                OnPropertyChanged();
            }
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
