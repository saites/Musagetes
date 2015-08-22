using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace Musagetes.WpfElements
{
    class ConverterComparer: IComparer
    {
        private readonly IValueConverter _converter;
        private readonly ListSortDirection _direction;
        private readonly object _parameter;
        private readonly Type _targetType;
        private readonly CultureInfo _cultureInfo;

        public ConverterComparer(IValueConverter converter, 
            ListSortDirection? direction, object parameter,
            Type targetType = null, CultureInfo cultureInfo = null)
        {
            _converter = converter;
            _direction = direction ?? ListSortDirection.Ascending;
            _parameter = parameter;
            _targetType = targetType ?? typeof(string);
            _cultureInfo = cultureInfo ?? System.Threading.Thread.CurrentThread.CurrentCulture;
        }

        public int Compare(object x, object y)
        {
            var transx = _converter.Convert(x, _targetType, _parameter, _cultureInfo);
            var transy = _converter.Convert(y, _targetType,_parameter, _cultureInfo);
            return _direction == ListSortDirection.Ascending
                ? Comparer.Default.Compare(transx, transy)
                : Comparer.Default.Compare(transx, transy)*-1;
    }
}
}
