using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Musagetes.Annotations;
using Musagetes.DataAccess;
using Musagetes.DataObjects;
using Musagetes.WpfElements;
using MvvmFoundation.Wpf;
using NAudio.Wave;

namespace Musagetes.ViewModels
{
    public class CategoryDisplayOptionsVm : INotifyPropertyChanged
    {
        public ListCollectionView DisplayColumns { get; private set; }
        public ObservableCollection<CategoryWrapper> AllCategories { get; private set; }
        public DataGridColumn SelectedColumn { get; set; }
        public Dictionary<int,string> Devices { get; private set; }
        public bool UpdatePlaycountOnPreview { get; set; }
        public int UpdateTime { get; set; }
        public string MainPlayerDevice {
            get { return Devices[App.Configuration.MainPlayerDeviceNum]; }
            set
            {
                App.Configuration.MainPlayerDeviceNum =
                    Devices.First(d => d.Value.Equals(value)).Key;
            }}

        public string PreviewPlayerDevice {
            get { return Devices[App.Configuration.SecondaryPlayerDeviceNum]; }
            set
            {
                App.Configuration.SecondaryPlayerDeviceNum =
                    Devices.First(d => d.Value.Equals(value)).Key;
            }}

        public IList<Category> DbGroupCategories { get; set; }
        public ObservableCollection<Category> DbAllCategories { get; set; }
        public CategoryDisplayOptionsVm(IList<DataGridColumn> columns,
            ObservableCollection<Category> categories, IList<Category> groupCategories)
        {
            DisplayColumns = new ListCollectionView((IList)columns);
            DisplayColumns.SortDescriptions.Add(new SortDescription("Visibility", ListSortDirection.Ascending));
            DisplayColumns.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));
            DbGroupCategories = groupCategories;
            DbAllCategories = categories;

            AllCategories = new ObservableCollection<CategoryWrapper>();
            foreach (var cat in categories)
                AllCategories.Add(new CategoryWrapper(cat, this));
            AllCategories.CollectionChanged += AllCategoriesCollectionChanged;

            Devices = new Dictionary<int, string> {{-1, "Default Audio Device"}};
            for (var device = 0; device < WaveOut.DeviceCount; device++)
            {
                var info = WaveOut.GetCapabilities(device);
                Devices.Add(device, info.ProductName);
            }
        }

        private bool _reinsert;
        private void AllCategoriesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Category cat;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems == null) return;
                    cat = ((CategoryWrapper) e.NewItems[0]).Category;
                    DbAllCategories.Insert(e.NewStartingIndex, cat);
                    if(_reinsert) AddGroupCategory(cat);
                    _reinsert = false;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems == null) return;
                    var catWrap = ((CategoryWrapper) e.OldItems[0]);
                    cat = ((CategoryWrapper) e.OldItems[0]).Category;
                    DbAllCategories.RemoveAt(e.OldStartingIndex);
                    if (catWrap.IsGrouping)
                    {
                        _reinsert = true;
                        DbGroupCategories.Remove(cat);
                    }
                    break;
            }
        }

        public ICommand MoveCategoriesCmd
        {
            get { return new DropListCommand<CategoryWrapper>(AllCategories); }
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
                DbGroupCategories.Insert((int)insertionPoint, category);
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
