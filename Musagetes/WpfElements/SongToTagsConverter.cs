using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Musagetes.DataObjects;

namespace Musagetes.WpfElements
{
    class SongToTagsConverter : IValueConverter
    {
        public SongDb Db { get; set; }
        public Category Category { get; set; }

        public SongToTagsConverter(SongDb songDb, Category category = null)
        {
            Db = songDb;
            Category = category;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            var song = value as Song;
            var cat = Category ?? parameter as Category;

            if (song == null
                || !(targetType == typeof (string) 
                    || targetType == typeof(object))
                || Db == null)
                return value;

            var tags = Db.SongTagDictionary[song];

            return cat == null
                ? string.Join(", ", tags.Select(t => t.TagName))
                : string.Join(", ", cat.Tags.Intersect(tags).Select(t => t.TagName));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
