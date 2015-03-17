using System.Collections.Generic;
namespace Musagetes.DataObjects
{
    public class Tag
    {
        public Tag(string tagName, Category category, int tagId)
        {
            TagName = tagName;
            Category = category;
            TagId = tagId;
            Songs = new HashSet<Song>();
        }

        public override string ToString()
        {
            return TagName +
                   (Category.CategoryName != null
                    && !Category.CategoryName.Equals(Constants.Uncategorized)
                       ? string.Format(" ({0})", Category.CategoryName)
                       : string.Empty);
        }

        public int TagId { get; set; }

        public string TagName { get; set; }

        Category _category;
        public Category Category
        {
            get { return _category; }
            set
            {
                if (_category != null)
                {
                    lock (_category)
                    {
                        _category.RemoveTag(this);
                    }
                }
                _category = value;
                lock (_category)
                {
                    _category.AddTag(this);
                }
            }
        }

        public HashSet<Song> Songs { get; set; }
    }
}
