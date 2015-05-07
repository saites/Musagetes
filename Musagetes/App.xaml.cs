using Musagetes.DataObjects;
using System.Threading.Tasks;
using System.Windows;
using Musagetes.DataAccess;
using Musagetes.Properties;

namespace Musagetes
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : SongDb.IDbReaderWriter
	{
        public static SongDb SongDb { get; private set; }
        public static Settings Configuration { get; private set; }

        public App()
        {
            Configuration = Settings.Default;
            InitializeComponent();
            SongDb = new SongDb(this);
            Task.Run(() => SongDb.ReadDbAsync(Configuration.DbLocation));
        }

	    protected override void OnExit(ExitEventArgs e)
	    {
	        Task.WaitAll(Task.Run(() => SongDb.SaveDbAsync(Configuration.DbLocation)));
            WriteConfiguration();
	        base.OnExit(e);
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
	        Configuration.Save();
	    }
	}
}