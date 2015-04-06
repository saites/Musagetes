using System;
using System.Collections;
using System.Windows;
using System.Windows.Input;

namespace Musagetes.WpfElements
{
    public class DropListCommand<T> : ICommand
    {
        private readonly IList _list;

        public DropListCommand(IList list)
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

            var l = dataObj.GetData(typeof(IList));
            if (l == null) return;
            foreach (T s in (IList)l)
                _list.Add(s);
        }

        public event EventHandler CanExecuteChanged;
    }
}
