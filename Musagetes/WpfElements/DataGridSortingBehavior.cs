using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Musagetes.WpfElements
{
    public static class DataGridSortingBehavior
    {
        public static readonly DependencyProperty UseBindingToSortProperty =
            DependencyProperty.RegisterAttached("UseBindingToSort", typeof (bool),
                typeof (DataGridSortingBehavior), new PropertyMetadata(GridSortPropertyChanged));

        public static void SetUseBindingToSort(DependencyObject element, bool value)
        {
            element.SetValue(UseBindingToSortProperty, value);
        }

        private static void GridSortPropertyChanged(DependencyObject elem, DependencyPropertyChangedEventArgs e)
        {
            var grid = elem as DataGrid;
            if (grid == null) return;

            if ((bool) e.NewValue)
            {
                grid.Sorting += GridSorting;
            }
            else
            {
                grid.Sorting -= GridSorting;
            }
        }

        private static void GridSorting(object sender, DataGridSortingEventArgs e)
        {
            var column = e.Column as DataGridTextColumn;
            var grid = sender as DataGrid;
            if (column == null || grid == null) return;

            var listCollectionView = (ListCollectionView) CollectionViewSource.GetDefaultView(grid.ItemsSource);

            var binding = column.Binding as Binding;
            if (binding == null || binding.Converter == null)
            {
                listCollectionView.CustomSort = null;
                return;
            }

            e.Handled = true;
            var converter = binding.Converter;
            var parameter = binding.ConverterParameter;
            
            //at first, column sort direction will be null, so we'll
            //set it to ascending; each time after that, we'll toggle it
            column.SortDirection = column.SortDirection != ListSortDirection.Ascending
                ? ListSortDirection.Ascending
                : ListSortDirection.Descending;

            listCollectionView.CustomSort = new ConverterComparer(converter, column.SortDirection, parameter);
        }
    }
}
