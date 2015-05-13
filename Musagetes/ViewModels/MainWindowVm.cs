using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Musagetes.Annotations;
using Musagetes.DataObjects;
using Musagetes.Toolkit;
using Musagetes.Windows;
using Musagetes.WpfElements;
using MvvmFoundation.Wpf;
using NAudio.Wave;
using NLog;
using Application = System.Windows.Application;
using Forms = System.Windows.Forms;

namespace Musagetes.ViewModels
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

        public BpmTapper BpmCalc { get; set; }

        public NAudioPlayer MainPlayer { get; private set; }
        public NAudioPlayer PreviewPlayer { get; private set; }

        private bool SongFilter(object item)
        {
            var song = item as Song;
            if (_filterText == null) return true;
            if (song == null) return false;
            var tempFilterText = _filterText.ToUpper();
            return song.SongTitle.ToUpper().Contains(tempFilterText)
                   || song.Tags.Any(
                       t => t.TagName.ToUpper().Contains(tempFilterText));
        }

        public string FilterText
        {
            get { return _filterText; }
            set
            {
                if (_filterText == value) return;

                _filterText = value;
                lock (_displayedSongs)
                    _displayedSongs.Refresh();
                OnPropertyChanged();
            }
        }

        private int _oldQueueSelection = -1;
        public ICommand ClearQueueSelectionCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    _oldQueueSelection = QueueSelectionIndex;
                    SelectedInQueue = null;
                });
            }
        }
        public int CurrentSongIndex
        {
            get { return _currentSongIndex; }
            private set
            {
                if (MainPlayer == null || SongQueue == null)
                {
                    _currentSongIndex = -1;
                    OnPropertyChanged();
                    return;
                }

                if (value >= 0 && value < SongQueue.Count)
                {
                    MainPlayer.Song = SongQueue.ElementAt(value);
                }
                else
                {
                    value = -1;
                }

                _currentSongIndex = value;
                OnPropertyChanged();
            }
        }

        public MainWindowVm()
        {
            TagEditorVm = new TagEditorVm();
            BpmCalc = new BpmTapper();
            CurrentSongIndex = 0;
            MainPlayer = new NAudioPlayer(App.Configuration.MainPlayerDeviceNum, true);
            PreviewPlayer = new NAudioPlayer(App.Configuration.SecondaryPlayerDeviceNum,
                App.Configuration.UpdatePlaycountOnPreview);
            MainPlayer.Volume = App.Configuration.MainPlayerVolume;
            PreviewPlayer.Volume = App.Configuration.SecondaryPlayerVolume;

            MainPlayer.SongCompletedEvent += (sender, args) =>
            {
                NextCmd.Execute(null);
            };

            MainPlayer.PropertyChanged += MainPlayerPropertyChanged;
            PreviewPlayer.PropertyChanged += PreviewPlayerPropertyChanged;

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
                AddGroupDescriptions(0, App.SongDb.GroupCategories);
                App.SongDb.GroupCategories.CollectionChanged +=
                    GroupCategoriesCollectionChanged;
                lock (_displayedSongs)
                {
                    DisplayedSongs.Filter = SongFilter;
                    DisplayedSongs.Refresh();
                }
            }

            lock ((App.SongDb.Columns as ICollection).SyncRoot)
            {
                ((App.SongDb.Columns) as INotifyCollectionChanged).CollectionChanged
                    += OnColumnsChange;
                foreach (var col in App.SongDb.Columns) AddColumn(col);
            }

            SongQueue = new ObservableCollection<Song>();
        }

        private void PreviewPlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "PlaybackState":
                    PreviewPlaybackChanged();
                    break;
                case "Volume":
                    App.Configuration.SecondaryPlayerVolume = PreviewPlayer.Volume;
                    break;
            }
        }

        public Song PreviewSong
        {
            get { return _previewSong; }
            set
            {
                _previewSong = value;
                if (value == null)
                {
                    PreviewPlayer.PlaybackState = MediaState.Stop;
                    BpmCalc.StopTapping.Execute(null);
                }
                else
                    PreviewPlayer.Song = value;
                OnPropertyChanged();
            }
        }

        private void PreviewPlaybackChanged()
        {
            if (_restartMainPlayer)
            {
                MainPlayer.PlaybackState = MediaState.Play;
                _restartMainPlayer = false;
            }
            else if (PreviewPlayer.PlaybackState == MediaState.Play
                && MainPlayer.DeviceNumber == PreviewPlayer.DeviceNumber
                && MainPlayer.IsPlaying)
            {
                MainPlayer.PlaybackState = MediaState.Pause;
                _restartMainPlayer = true;
            }
        }

        private void MainPlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Volume"))
                App.Configuration.MainPlayerVolume = MainPlayer.Volume;
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
                    AddGroupDescriptions(e.NewStartingIndex, e.NewItems.Cast<Category>());
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldItems == null) return;
                    lock (_displayedSongs)
                        DisplayedSongs.GroupDescriptions.
                            Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems == null) return;
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
                    AddGroupDescriptions(e.NewStartingIndex, e.NewItems.Cast<Category>());
                    break;
            }
            lock (_displayedSongs) DisplayedSongs.Refresh();
        }

        private void AddGroupDescriptions(int index, IEnumerable<Category> categories)
        {
            lock (_displayedSongs)
            {
                if (DisplayedSongs == null
                    || DisplayedSongs.GroupDescriptions == null) return;
                lock (_groupDescriptionDictionary)
                {
                    foreach (var cat in categories)
                    {
                        PropertyGroupDescription groupDesc;
                        if (_groupDescriptionDictionary.ContainsKey(cat))
                            groupDesc = _groupDescriptionDictionary[cat];
                        else
                        {
                            groupDesc = new PropertyGroupDescription(
                                string.Format(Constants.CategoryTagsBinding, cat.CategoryName));
                            _groupDescriptionDictionary.Add(cat, groupDesc);
                        }
                        DisplayedSongs.GroupDescriptions.Insert(index, groupDesc);
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
                    var categoryOptionsVm = new CategoryDisplayOptionsVm(ColumnManager.Columns,
                        App.SongDb.Categories, App.SongDb.GroupCategories)
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
                    Task.Run(() => App.SongDb.SaveDbAsync(App.Configuration.DbLocation));
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
                    var tempSelect = QueueSelectionIndex;
                    if (QueueSelectionIndex >= 0
                        && QueueSelectionIndex < SongQueue.Count)
                        SongQueue.RemoveAt(QueueSelectionIndex);

                    if (CurrentSongIndex > tempSelect)
                        CurrentSongIndex--;
                    else if (CurrentSongIndex == tempSelect)
                        PlaySongAtIndex(CurrentSongIndex);
                    if (tempSelect >= SongQueue.Count) tempSelect--;
                    QueueSelectionIndex = tempSelect;
                });
            }
        }
        #endregion

        private void PlaySongAtIndex(int index)
        {
            MainPlayer.PlaybackState = MediaState.Stop;

            if (index == -1 || index >= SongQueue.Count || index < -2)
            {
                CurrentSongIndex = -1;
                return;
            }

            if (CurrentSongIndex < 0 || CurrentSongIndex >= SongQueue.Count)
            {
                if (index >= 0) CurrentSongIndex = 0;
                else CurrentSongIndex = SongQueue.Count - 1;
            }
            else CurrentSongIndex = index;
            MainPlayer.PlaybackState = MediaState.Play;
        }

        #region Playback Commands
        public ICommand TogglePlayCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (!SongQueue.Any()) return;

                    if (MainPlayer.PlaybackState == MediaState.Stop)
                    {
                        if (_oldQueueSelection >= 0
                            && _oldQueueSelection < SongQueue.Count)
                            CurrentSongIndex = _oldQueueSelection;
                        else
                            CurrentSongIndex = 0;
                        MainPlayer.PlaybackState = MediaState.Play;
                        return;
                    }

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
                    CurrentSongIndex = -1;
                });
            }
        }

        bool _restartMainPlayer;
        private int _currentSongIndex = -1;
        private string _filterText;
        private int _queueSelectionIndex;
        private Song _previewSong;

        public ICommand TogglePreviewCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (PreviewSong == null) return;
                    if (PreviewPlayer.Song == null)
                        PreviewPlayer.Song = PreviewSong;

                    PreviewPlayer.PlaybackState =
                        PreviewPlayer.IsPlaying
                            ? MediaState.Pause
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
                    PlaySongAtIndex(CurrentSongIndex + 1);
                });
            }
        }

        public ICommand PrevCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    PlaySongAtIndex(CurrentSongIndex - 1);
                });
            }
        }

        public ICommand SwitchToSongCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (CurrentSongIndex == QueueSelectionIndex) return;
                    MainPlayer.PlaybackState = MediaState.Stop;
                    CurrentSongIndex = QueueSelectionIndex;
                    MainPlayer.PlaybackState = MediaState.Play;
                });
            }
        }
        #endregion


        public ICommand SaveBpmCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    PreviewSong.Bpm.Value = (int)BpmCalc.Value;
                    PreviewSong.Bpm.Guess = false;
                    BpmCalc.StopTapping.Execute(null);
                });
            }
        }

        public int QueueSelectionIndex
        {
            get { return _queueSelectionIndex; }
            set
            {
                _queueSelectionIndex = value;
                OnPropertyChanged();
            }
        }

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

        public ICommand IncrementPlayCountCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (PreviewSong == null
                        || PreviewSong.PlayCount == UInt32.MaxValue) 
                        return;
                    PreviewSong.PlayCount++;
                });
            }
        }

        public ICommand DecrementPlayCountCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (PreviewSong == null
                        || PreviewSong.PlayCount == 0) 
                        return;
                    PreviewSong.PlayCount--;
                });
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
