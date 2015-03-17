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
using MvvmFoundation.Wpf;

namespace Musagetes
{
    class CategoryDisplayOptionsVm : INotifyPropertyChanged
    {
        public ListCollectionView Columns { get; private set; }
        public DataGridColumn SelectedCategory { get; set; }

        public CategoryDisplayOptionsVm(IList<DataGridColumn> columns)
        {
            Columns = new ListCollectionView((IList) columns);
            Columns.SortDescriptions.Add(new SortDescription("Visibility", ListSortDirection.Ascending));
            Columns.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));
        }

        private static readonly VisibilityToBooleanConverter V2B = 
            new VisibilityToBooleanConverter();
        public ICommand ToggleCmd
        {
            get
            {
                return new RelayCommand(() =>
                {
                    SelectedCategory.Visibility = (Visibility)V2B.ConvertBack(
                        !(bool)V2B.Convert(SelectedCategory.Visibility, 
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
