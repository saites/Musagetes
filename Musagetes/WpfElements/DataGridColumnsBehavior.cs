using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Musagetes.WpfElements
{
    public class DataGridColumnsBehavior
    {
        public static readonly DependencyProperty BindableColumnsProperty =
            DependencyProperty.RegisterAttached("BindableColumns",
                typeof (ObservableCollection<DataGridColumn>),
                typeof (DataGridColumnsBehavior),
                new UIPropertyMetadata(null, BindableColumnsPropertyChanged));

        private static void BindableColumnsPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var dataGrid = source as DataGrid;
            var columns 
                = e.NewValue as ObservableCollection<DataGridColumn>;
            if (dataGrid == null || columns == null) return;

            dataGrid.Columns.Clear();
            foreach (var column in columns)
                dataGrid.Columns.Add(column);

            BindingOperations.EnableCollectionSynchronization(columns, 
                ((ICollection) columns).SyncRoot);
            BindingOperations.EnableCollectionSynchronization(dataGrid.Columns, 
                ((ICollection)dataGrid.Columns).SyncRoot);

            columns.CollectionChanged += (sender, e2) =>
            {
                Dispatcher.CurrentDispatcher.BeginInvoke((Action) (() =>
                {
                    switch (e2.Action)
                    {
                        case NotifyCollectionChangedAction.Reset:
                            dataGrid.Columns.Clear();
                            foreach (DataGridColumn column in e2.NewItems)
                                dataGrid.Columns.Add(column);
                            break;
                        case NotifyCollectionChangedAction.Add:
                            foreach (DataGridColumn column in e2.NewItems)
                                dataGrid.Columns.Add(column);
                            break;
                        case NotifyCollectionChangedAction.Move:
                            dataGrid.Columns.Move(e2.OldStartingIndex, e2.NewStartingIndex);
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            foreach (DataGridColumn column in e2.OldItems)
                                dataGrid.Columns.Remove(column);
                            break;
                        case NotifyCollectionChangedAction.Replace:
                            dataGrid.Columns[e2.NewStartingIndex] = e2.NewItems[0] as DataGridColumn;
                            break;
                    }
                }));
            };
        }

        public static void SetBindableColumns(DependencyObject element, 
            ObservableCollection<DataGridColumn> value)
        {
            element.SetValue(BindableColumnsProperty, value);
        }

        public static ObservableCollection<DataGridColumn> 
            GetBindableColumns(DependencyObject element)
        {
            return (ObservableCollection<DataGridColumn>) 
                element.GetValue(BindableColumnsProperty);
        }
    }
}