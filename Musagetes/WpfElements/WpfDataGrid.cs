using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Musagetes.WpfElements
{
    public class WpfDataGrid : DataGrid
    {
        public WpfDataGrid()
        {
            SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItemsList = SelectedItems;
        }

        public IList SelectedItemsList
        {
            get { return (IList)GetValue(SelectedItemsListProperty); }
            set { SetValue(SelectedItemsListProperty, value); }
        }

        public static DependencyProperty SelectedItemsListProperty =
            DependencyProperty.Register("SelectedItemsList", typeof(IList),
            typeof(WpfDataGrid), new PropertyMetadata(default(IList)));
    }
}
