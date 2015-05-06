using System.Collections.Generic;

namespace Musagetes.DataObjects
{
    public class Tag
    {
        public static class TagId
        {
            private static uint _nextTagId;
            private static readonly object TagIdLock = new object();

            public static uint GetNextTagId()
            {
                uint retval;
                lock (TagIdLock) retval = _nextTagId++;
                return retval;
            }

            public static bool UpdateTagId(uint value)
            {
                lock (TagIdLock)
                {
                    if (_nextTagId >= value) 
                        return false;
                    _nextTagId = value + 1;
                    return true;
                }
            }
        }

        public Tag(string tagName, Category category, uint? tagId = null)
        {
            TagName = tagName;
            Category = category;
            if (tagId != null) TagId.UpdateTagId(tagId.Value);
            Id = tagId ?? TagId.GetNextTagId();
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

        public uint Id { get; private set; }

        public string TagName { get; set; }

        private readonly object _categoryLock = new object();
        Category _category;
        public Category Category
        {
            get { return _category; }
            set
            {
                if (_category != null)
                    lock (_categoryLock)
                        _category.RemoveTag(this);
                lock (_categoryLock)
                {
                    _category = value;
                    _category.AddTag(this);
                }
            }
        }

        public HashSet<Song> Songs { get; set; }
    }
}
