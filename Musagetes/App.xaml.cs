using Musagetes.DataObjects;
using System.Threading.Tasks;
using Musagetes.DataAccess;

namespace Musagetes
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : SongDb.IDbReaderWriter
	{
        public static SongDb SongDb { get; private set; }
        public static Configuration Configuration { get; private set; }

        public App()
        {
            InitializeComponent();
            Configuration = new Configuration();
            Configuration.Initialize();
            SongDb = new SongDb(this);
            Task.Run(() => SongDb.ReadDbAsync(Configuration.DbLocation));
        }

        public async Task WriteDbAsync(string filename, SongDb songDb)
        {
            var writer = new SongDbWriter(filename, songDb);
            await writer.WriteDb();
        }

        public async Task ReadDbAsync(string filename, SongDb songDb)
        {
            var reader = new SongDbReader(filename, songDb);
            await reader.ReadDb();
        }

	    public void WriteConfiguration()
	    {
	        Configuration.Write();
	    }
	}
}