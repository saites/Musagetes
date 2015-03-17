using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Musagetes.DataObjects
{
    public class GridColumn : INotifyPropertyChanged
    {
        private bool _isVisible;

        public enum ColumnTypeEnum
        {
            BasicText,
            Bpm,
            Category
        }

        public Category Category { get; private set; }
        public string Header { get; private set; }
        public string Binding { get; private set; }
        public ColumnTypeEnum ColumnType { get; private set; }

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value; 
                OnPropertyChanged("IsVisible");
            }
        }

        public GridColumn(ColumnTypeEnum columnType = ColumnTypeEnum.BasicText,
            Category cateogry = null, string header = "", string binding = "",
            bool isVisible = true)
        {
            IsVisible = isVisible;
            Category = cateogry;
            Header = header;
            ColumnType = columnType;
            Binding = binding;

            if (Category != null) ColumnType = ColumnTypeEnum.Category;

            if(Category != null && !string.IsNullOrWhiteSpace(header))
                throw new Exception("Cannot specify a category and a header");
            if(ColumnType == ColumnTypeEnum.BasicText 
                && (string.IsNullOrWhiteSpace(binding)
                    || string.IsNullOrWhiteSpace(header)))
                throw new Exception("BasicText must specify header and binding");
            if(ColumnType == ColumnTypeEnum.Category && Category == null)
                throw new Exception("Column cannot be of type Category if category is null");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
