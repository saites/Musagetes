using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using Musagetes.Annotations;

namespace Musagetes.WpfElements
{
    /// <summary>
    /// IComparer capable of using an IValueConverter to 
    /// convert the objects to be compared before comparing them
    /// </summary>
    class ConverterComparer: IComparer
    {
        private readonly IValueConverter _converter;
        private readonly ListSortDirection _direction;
        private readonly object _parameter;
        private readonly Type _targetType;
        private readonly CultureInfo _cultureInfo;

        /// <summary>
        /// ConverterComparer constructor
        /// </summary>
        /// <param name="converter">Converter to use to convert the objects before comparing them</param>
        /// <param name="direction">Sort direction for the comparison; defaults to Ascending</param>
        /// <param name="parameter">Parameter that will be passed to the converter</param>
        /// <param name="targetType">Target type that will be passed to the converter; defaults to typeof(string)</param>
        /// <param name="cultureInfo">Cultural info to pass to converter; defaults to CurrentThread.CurrentCulture</param>
        public ConverterComparer([NotNull] IValueConverter converter, 
            ListSortDirection? direction, [CanBeNull] object parameter,
            Type targetType = null, CultureInfo cultureInfo = null)
        {
            _converter = converter;
            _direction = direction ?? ListSortDirection.Ascending;
            _parameter = parameter;
            _targetType = targetType ?? typeof(string);
            _cultureInfo = cultureInfo ?? System.Threading.Thread.CurrentThread.CurrentCulture;
        }

        /// <summary>
        /// Compares objects x and y after passing them through a converter
        /// </summary>
        /// <param name="x">the first object to compare</param>
        /// <param name="y">the second object to compare</param>
        /// <returns>
        /// a negative int32 if x is less than y,
        /// zero if x equals y,
        /// a positive int32 if x is greater than y
        /// </returns>
        public int Compare(object x, object y)
        {
            var transx = _converter.Convert(x, _targetType, _parameter, _cultureInfo);
            var transy = _converter.Convert(y, _targetType, _parameter, _cultureInfo);
            return _direction == ListSortDirection.Ascending
                ? Comparer.Default.Compare(transx, transy)
                : Comparer.Default.Compare(transx, transy)*-1;
    }
}
}
