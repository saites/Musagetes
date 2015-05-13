using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Musagetes.Annotations;
using MvvmFoundation.Wpf;

namespace Musagetes.Toolkit
{
    public class BpmTapper : INotifyPropertyChanged
    {
        public const int Resolution = 5;
        public bool IsTapping { get { return _watch.IsRunning; } }

        public long Value
        {
            get
            {
                if (_intervals == null) return 0;
                var goodVals = _intervals.Where(i => i != 0).ToList();
                if(!goodVals.Any()) return 0;
                return (ConversionFactor * goodVals.Count) / goodVals.Sum();
            } 
        }

        private readonly Stopwatch _watch = new Stopwatch();
        private int _position;
        private const long ConversionFactor = 60*1000; // (sec / min) * (ms / sec)
        private long[] _intervals; 

        public ICommand RegisterTap
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (!IsTapping)
                    {
                        StartTapping.Execute(null);
                        return;
                    }

                    _intervals[_position] = _watch.ElapsedMilliseconds;
                    _position = (_position + 1) % Resolution;
                    OnPropertyChanged("Value");
                    _watch.Restart();
                });
            }
        }

        public ICommand StartTapping
        {
            get
            {
                return new RelayCommand(() =>
                {
                    _watch.Restart();
                    _intervals = new long[Resolution]; 
                    OnPropertyChanged("IsTapping");
                });
            }
        }

        public ICommand StopTapping
        {
            get
            {
                return new RelayCommand(() =>
                {
                    _watch.Stop();
                    OnPropertyChanged("IsTapping");
                });
            }
        }

        public ICommand ToggleTapping
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (IsTapping) StopTapping.Execute(null);
                    else StartTapping.Execute(null);
                });
            }
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
