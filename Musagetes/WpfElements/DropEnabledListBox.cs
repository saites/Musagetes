using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Musagetes.Toolkit;

namespace Musagetes.WpfElements
{
    public class DropEnabledListBox : ListBox
    {
        public Style HighlightStyle
        {
            get { return (Style)GetValue(HighlightStyleProperty); }
            set { SetValue(HighlightStyleProperty, value); }
        }

        public Style InUseStyle
        {
            get { return (Style)GetValue(InUseStyleProperty); }
            set { SetValue(InUseStyleProperty, value); }
        }

        public int InUseIndex
        {
            get { return (int)GetValue(InUseIndexProperty); }
            set { SetValue(InUseIndexProperty, value); }
        }

        public ICommand DropCommand
        {
            get { return (ICommand)GetValue(DropCommandProperty); }
            set { SetValue(DropCommandProperty, value); }
        }

        public static DependencyProperty HighlightStyleProperty =
            DependencyProperty.Register("HighlightStyle", typeof(Style),
            typeof(DropEnabledListBox), new PropertyMetadata(null));

        public static DependencyProperty InUseStyleProperty =
            DependencyProperty.Register("InUseStyle", typeof(Style),
            typeof(DropEnabledListBox), new PropertyMetadata(null));

        public static DependencyProperty InUseIndexProperty =
            DependencyProperty.Register("InUseIndex", typeof(int),
            typeof(DropEnabledListBox), new PropertyMetadata(-1, OnInUseIndexChanged));

        public static DependencyProperty DropCommandProperty =
            DependencyProperty.Register("DropCommand", typeof(ICommand),
            typeof(DropEnabledListBox), new PropertyMetadata(null));

        private Style _oldIndexStyle;
        private ListBoxItem _oldInUseItem;
        private static void OnInUseIndexChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var lb = d as DropEnabledListBox;
            if (lb == null || lb.InUseStyle == null) return;

            if (lb._oldInUseItem != null)
                lb._oldInUseItem.Style = lb._oldIndexStyle;
            lb._oldInUseItem = null;

            var newIndexValue = e.NewValue as int? ?? -1;
            if (newIndexValue == -1) return;
            lb._oldInUseItem = lb.ItemContainerGenerator.ContainerFromIndex(newIndexValue)
                as ListBoxItem;
            if (lb._oldInUseItem == null) return;
            lb._oldIndexStyle = lb._oldInUseItem.Style;
            lb._oldInUseItem.Style = lb.InUseStyle;
        }

        public DropEnabledListBox()
        {
            //need to update this whenever the layout updates,
            //since, if you the InUseItem, its containers isn't
            //generated immediately
            LayoutUpdated += (sender, args) =>
            {
                OnInUseIndexChanged(this, new DependencyPropertyChangedEventArgs(
                    InUseIndexProperty, -1, InUseIndex));
            };
        }

        private bool _isDragging;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (SelectedItem == null
                || e.LeftButton != MouseButtonState.Pressed) return;
            var data = new DataObject();
            data.SetData(typeof(IList), new List<object>{SelectedItem});
            _isDragging = true;
            DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);
            _isDragging = false;
        }

        private Style _oldHighlightStyle;
        private ListBoxItem _highlightedItem;
        protected override void OnPreviewDragOver(DragEventArgs e)
        {
            base.OnPreviewDragOver(e);
            if (HighlightStyle == null) return;
            var newItem = ((UIElement)e.OriginalSource).TryFindParent<ListBoxItem>();
            // ReSharper disable once PossibleUnintendedReferenceComparison
            if (newItem == _highlightedItem) return;
            if (_highlightedItem != null) _highlightedItem.Style = _oldHighlightStyle;
            _highlightedItem = newItem;
            if (_highlightedItem == null) return;
            _oldHighlightStyle = _highlightedItem.Style;
            _highlightedItem.Style = HighlightStyle;
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            if (e.Data.GetData(typeof (IList)) == null)
                e.Effects = DragDropEffects.None;
        }

        protected override void OnPreviewDragLeave(DragEventArgs e)
        {
            base.OnPreviewDragLeave(e);
            var newItem = ((UIElement)e.OriginalSource).TryFindParent<ListBoxItem>();
            // ReSharper disable once PossibleUnintendedReferenceComparison
            if (newItem == _highlightedItem) return;
            if (_highlightedItem != null) _highlightedItem.Style = _oldHighlightStyle;
            _highlightedItem = null;
            _oldHighlightStyle = null;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if (_highlightedItem != null) _highlightedItem.Style = _oldHighlightStyle;
            if (DropCommand == null
                || e.Data.GetData(typeof (IList)) == null) return;

            if (_isDragging)
            {
                e.Data.SetData("SelectedIndex", SelectedIndex);
                e.Effects = DragDropEffects.Move;
            }

            var dropIndex = _highlightedItem == null 
                ? Items.Count : ItemContainerGenerator.IndexFromContainer(_highlightedItem);
            e.Data.SetData("DropIndex", dropIndex);

            var saveSelected = SelectedIndex;

            if(DropCommand.CanExecute(e.Data))
                DropCommand.Execute(e.Data);

            var rearrange = _isDragging;
                _isDragging = false;

            if (InUseIndex < 0 || InUseIndex >= Items.Count) return;

            if (rearrange)
            {
                if (saveSelected > InUseIndex && dropIndex <= InUseIndex)
                    InUseIndex++;
                else if (saveSelected < InUseIndex && dropIndex > InUseIndex)
                    InUseIndex--;
                else if (saveSelected == InUseIndex)
                {
                    if (dropIndex > saveSelected) InUseIndex = dropIndex - 1;
                    else InUseIndex = dropIndex;
                }
            }
            else if (InUseIndex >= dropIndex)
            {
                var newItems = (IList) e.Data.GetData(typeof (IList));
                InUseIndex += newItems.Count;
            }
        }
    }
}
