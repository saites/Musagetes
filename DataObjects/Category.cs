using System.Collections.Generic;
namespace Musagetes.DataObjects
{

    public class Category
    {
        public string CategoryName { get; private set; }
        public IReadOnlyCollection<Tag> Tags
        {
            get { return new List<Tag>(_tags).AsReadOnly(); }
        }

        private readonly HashSet<Tag> _tags =
            new HashSet<Tag>();
        private readonly Dictionary<string, Tag> _tagDictionary
            = new Dictionary<string,Tag>();

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
                if (_tags.Contains(t)
                    || _tagDictionary.ContainsKey(t.TagName))
                    return false;
                _tags.Add(t);
                _tagDictionary.Add(t.TagName, t);
                return true;
        }

        public bool RemoveTag(Tag t)
        {
                if (!_tags.Contains(t)
                    || !_tagDictionary.ContainsKey(t.TagName))
                    return false;
                _tags.Remove(t);
                _tagDictionary.Remove(t.TagName);
                return true;
        }

        public Tag this[string s]
        {
            get
            {
                if (s == null) return null;
                return _tagDictionary.ContainsKey(s)
                    ? _tagDictionary[s]
                    : null;
            }
        }
    }
}
