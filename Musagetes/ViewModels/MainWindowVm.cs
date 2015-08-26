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
            var tags = App.SongDb.SongTagDictionary[song];
            var tempFilterText = _filterText.ToUpper();
            return song.SongTitle.ToUpper().Contains(tempFilterText)
               || tags.Any(t => t.TagName.ToUpper().Contains(tempFilterText));
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
                    Logger.Debug("Clearing queue selection index");
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
                Logger.Debug("Setting CurrentSongIndex to {0}", value);

                if (MainPlayer == null || SongQueue == null)
                {
                    Logger.Warn("MainPlayer or SongQueue is null, so setting current index to -1");
                    _currentSongIndex = -1;
                    OnPropertyChanged();
                    return;
                }

                if (value >= 0 && value < SongQueue.Count)
                {
                    Logger.Info("Setting main player song to CurrentSongIndex value of {0} ({1})",
                        value, SongQueue.ElementAt(value).SongTitle);
                    MainPlayer.Song = SongQueue.ElementAt(value);
                }
                else
                {
                    Logger.Info("Setting CurrentSongIndex to -1, since value was outside of SongQueue bound");
                    value = -1;
                }

                _currentSongIndex = value;
                OnPropertyChanged();
            }
        }

        public MainWindowVm()
        {
            Logger.Info("Initializing MainWindow View Model");
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
                Logger.Info("Song completed; playing next song");
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
            Logger.Info("Done initializing MainWindow view model");
        }

        private void PreviewPlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Logger.Debug("PreviewPlayer property changed: {0}", e.PropertyName);
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
                    Logger.Debug("Stopping preview player and Bpm calculator");
                    PreviewPlayer.PlaybackState = MediaState.Stop;
                    BpmCalc.StopTapping.Execute(null);
                }
                else
                {
                    Logger.Debug("Setting preview player song to {0}", value.SongTitle);
                    PreviewPlayer.Song = value;
                }
                OnPropertyChanged();
            }
        }

        private void PreviewPlaybackChanged()
        {
            if (_restartMainPlayer)
            {
                Logger.Info("Restarting the main player after preview");
                MainPlayer.PlaybackState = MediaState.Play;
                _restartMainPlayer = false;
            }
            else if (PreviewPlayer.PlaybackState == MediaState.Play
                && MainPlayer.DeviceNumber == PreviewPlayer.DeviceNumber
                && MainPlayer.IsPlaying)
            {
                Logger.Info("Pausing the main player for preivew");
                MainPlayer.PlaybackState = MediaState.Pause;
                _restartMainPlayer = true;
            }
        }

        private void MainPlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals("Volume")) return;
            Logger.Debug("Updating MainPlayer volume");
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

        private readonly SongToTagsConverter SttConverter =
            new SongToTagsConverter(App.SongDb);
        private void AddColumn(GridColumn column)
        {
            Logger.Debug("Adding column to column manager: {0}", column.Header);
            switch (column.ColumnType)
            {
                case GridColumn.ColumnTypeEnum.BasicText:
                    ColumnManager.AddNewTextColumn(column.Header,
                        column.Binding, column.IsVisible);
                    break;
                case GridColumn.ColumnTypeEnum.Bpm:
                    ColumnManager.AddBpmColumn(column.Header,
                        column.Binding, column.IsVisible);
                    break;
                case GridColumn.ColumnTypeEnum.Category:
                    var catcol = ColumnManager.AddNewTextColumn(
                        column.Category.CategoryName,
                        "Self",
                        column.IsVisible);
                    catcol.Binding = new Binding(Constants.SongBinding)
                    {
                        Converter = SttConverter,
                        ConverterParameter = column.Category,
                        NotifyOnTargetUpdated = true,
                    };

                    column.Category.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName != "CategoryName") return;
                        catcol.Header = column.Category.CategoryName;
                        catcol.Binding = new Binding(Constants.SongBinding)
                        {
                            Converter = SttConverter,
                            ConverterParameter = column.Category,
                            NotifyOnTargetUpdated = true
                        };
                    };
                    break;
                case GridColumn.ColumnTypeEnum.Tags:
                    var col = ColumnManager.AddNewTextColumn(column.Header,
                        string.Empty, column.IsVisible);
                    col.Binding = new Binding(Constants.SongBinding)
                    {
                        Converter = SttConverter,
                        ConverterParameter = null,
                        NotifyOnTargetUpdated = true
                    };
                    break;
            }
        }

        public void OnColumnsChange(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems == null) return;
                    Logger.Debug("Adding new columns");
                    Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        foreach (GridColumn col in e.NewItems) AddColumn(col);
                    }));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems == null) return;
                    Logger.Debug("Removing columns");
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

        private void GroupCategoriesCollectionChanged(object sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (DisplayedSongs == null) return;
            Logger.Debug("Updating group descriptions");
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
                Logger.Debug("Adding group descriptions");
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
                                Constants.CategoryTagsBinding,
                                new SongToTagsConverter(App.SongDb, cat));
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
                Logger.Debug("Changing MainPlayer device number to {0}", value);
                MainPlayer.DeviceNumber = value;
                OnPropertyChanged();
            }
        }

        public int PreviewDeviceNumber
        {
            get { return PreviewPlayer.DeviceNumber; }
            set
            {
                Logger.Debug("Changing PreviewPlayer device number to {0}", value);
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
                Logger.Debug("Setting DisplayedSongs collection");
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

                Logger.Debug("Setting SongQueue collection");
                _songQueue = value;
                OnPropertyChanged();
            }
        }

        #region Menu Commands
        public ICommand QuitCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    Logger.Info("Shutting down Musagetes from QuitCmd");
                    Environment.Exit(0);
                });
            }
        }

        public ICommand AddDirCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    Logger.Debug("Openning folder browser dialog");
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
                    Logger.Debug("Openning CategoryOptions");
                    var categoryOptions = new CategoryDisplayOptions();
                    var categoryOptionsVm = new OptionsVm(ColumnManager.Columns,
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
                    Logger.Debug("Enqueing song");
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
                    Logger.Debug("Removing song from queue");
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
            Logger.Debug("Playing song at index {0}; CurrentSongIndex is {1}", 
                index, CurrentSongIndex);
            Logger.Debug("Stopping current playback, if any");
            MainPlayer.PlaybackState = MediaState.Stop;

            if (index == -1 || index >= SongQueue.Count || index < -2)
            {
                Logger.Debug("index is outside of range, so setting CurrentSongIndex to -1 and returning");
                CurrentSongIndex = -1;
                return;
            }

            if (CurrentSongIndex < 0 || CurrentSongIndex >= SongQueue.Count)
            {
                if (index >= 0) CurrentSongIndex = 0;
                else CurrentSongIndex = SongQueue.Count - 1;
                Logger.Debug("CurrentSongIndex is outside of SongQueue range, so setting it to {0}", CurrentSongIndex);
            }
            else
            {
                Logger.Debug("Setting CurrentSongIndex to our index of {0}", index);
                CurrentSongIndex = index;
            }

            Logger.Debug("Starting playback of our new song");
            MainPlayer.PlaybackState = MediaState.Play;
            if(MainPlayer.PlaybackState != MediaState.Play) 
                NextCmd.Execute(null);
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
                        Logger.Debug("Toggling playback from stopped state");
                        if (_oldQueueSelection >= 0
                            && _oldQueueSelection < SongQueue.Count)
                            CurrentSongIndex = _oldQueueSelection;
                        else
                            CurrentSongIndex = 0;
                        Logger.Debug("Updated CurrentSongIndex to {0}", CurrentSongIndex);
                        MainPlayer.PlaybackState = MediaState.Play;
                        return;
                    }

                    Logger.Debug("Toggling playback from {0}", MainPlayer.PlaybackState);
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
                    Logger.Debug("Stopping media playback and setting CurrentSongIndex to -1");
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

                    Logger.Debug("Toggling preview playback");
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
                    Logger.Info("Playing next song at index {0}", CurrentSongIndex+1);
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
                    Logger.Info("Playing previous song at index {0}", CurrentSongIndex-1);
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
                    Logger.Info("Switching playback to queue selection at {0}", QueueSelectionIndex);
                    Logger.Debug("Stopping playback of current song, if any");
                    MainPlayer.PlaybackState = MediaState.Stop;
                    CurrentSongIndex = QueueSelectionIndex;
                    Logger.Debug("Starting playback of new song");
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
                    Logger.Info("Updating BPM to newly calculated value of {0}", (int)BpmCalc.Value);
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
                Logger.Debug("Changing queue selection index to {0}", value);
                _queueSelectionIndex = value;
                OnPropertyChanged();
            }
        }

        public IList SelectedSongs
        {
            get { return _selectedSongs; }
            set
            {
                Logger.Debug("SelectedSongs changed");
                _selectedSongs = value;
                OnPropertyChanged();
            }
        }

        public TagEditorVm TagEditorVm
        {
            get { return _tagEditorVm; }
            set
            {
                Logger.Debug("TagEditorVm changed");
                _tagEditorVm = value;
                OnPropertyChanged();
            }
        }

        public Song SelectedInGrid
        {
            get { return _selectedInGrid; }
            set
            {
                Logger.Debug("SelectedInGrid changed");
                _selectedInGrid = value;
                OnPropertyChanged();
            }
        }

        public Song SelectedInQueue
        {
            get { return _selectedInQueue; }
            set
            {
                Logger.Debug("SelectedInQueue changed to {0}", value);
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
                    Logger.Debug("Incrementing preview play count");
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
                    Logger.Debug("Decrementing preview play count");
                    PreviewSong.PlayCount--;
                });
            }
        }

        private AllTagEditor _allTagEditor;
        public ICommand OpenTagEditorCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (_allTagEditor == null)
                    {
                        Logger.Debug("Openning the tag editor");
                        _allTagEditor = new AllTagEditor
                        {
                            DataContext = new CategoryTagEditorVm(App.SongDb)
                        };
                        _allTagEditor.Closed += (sender, args) => _allTagEditor = null;
                        _allTagEditor.Show();
                    }
                    else
                    {
                        Logger.Debug("Focusing on the tag editor");
                        _allTagEditor.Focus();
                    }
                });
            }
        }

        private async Task AddDirAndFiles(string dir)
        {
            Logger.Debug("Adding directory and files from {0}", dir);
            foreach (var filename in Directory.GetFiles(dir)
                .Where(filename => App.SongDb.IsFiletypeSupported(filename)))
            {
                Logger.Debug("Inserting file {0}", filename);
                App.SongDb.InsertFromFile(filename);
            }

            foreach (var subdir in Directory.EnumerateDirectories(dir))
            {
                Logger.Debug("Recursively adding directory and files in {0}", subdir);
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
