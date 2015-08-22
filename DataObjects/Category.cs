using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Musagetes.DataObjects
{

    public class Category : INotifyPropertyChanged
    {
        public string CategoryName
        {
            get { return _categoryName; }
            set
            {
                _categoryName = value;
                OnPropertyChanged("CategoryName");
            }
        }

        public IReadOnlyCollection<Tag> Tags
        {
            get { return new List<Tag>(_tags).AsReadOnly(); }
        }

        private readonly HashSet<Tag> _tags =
            new HashSet<Tag>();
        //private readonly Dictionary<string, Tag> _tagDictionary
        //    = new Dictionary<string, Tag>();

        private string _categoryName;

        public override string ToString()
        {
            return CategoryName;
        }

        public Category(string cateogryName)
        {
            CategoryName = cateogryName;
        }

        public bool AddTag(Tag t)
        {
            if (_tags.Contains(t))
                return false;
                //|| _tagDictionary.ContainsKey(t.TagName))
            _tags.Add(t);
            //_tagDictionary.Add(t.TagName, t);

            OnPropertyChanged("Tags");
            return true;
        }

        public bool RemoveTag(Tag t)
        {
            if (!_tags.Contains(t))
                return false;
           //     || !_tagDictionary.ContainsKey(t.TagName))
            _tags.Remove(t);
            //_tagDictionary.Remove(t.TagName);

            OnPropertyChanged("Tags");
            return true;
        }

        public Tag this[string s]
        {
            get
            {
                if (s == null) return null;
                return _tags.FirstOrDefault(t => t.TagName.Equals(s, StringComparison.CurrentCulture));
                /*
                return _tagDictionary.ContainsKey(s)
                    ? _tagDictionary[s]
                    : null;
                */
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
