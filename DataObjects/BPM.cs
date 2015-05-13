using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Musagetes.DataObjects
{
    public class Bpm : INotifyPropertyChanged
    {
        private bool _guess;
        private int _value;

        /// <summary>
        /// Simple class for managing BPM data
        /// </summary>
        /// <param name="value">Beats per minute</param>
        /// <param name="guess">true indicates this value was inferred</param>
        public Bpm(int value, bool guess)
        {
            Value = value;
            Guess = guess;
        }

        /// <summary>
        /// true indicates this value was automatically generated based on song
        /// data; false indicates it was entered by the user or read from song
        /// tag data
        /// </summary>
        public bool Guess
        {
            get { return _guess; }
            set
            {
                _guess = value; 
                OnPropertyChanged("Guess");
            }
        }

        /// <summary>
        /// the number of beats per minute
        /// </summary>
        public int Value
        {
            get { return _value; }
            set
            {
                _value = value; 
                OnPropertyChanged("Value");
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
