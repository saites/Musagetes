using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Musagetes.Annotations;
using Musagetes.DataObjects;
using Musagetes.Windows;
using MvvmFoundation.Wpf;

namespace Musagetes.ViewModels
{
    class TagEditorVm : INotifyPropertyChanged
    {
        public static int MaxPredictions = 10;
        public string TagHeader
        {
            get
            {
                return _songs != null
                    && _songs.Cast<Song>().Skip(1).Any()
                    ? "Common Tags:"
                    : "Tags:";
            }
        }

        private int _tagListIndex;
        public int TagListIndex
        {
            get { return _tagListIndex; }
            set
            {
                _tagListIndex = value;
                OnPropertyChanged();
            }
        }

        private int _selectedIndex;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;
                OnPropertyChanged();
            }
        }

        public TagEditorVm()
        {
            Prefix = string.Empty;
            TagList = new ObservableCollection<Tag>();
        }

        private ObservableCollection<Tag> _tagList;
        public ObservableCollection<Tag> TagList
        {
            get
            {
                _tagList.Clear();
                if (_songs == null || _songs.Count == 0) return _tagList;

                foreach (var tag in _songs
                    .Cast<Song>()
                    .Select(s => s.Tags)
                    .Aggregate((tl, t) => tl.Intersect(t)))
                    _tagList.Add(tag);
                return _tagList;
            }
            set
            {
                _tagList = value;
                OnPropertyChanged();
            }
        }

        public ICommand RemoveTagCmd
        {
            get
            {
                return new RelayCommand(()
                => RemoveTag(SelectedOldTag, Songs));
            }
        }

        public void RemoveTag(Tag tag, IList songs)
        {
            if (tag == null || songs.Count < 0) return;
            foreach (Song song in songs)
                song.RemoveTag(tag);
            OnPropertyChanged("TagList");
            OnPropertyChanged("Prediction");
            if (TagList.Any())
                TagListIndex = 0;
        }

        public ICommand CreateNewTagCmd
        {
            get { return new RelayCommand(() => CreateNewTag(Prefix, Songs)); }
        }

        readonly static Regex _parenRegex = new Regex(@".+\s+\(\s*(.+)\s*\)", RegexOptions.Compiled);
        readonly static Regex _tagPrefixRegex = new Regex(@"(.+)\s+\(\s*.+\s*\)", RegexOptions.Compiled);
        private void CreateNewTag(string tagString, IList songs)
        {
            var categoryMatch = _parenRegex.Match(tagString);
            var catName = categoryMatch.Success 
                ? categoryMatch.Groups[1].Captures[0].Value.Trim() 
                : string.Empty;

            var tagNameMatch = _tagPrefixRegex.Match(tagString);
            var tagName = tagNameMatch.Success
                ? tagNameMatch.Groups[1].Captures[0].Value.Trim()
                : tagString.Trim();

            var tagEditorWindow = new CreateNewTagWindow();
            var tagVm = new CreateNewTagVm(tagName, catName)
            {
                CloseAction = tagEditorWindow.Close
            };
            tagEditorWindow.DataContext = tagVm;
            tagEditorWindow.ShowDialog();
            if(tagVm.CreateTagSuccessful)
                AddTag(tagVm.NewTag, songs);
        }

        public ICommand AddTagCmd
        {
            get
            {
                return new RelayCommand(() =>
                    AddTag(SelectedNewTag, Songs));
            }
        }

        public void AddTag(Tag tag, IList songs)
        {
            if (tag == null || songs.Count <= 0) return;
            foreach (Song song in songs)
                song.TagSong(tag);
            OnPropertyChanged("TagList");
            OnPropertyChanged("Prediction");
            if (Prediction.Any())
                SelectedIndex = 0;
        }

        public Tag SelectedNewTag { get; set; }
        public Tag SelectedOldTag { get; set; }

        public IList Songs
        {
            get { return _songs; }
            set
            {
                _songs = value;
                OnPropertyChanged();
                OnPropertyChanged("TagList");
                OnPropertyChanged("TagHeader");
            }
        }
        private string _prefix;
        private IList _songs;

        public string Prefix
        {
            get { return _prefix; }
            set
            {
                _prefix = value;
                OnPropertyChanged();
                OnPropertyChanged("Prediction");
            }
        }

        private IEnumerable<Tag> _predictedTags;
        public IEnumerable<Tag> Prediction
        {
            get
            {
                var upperPrefix = Prefix.ToUpper();
                IEnumerable<Tag> baseTags = App.SongDb.TagIds.Values;
                if (TagList != null) baseTags = baseTags.Except(TagList);
                _predictedTags = baseTags.Where(t => t.TagName.ToUpper()
                        .Contains(upperPrefix)).Take(MaxPredictions);
                return string.IsNullOrWhiteSpace(_prefix) ? null : _predictedTags;
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
