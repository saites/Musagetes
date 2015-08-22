using System.Collections.Generic;
using System.ComponentModel;

namespace Musagetes.DataObjects
{
    public class Tag : INotifyPropertyChanged
    {
        public static class TagId
        {
            private static uint _nextTagId;
            private static readonly object TagIdLock = new object();

            public static uint GetNextTagId()
            {
                lock (TagIdLock)
                {
                    var retval = _nextTagId;
                    _nextTagId++;
                    return retval;
                }
            }

            public static bool UpdateTagId(uint value)
            {
                lock (TagIdLock)
                {
                    if (_nextTagId > value) 
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

        public string TagName
        {
            get { return _tagName; }
            set
            {
                _tagName = value;
                OnPropertyChanged("TagName");
            }
        }

        private readonly object _categoryLock = new object();
        Category _category;
        private string _tagName;

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
                OnPropertyChanged("Category");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public delegate void TagDeletedEventHandler(Tag deletedTag);
        public event TagDeletedEventHandler TagDeleted;
        public void TriggerTagDeleted()
        {
            if (TagDeleted != null) TagDeleted(this);
        }
    }
}
