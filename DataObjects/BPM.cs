namespace Musagetes.DataObjects
{
    public class BPM
    {
        public BPM(int value, bool guess)
        {
            Value = value;
            Guess = guess;
        }
        public bool Guess { get; set; }
        public int Value { get; set; }
    }
}
