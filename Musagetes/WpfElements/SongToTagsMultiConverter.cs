using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Musagetes.DataObjects;

namespace Musagetes.WpfElements
{
    /// <summary>
    /// Like the SongtToTagsConverter, this can be used to
    /// retrieve tags for a song from the database; however,
    /// since IValueConverter's ConverterParameter is not a
    /// bindable dependency property, the multiconverter is
    /// much easier to use in XAML. The IValueConverter is
    /// a lot easier to use in code, where the dynamic bindings
    /// to Categories are created (hence, the two classes)
    /// </summary>
    public class SongToTagsMultiConverter : IMultiValueConverter
    {
        private SongToTagsConverter _sttConverter;

        public SongToTagsMultiConverter()
        {
        }

        public SongToTagsMultiConverter(SongDb songDb)
        {
            _sttConverter = new SongToTagsConverter(songDb);
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null) return null;
            if (_sttConverter == null) 
                _sttConverter = GetSongDb(values);

            var song = values[0] as Song;
            var category = values[1] as Category;

            return _sttConverter.Convert(song, typeof(string), category, culture);
        }

        private SongToTagsConverter GetSongDb(object[] values)
        {
            if (values.Count() != 3) throw new Exception("No SongDb is provided");
            return new SongToTagsConverter(values[2] as SongDb);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); 
        }
    }
}
