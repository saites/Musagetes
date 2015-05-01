using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Musagetes.Toolkit;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Musagetes.WpfElements
{
    public class MusagetesDataGrid : DataGrid
    {
        public MusagetesDataGrid()
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

        public object PreviewTarget
        {
            get { return GetValue(PreviewTargetProperty); }
            set { SetValue(PreviewTargetProperty, value); }
        }

        public Popup ContextPopup 
        {
            get { return (Popup)GetValue(ContextPopupProperty); }
            set { SetValue(ContextPopupProperty, value); }
        }

        public static DependencyProperty SelectedItemsListProperty =
            DependencyProperty.Register("SelectedItemsList", typeof(IList),
            typeof(MusagetesDataGrid), new PropertyMetadata(default(IList)));

        public static DependencyProperty ContextPopupProperty =
            DependencyProperty.Register("ContextPopup", typeof(Popup),
            typeof(MusagetesDataGrid), new PropertyMetadata(null));

        public static DependencyProperty PreviewTargetProperty =
            DependencyProperty.Register("PreviewTarget", typeof (object),
                typeof (MusagetesDataGrid), new FrameworkPropertyMetadata(null, 
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        protected bool IsSelecting { get; private set; }
        protected bool MouseMoved { get; private set; }
        protected bool IsLeftButtonPressed { get; private set; }
        protected bool IsMiddleButtonPressed { get; private set; }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            if (e.MiddleButton == MouseButtonState.Pressed)
                IsMiddleButtonPressed = true;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                HandleLeftButtonDown(e);
            }
        }

        private void HandleLeftButtonDown(MouseButtonEventArgs e)
        {
            IsLeftButtonPressed = true;
            MouseMoved = false;

            if (ContextPopup != null) ContextPopup.IsOpen = false;

            var row = ((UIElement) e.OriginalSource).TryFindParent<DataGridRow>();
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

            if (e.ChangedButton == MouseButton.Middle 
                && e.MiddleButton == MouseButtonState.Released
                && IsMiddleButtonPressed)
            {
                HandleMiddleButtonUp(e);
            }

            if (e.ChangedButton == MouseButton.Left
                && e.LeftButton == MouseButtonState.Released
                && IsLeftButtonPressed)
            {
                HandleLeftButtonUp(e);
            }
        }

        private void HandleLeftButtonUp(MouseButtonEventArgs e)
        {
            IsLeftButtonPressed = false;
            IsSelecting = false;
            if (Keyboard.IsKeyDown(Key.LeftCtrl)
                || Keyboard.IsKeyDown(Key.RightCtrl)
                || MouseMoved) return;
            var row = ((UIElement) e.OriginalSource).TryFindParent<DataGridRow>();
            if (row == null) return;
            SelectedItems.Clear();
            SelectedItems.Add(row.Item);
            e.Handled = true;
        }

        private void HandleMiddleButtonUp(MouseButtonEventArgs e)
        {
            PreviewTarget = ((UIElement)e.OriginalSource).TryFindParent<DataGridRow>();
            if (PreviewTarget != null && ContextPopup != null)
            {
                ContextPopup.PlacementTarget = (DataGridRow)PreviewTarget;
                ContextPopup.IsOpen = true;
                PreviewTarget = ((DataGridRow)PreviewTarget).Item;
            }
            IsMiddleButtonPressed = false;
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
