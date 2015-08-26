using System.Collections.Generic;

namespace Musagetes.DataObjects
{
    public static class Constants
    {
        public const string Album = "Album";
        public const string Artist = "Artist";
        public const string DisplayedSongs = "DisplayedSongs";
        public const string Genre = "Genre";
        public const string SongQueue = "SongQueue";
        public const string SongTags = "SongTags";
        public const string Uncategorized = "Uncategorized";
        public const string SongBinding = "Self";

        public const string DbLocation = "../../../Musagetes/Collaterals/SongDB.xml";
        public const string SaveLoc = DbLocation;

        public const string TimeString = @"mm\:ss";
        
        //@"C:\Users\ajsaites\Documents\Mine\Musagetes\Musagetes\Collaterals\SongDBNewlySaved.xml";
        //public const string saveLoc = @"C:\Users\ajsaites\Documents\Musagetes\DB\SongDBTesting.xml";
        //public const string DBLocation = @"C:\Users\saites\Documents\Visual Studio 2012\Projects\Musagetes\DB\SongDBTesting.xml";
        //public const string DBLocation = @"C:\Users\ajsaites\Documents\Musagetes\Musagetes\Musagetes\Collaterals\SongDB.xml";
        //public const string DbLocation = @"C:\Users\ajsaites\Documents\Mine\Musagetes\Musagetes\Collaterals\SongDBEmpty.xml";
        //public const string DbLocation = @"C:\Users\ajsaites\Documents\Mine\Musagetes\Musagetes\Collaterals\SongDB.xml";

        /* Supported File Types */
        public static readonly string[] SupportedFileTypes = 
        {
            /* mpeg-4 */
            ".3g2", ".3gp2", ".3gp", ".3gpp", ".m4a", ".m4v",
                ".mp4v", ".mp4", ".mov",
            /* mpeg-2 */
            ".m2ts",
            /* asf */
            ".asf", ".wm", ".wmv", ".wma",
            /* adts */
            ".aac", ".adt", ".adts",
            /* mp3, wav, avi */
            ".mp3", ".wav", ".avi", 
            /* ac-3 */
            ".ac3", ".ec3"
        };

        /* DB Constants */
        public static class Db
        {
            public const string MusagetesSongDb = "MusagetesSongDb";

            public const string Columns = "Columns";
            public const string Column = "Column";
            public const string Header = "header";
            public const string Type = "type";
            public const string Display = "display";
            public const string Order = "order";
            public const string Binding = "binding";

            public const string CategoryTags = "CategoryTags";
            public const string Category = "Category";
            public const string Name = "name";
            public const string Id = "id";

            public const string Songs = "Songs";
            public const string Song = "Song";
            public const string SongTitle = "SongTitle";
            public const string Location = "Location";
            public const string Milliseconds = "Milliseconds";
            public const string PlayCount = "PlayCount";
            
            public const string Bpm = "BPM";
            public const string Guess = "Guess";
            
            public const string Tags = "Tags";
            public const string Tag = "Tag";
        }

    }
}
