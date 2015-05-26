using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Musagetes.Annotations;
using Musagetes.DataObjects;

namespace Musagetes.ViewModels
{
    internal class AllTagEditorVm : INotifyPropertyChanged
    {
        private List<Tag> _tagsList;
        private Category _selectedCategory;
        public ObservableCollection<Category> Categories { get; set; }

        public Tag SelectedTag { get; set; }

        public Category SelectedCategory
        {
            get { return _selectedCategory; }
            set
            {
                _selectedCategory = value;
                TagList = value.Tags.ToList();
                OnPropertyChanged();
            }
        }

        public List<Tag> TagList
        {
            get { return _tagsList; }
            set
            {
                _tagsList = value;
                OnPropertyChanged();
            }
        }

        public AllTagEditorVm(ObservableCollection<Category> categories)
        {
            Categories = categories;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
