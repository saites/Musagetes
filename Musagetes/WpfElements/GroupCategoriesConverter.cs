using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using Musagetes.DataObjects;

namespace Musagetes.WpfElements
{
    public class GroupCategoriesConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var category = values[0] as Category;
            var groupCategories = values[1] as IList<Category>;
            return groupCategories.Contains(category);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
