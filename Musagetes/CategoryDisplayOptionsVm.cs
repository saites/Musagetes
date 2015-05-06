using System.ComponentModel;
using System.Runtime.CompilerServices;
using Musagetes.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Musagetes.DataAccess;
using Musagetes.DataObjects;
using Musagetes.WpfElements;
using MvvmFoundation.Wpf;

namespace Musagetes
{
    public class CategoryDisplayOptionsVm : INotifyPropertyChanged
    {
        public ListCollectionView DisplayColumns { get; private set; }
        public IList<CategoryWrapper> AllCategories { get; private set; }
        public DataGridColumn SelectedColumn { get; set; }
        public Category SelectedCategory { get; set; }

        public IList<Category> DbGroupCategories { get; set; }
        public IList<Category> DbAllCategories { get; set; } 
        public CategoryDisplayOptionsVm(IList<DataGridColumn> columns,
            IList<Category> categories, IList<Category> groupCategories)
        {
            DisplayColumns = new ListCollectionView((IList)columns);
            DisplayColumns.SortDescriptions.Add(new SortDescription("Visibility", ListSortDirection.Ascending));
            DisplayColumns.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));
            DbGroupCategories = groupCategories;
            DbAllCategories = categories;

            AllCategories = new List<CategoryWrapper>();
            foreach(var cat in categories)
                AllCategories.Add(new CategoryWrapper(cat, this));
        }

        public ICommand MoveCategoriesCmd
        {
            get
            {
                return new DropListCommand<CategoryWrapper>(AllCategories); 
            }
        }

        public class CategoryWrapper : INotifyPropertyChanged
        {
            public Category Category { get; set; }
            CategoryDisplayOptionsVm Vm { get; set; }
            public CategoryWrapper(Category cat, CategoryDisplayOptionsVm vm)
            {
                Category = cat;
                Vm = vm;
            }

            public bool IsGrouping
            {
                get { return Vm.DbGroupCategories.Contains(Category); }
                set
                {
                    if (value)
                        Vm.AddGroupCategory(Category);
                    else
                        Vm.DbGroupCategories.Remove(Category);
                    OnPropertyChanged();
                }
            }

            public override string ToString()
            {
                return Category.ToString();
            }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void AddGroupCategory(Category category)
        {
            var idx = DbAllCategories.IndexOf(category);
            var insertionPoint = DbGroupCategories.FirstIndex(
                item => DbAllCategories.IndexOf(item) > idx);
            if (insertionPoint == null)
                DbGroupCategories.Add(category);
            else
                DbGroupCategories.Insert((int) insertionPoint, category);
        }

        private static readonly VisibilityToBooleanConverter V2B =
            new VisibilityToBooleanConverter();
        public ICommand ToggleVisibleCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    SelectedColumn.Visibility = (Visibility)V2B.ConvertBack(
                        !(bool)V2B.Convert(SelectedColumn.Visibility,
                            typeof(bool), null, null),
                        typeof(Visibility), null, null);
                });
            }
        }

        public Action CloseAction { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
