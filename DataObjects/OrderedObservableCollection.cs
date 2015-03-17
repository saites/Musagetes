using System.Collections.ObjectModel;

namespace Musagetes.DataObjects
{
    public class OrderedObservableCollection<T> : ObservableCollection<T>
    {
        public void MoveUp(T item)
        {
            var index = IndexOf(item);
            if (index == 0) return;
            Move(index, index - 1);
        }

        public void MoveDown(T item)
        {
            var index = IndexOf(item);
            if (index == Count - 1) return;
            Move(index, index + 1);
        }
    }
}
