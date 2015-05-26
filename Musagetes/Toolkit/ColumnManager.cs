using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace Musagetes.Toolkit
{
    public class ColumnManager
    {
        public ObservableCollection<DataGridColumn> Columns { get; set; }

        public ColumnManager()
        {
            Columns = new ObservableCollection<DataGridColumn>();
            BindingOperations.EnableCollectionSynchronization(Columns,
                ((ICollection)Columns).SyncRoot);
        }

        public void AddNewTextColumn(string header, string binding,
            bool isVisible = false, BindingMode mode = BindingMode.OneWay,
            bool notifyOnTargetUpdated = true, double width = 1.0,
            DataGridLengthUnitType widthType = DataGridLengthUnitType.Star)
        {
            var textColumn = DataGridTextColumn(header, binding, isVisible,
                mode, notifyOnTargetUpdated, width, widthType);
            Columns.Add(textColumn);
        }

        private static DataGridTextColumn DataGridTextColumn(string header, string binding, bool isVisible, BindingMode mode,
            bool notifyOnTargetUpdated, double width, DataGridLengthUnitType widthType)
        {
            return new DataGridTextColumn
            {
                Header = header,
                Width = new DataGridLength(width, widthType),
                Visibility = isVisible ? Visibility.Visible : Visibility.Hidden,
                Binding = new Binding(binding)
                {
                    Mode = mode,
                    NotifyOnTargetUpdated = notifyOnTargetUpdated,
                },
                IsReadOnly = false
            };
        }

        public bool RemoveColumn(string categoryName)
        {
            var col = Columns
                .LastOrDefault(c => c.Header != null
                    && c.Header.Equals(categoryName));
            if (col == null) return false;
            Columns.Remove(col);
            return true;
        }

        const string BpmXaml = "<DataTemplate><TextBlock Text=\"{Binding Bpm.Value}\" "
                            + "Style=\"{StaticResource BPMStyle}\"/></DataTemplate>";
        public void AddBpmColumn(string header="BPM", string binding="Bpm.Value", 
            bool display=false)
        {
            var col = new DataGridTemplateColumn
            {
                Header = header,
                Width = new DataGridLength(1.0, DataGridLengthUnitType.Auto),
                CellTemplate = (DataTemplate)XamlReader.Load(
                    new MemoryStream(Encoding.ASCII.GetBytes(BpmXaml)),
                    new ParserContext
                    {
                        XmlnsDictionary =
                        {
                            {"", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"},
                            {"x", "http://schemas.microsoft.com/winfx/2006/xaml"}
                        }
                    }),
                SortMemberPath = binding, 
                Visibility = display ? Visibility.Visible : Visibility.Hidden
            };
            Columns.Add(col);
        }
    }
}
