using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var data = new DataObject();
            data.SetData(typeof(IList), SelectedItems);

            DragDrop.DoDragDrop(this, data, DragDropEffects.Copy | DragDropEffects.Move);
        }
    }
}
