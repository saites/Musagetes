using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Musagetes.Toolkit;

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

        public bool IsSelecting { get; private set; }
        public bool MouseMoved { get; private set; }
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            MouseMoved = false;
            var row = ((UIElement)e.OriginalSource).TryFindParent<DataGridRow>();
            if (row == null
                || Keyboard.IsKeyDown(Key.LeftCtrl)
                || Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            if (SelectedItems.Contains(row.Item))
            {
                e.Handled = true;
                IsSelecting = false;
            }
            else
            {
                IsSelecting = true;
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);
            IsSelecting = false;
            if (Keyboard.IsKeyDown(Key.LeftCtrl) 
                || Keyboard.IsKeyDown(Key.RightCtrl)
                || MouseMoved) return;
            var row = ((UIElement)e.OriginalSource).TryFindParent<DataGridRow>();
            if (row == null) return;
            SelectedItems.Clear();
            SelectedItems.Add(row.Item);
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            MouseMoved = true;
            if (e.LeftButton != MouseButtonState.Pressed
                || IsSelecting) return;
            
            var data = new DataObject();
            data.SetData(typeof(IList), SelectedItems);

            DragDrop.DoDragDrop(this, data, DragDropEffects.Copy | DragDropEffects.Move);
        }
    }
}
