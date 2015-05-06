using Westwind.Utilities.Configuration;

namespace Musagetes.DataObjects 
{
    public class Configuration : AppConfiguration
    {
        public int MainPlayerDeviceNum { get; set; }
        public int SecondaryPlayerDeviceNum { get; set; }
        public string DbLocation { get; set; }
        public bool UpdatePlaycountOnPreview { get; set; }

        public Configuration()
        {
            MainPlayerDeviceNum = -1;
            SecondaryPlayerDeviceNum = -1;
            DbLocation = Constants.DbLocation;
            UpdatePlaycountOnPreview = false;
        }
    }
}