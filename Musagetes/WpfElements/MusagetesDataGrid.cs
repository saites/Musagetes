using System;
using System.Collections;
using System.Runtime.Remoting.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Musagetes.Toolkit;
using System.Windows.Controls.Primitives;

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
            typeof(MusagetesDataGrid), new PropertyMetadata(null, OnContextPopupChanged));

        private static void OnContextPopupChanged(DependencyObject d, 
            DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null) return;
            var cp = ((Popup) e.NewValue);
            var grid = (MusagetesDataGrid) d;
            cp.Closed += (sender, ev) => { grid.PreviewTarget = null; };
        }

        public static readonly DependencyProperty PreviewTargetProperty =
            DependencyProperty.Register("PreviewTarget", typeof (object),
                typeof (MusagetesDataGrid), new FrameworkPropertyMetadata(null));

        protected bool IsSelecting { get; private set; }
        protected bool MouseMoved { get; private set; }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                HandleLeftButtonDown(e);
            }
        }

        private void HandleLeftButtonDown(MouseButtonEventArgs e)
        {
            MouseMoved = false;

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

            if (e.ChangedButton == MouseButton.Right 
                && e.RightButton == MouseButtonState.Released)
            {
                HandleRightButtonUp(e);
            }

            if (e.ChangedButton == MouseButton.Left
                && e.LeftButton == MouseButtonState.Released)
            {
                HandleLeftButtonUp(e);
            }
        }

        private void HandleLeftButtonUp(MouseButtonEventArgs e)
        {
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

        private void HandleRightButtonUp(MouseButtonEventArgs e)
        {
            if (ContextPopup == null) return;

            PreviewTarget = ((UIElement)e.OriginalSource).TryFindParent<DataGridRow>();
            if (PreviewTarget == null)
            {
                ContextPopup.IsOpen = false;
            }
            else
            {
                ContextPopup.PlacementTarget = (DataGridRow) PreviewTarget;
                PreviewTarget = ((DataGridRow) PreviewTarget).Item;
                ContextPopup.IsOpen = true;
                ContextPopup.Focus();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            MouseMoved = true;
            if (e.LeftButton != MouseButtonState.Pressed
                || IsSelecting
                || UiHelper.IsMouseOverScrollbar(this, e.GetPosition(this))) 
                return;
            
            var data = new DataObject(SelectedItems);
            data.SetData(typeof(IList), SelectedItems);

            DragDrop.DoDragDrop(this, data, DragDropEffects.Copy);
        }
    }
}
