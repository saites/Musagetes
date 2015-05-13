using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Musagetes.Annotations;
using Musagetes.DataObjects;
using MvvmFoundation.Wpf;

namespace Musagetes.ViewModels
{
    class CreateNewTagVm : INotifyPropertyChanged
    {
        private string _tagName;
        private IEnumerable<Category> _categoryList;
        private string _categoryName;
        private Category _assignedCategory;
        private Tag _newTag;

        public bool CreateTagSuccessful
        {
            get { return AssignedCategory != null && NewTag != null; }
        }

        public CreateNewTagVm(string tagName, string catName)
        {
            CategoryList = App.SongDb.Categories;
            CanCreateNewTag = false;
            CategoryName = catName.Trim();
            TagName = tagName.Trim();
        }

        public Action CloseAction { get; set; }
        public ICommand CreateNewTagCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (AssignedCategory == null)
                        CreateCategory();
                    if (NewTag == null)
                        CreateTag();
                    CloseAction();
                });
            }
        }

        private void CreateTag()
        {
            NewTag = new Tag(TagName.Trim(), AssignedCategory);
            App.SongDb.AddTag(NewTag);
        }

        private void CreateCategory()
        {
            AssignedCategory = new Category(CategoryName.Trim());
            App.SongDb.AddCategory(AssignedCategory);
            lock (((ICollection) App.SongDb.Columns).SyncRoot)
            {
                App.SongDb.Columns.Add(
                    new GridColumn(GridColumn.ColumnTypeEnum.Category,
                        AssignedCategory, isVisible: false));
            }
        }

        public bool CanCreateNewTag { get; set; }
        public Tag NewTag
        {
            get { return _newTag; }
            set
            {
                _newTag = value;
                OnPropertyChanged();
                OnPropertyChanged("CreateMessage");
            }
        }

        public Category AssignedCategory
        {
            get { return _assignedCategory; }
            set
            {
                _assignedCategory = value;
                OnPropertyChanged();
                UpdateNewTag();
            }
        }

        private void UpdateNewTag()
        {
            if (AssignedCategory != null && TagName != null)
                NewTag = AssignedCategory.Tags.SingleOrDefault(
                    t => t.TagName.Equals(TagName.Trim(),
                        StringComparison.InvariantCultureIgnoreCase));
            OnPropertyChanged("CreateMessage");
        }

        public string TagName
        {
            get { return _tagName; }
            set
            {
                _tagName = value;
                UpdateNewTag();
                OnPropertyChanged();
            }
        }

        public string CategoryName
        {
            get { return _categoryName; }
            set
            {
                _categoryName = value;
                AssignedCategory = App.SongDb.CategoryDictionary.ContainsKey(_categoryName.Trim())
                    ? App.SongDb.CategoryDictionary[_categoryName.Trim()]
                    : null;
                OnPropertyChanged();
            }
        }

        public IEnumerable<Category> CategoryList
        {
            get { return _categoryList; }
            set
            {
                _categoryList = value;
                OnPropertyChanged();
            }
        }

        public string CreateMessage
        {
            get
            {
                string msg;
                if (NewTag == null)
                {
                    msg = AssignedCategory == null
                        ? "Create New Tag and New Category"
                        : "Create New Tag";
                    CanCreateNewTag = true;
                }
                else
                {
                    msg = "Tag Already Exists In Category";
                    CanCreateNewTag = false;
                }
                if (string.IsNullOrWhiteSpace(TagName)
                    || string.IsNullOrWhiteSpace(CategoryName))
                {
                    msg = "Enter a Tag and Category";
                    CanCreateNewTag = false;
                }
                OnPropertyChanged("CanCreateNewTag");
                return msg;
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
