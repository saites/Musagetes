using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Musagetes.Annotations;
using Musagetes.DataObjects;
using MvvmFoundation.Wpf;

namespace Musagetes.ViewModels
{
    internal class CategoryTagEditorVm : INotifyPropertyChanged
    {
        private Category _selectedCategory;
        private string _oldTagName;
        private string _oldCategoryName;
        private Tag _selectedTag;
        public ObservableCollection<Category> Categories { get; set; }

        private readonly SongDb _songDb;

        public CategoryTagEditorVm(SongDb songDb)
        {
            _songDb = songDb;
            Categories = _songDb.Categories;
        }

        public Tag SelectedTag
        {
            get { return _selectedTag; }
            set
            {
                lock (_lock)
                {
                    if (_selectedTag == value) return;
                    if (_selectedTag != null)
                        _selectedTag.PropertyChanged -= VerifyUniqueNames;
                    if (value != null)
                    {
                        _oldTagName = value.TagName;
                        value.PropertyChanged += VerifyUniqueNames;
                    }
                    _selectedTag = value;
                }

                OnPropertyChanged();
            }
        }

        private readonly object _lock = new object();
        public Category SelectedCategory
        {
            get { return _selectedCategory; }
            set
            {
                lock (_lock)
                {
                    if (_selectedCategory == value) return;
                    if (_selectedCategory != null)
                        _selectedCategory.PropertyChanged -= VerifyUniqueNames;
                    if (value != null)
                    {
                        _oldCategoryName = value.CategoryName;
                        value.PropertyChanged += VerifyUniqueNames;
                    }
                    _selectedCategory = value;
                }

                SelectedTag = null;
                OnPropertyChanged();
                OnPropertyChanged("TagList");
            }
        }

        private void VerifyUniqueNames(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CategoryName":
                    VerifyUniqueCategoryName();
                    break;
                case "Tags":
                    OnPropertyChanged("TagList");
                    break;
                case "TagName":
                    VerifyUniqueTagName();
                    break;
            }
        }

        private void VerifyUniqueTagName()
        {
            if (_verifingName) return;
            if (!SelectedCategory.Tags.Any(t => t != SelectedTag && t.TagName == SelectedTag.TagName))
            {
                foreach(var song in App.SongDb.TagSongDictionary[SelectedTag])
                    song.NotifyTagChanged();
                _oldTagName = SelectedTag.TagName;
                return;
            }
            MessageBox.Show("Tag names must be unqiue", "Rename Tag", MessageBoxButton.OK);
            _verifingName = true;
            SelectedTag.TagName = _oldTagName;
            _verifingName = false;
        }

        private bool _verifingName;
        private void VerifyUniqueCategoryName()
        {
            if (_verifingName) return;
            if (!App.SongDb.Categories.Any(c => c != SelectedCategory && c.CategoryName == SelectedCategory.CategoryName))
            {
                _oldCategoryName = SelectedCategory.CategoryName;
                return;
            }
            MessageBox.Show("Category names must be unqiue", "Rename Category", MessageBoxButton.OK);
            _verifingName = true;
            SelectedCategory.CategoryName = _oldCategoryName;
            _verifingName = false;
        }

        public List<Tag> TagList
        {
            get { return _selectedCategory != null ? _selectedCategory.Tags.ToList() : null; }
        }

        public ICommand DeleteCategoryCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (App.SongDb.IsDefaultCategory(SelectedCategory))
                    {
                        MessageBox.Show("Default categories cannot be deleted",
                            "Delete Category", MessageBoxButton.OK);
                        return;
                    }

                    var res = MessageBox.Show(string.Format("Are you sure you want to delete {0} and all its tags?",
                        SelectedCategory.CategoryName), "Delete Category", MessageBoxButton.YesNo);
                    if (res != MessageBoxResult.Yes) return;

                    RemoveTags(SelectedCategory);
                    App.SongDb.RemoveCategory(SelectedCategory);
                    SelectedCategory = null;
                });
            }
        }

        public ICommand DeleteTagCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    var res = MessageBox.Show(string.Format("Are you sure you want to delete {0}?",
                        SelectedTag.TagName), "Delete Tag", MessageBoxButton.YesNo);
                    if (res != MessageBoxResult.Yes) return;

                    RemoveTag(SelectedTag);
                    SelectedTag = null;
                    OnPropertyChanged("TagList");
                });
            }
        }

        private static void RemoveTags(Category cat)
        {
            foreach (var tag in cat.Tags)
            {
                RemoveTag(tag);
            }
        }

        private static void RemoveTag(Tag tag)
        {
            var sourceSet = App.SongDb.TagSongDictionary[tag];
            var songList = new Song[sourceSet.Count];
            App.SongDb.TagSongDictionary[tag].CopyTo(songList);

            foreach (var song in songList)
            {
                App.SongDb.UntagSong(song, tag);
            }
            App.SongDb.TagSongDictionary.Remove(tag);
            tag.Category.RemoveTag(tag); 
            tag.TriggerTagDeleted();
        }

        public ICommand ReorderCategoriesCmd
        {
            get
            {
                return new RelayCommand<object>(param =>
                {
                    var dataObj = param as IDataObject;
                    if (dataObj == null) return;

                    var selectedIndex = dataObj.GetData("SelectedIndex") as int? ?? -1;
                    var dropIndex = dataObj.GetData("DropIndex") as int? ?? -1;

                    if ((selectedIndex != -1 && selectedIndex == dropIndex)
                        || dropIndex < 0 || dropIndex >= _songDb.Categories.Count) return;

                    var l = dataObj.GetData(typeof(IList)) as IList;
                    if (l == null || l.Count == 0) return;

                    var source = l[0];
                    if (source == null) return;
                    var target = _songDb.Categories.ElementAt(dropIndex);
                    if (source is Category && source != target)
                    {
                        MergeCategories(target, (Category)source);
                    }
                    else if (source is Tag && !target.Tags.Contains(source))
                    {
                        MoveTags(target, l);
                    }
                });
            }
        }

        private class TagPair
        {
            public readonly Tag Source;
            public readonly Tag Target;

            public TagPair(Tag target, Tag source)
            {
                Source = source;
                Target = target;
            }
        }

        private void MoveTags(Category target, IEnumerable tags)
        {
            //TODO: this check takes too long; make it faster
            var targetTags = target.Tags;
            var tagList = tags.Cast<Tag>().ToList();
            var matchingTags = (from tag in tagList
                                from tag2 in targetTags.Where(t => t.TagName.Equals(tag.TagName)) 
                                select new TagPair(tag2, tag)).ToList();

            if (matchingTags.Any())
            {
               var res = MessageBox.Show(string.Format("Both categories have {0}: {1}. Combine them?",
                        matchingTags.Count() == 1 ? "this tag" : "these tags",
                        string.Join(", ", matchingTags.Select(t => t.Source.TagName))),
                        "Merge Tags", MessageBoxButton.OKCancel);
                if (res != MessageBoxResult.OK) return;

                foreach (var tp in matchingTags)
                {
                    MergeTags(tp.Target, tp.Source);
                }
            }

            var modifiedSongs = new HashSet<Song>();
            foreach (var t in tagList.Where(tag => !matchingTags.Select(tp => tp.Source).Contains(tag)))
            {
                t.Category = target;
                modifiedSongs.UnionWith(App.SongDb.TagSongDictionary[t]);
            }

            foreach(var s in modifiedSongs)
                s.NotifyTagChanged();

            OnPropertyChanged("TagList");
        }

        private static void MergeTags(Tag target, Tag source)
        {
            //make a local copy, since we'll be removing songs from the list as we go
            var sourceSet = App.SongDb.TagSongDictionary[source];
            var songList = new Song[sourceSet.Count];
            App.SongDb.TagSongDictionary[source].CopyTo(songList);

            foreach (var song in songList) 
            {
                App.SongDb.UntagSong(song, source);
                App.SongDb.TagSong(song, target);
            }
            App.SongDb.TagSongDictionary.Remove(source);
            source.Category.RemoveTag(source);
        }

        private void MergeCategories(Category target, Category source)
        {
            if (App.SongDb.IsDefaultCategory(source))
            {
                MessageBox.Show(string.Format("Cannot merge default category {0}", 
                    source.CategoryName), "Merge Categories", MessageBoxButton.OK);
                return;
            }

            var res = MessageBox.Show(string.Format("Merge {0} into {1}?",
                            source, target), "Merge Categories", MessageBoxButton.OKCancel);
            if (res != MessageBoxResult.OK) return;

            MoveTags(target, source.Tags);
            App.SongDb.RemoveCategory(source);
            OnPropertyChanged("TagList");
        }

        public Func<bool> CanEditFunc
        {
            get
            {
                return () =>
                {
                    if (!App.SongDb.IsDefaultCategory(SelectedCategory)) return true;
                    MessageBox.Show("Default category names cannot be changed", 
                        "Rename Category", MessageBoxButton.OK);
                    return false;
                };
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
