using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Musagetes.WpfElements
{
    public class DropListCommand<T> : ICommand
    {
        private readonly IList<T> _list;

        public DropListCommand(IList<T> list)
        {
            _list = list;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var dataObj = parameter as IDataObject;
            if (dataObj == null) return;

            var selectedIndex = dataObj.GetData("SelectedIndex") as int? ?? -1;
            var dropIndex = dataObj.GetData("DropIndex") as int? ?? -1;

            if (selectedIndex != -1 && selectedIndex == dropIndex) return;

            if (selectedIndex >= 0 && selectedIndex < _list.Count)
            {
                _list.RemoveAt(selectedIndex);
                if(selectedIndex < dropIndex) dropIndex--;
            }

            if(dropIndex < 0 || dropIndex > _list.Count) 
                dropIndex = Math.Max(_list.Count, 0); 

            var l = dataObj.GetData(typeof(IList));
            if (l == null) return;
            foreach (var s in ((IList) l).OfType<T>())
                _list.Insert(dropIndex++, s);
        }

        public event EventHandler CanExecuteChanged;
    }
}
