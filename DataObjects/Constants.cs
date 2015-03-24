using System.Collections.Generic;

namespace Musagetes.DataObjects
{
    public static class Constants
    {
        public const string Album = "Album";
        public const string Artist = "Artist";
        public const string CategoryTagsBinding = "CategoryTags[{0}]";
        //public const string DbLocation = @"C:\Users\ajsaites\Documents\Mine\Musagetes\Musagetes\Collaterals\SongDBEmpty.xml";
        public const string DbLocation = @"C:\Users\ajsaites\Documents\Mine\Musagetes\Musagetes\Collaterals\SongDB.xml";
        public const string DisplayedSongs = "DisplayedSongs";
        public const string Genre = "Genre";
        public const string SongQueue = "SongQueue";
        public const string SongTags = "SongTags";
        public const string Uncategorized = "Uncategorized";
        public const string SaveLoc = @"C:\Users\ajsaites\Documents\Mine\Musagetes\Musagetes\Collaterals\SongDBNewlySaved.xml";

        //public const string saveLoc = @"C:\Users\ajsaites\Documents\Musagetes\DB\SongDBTesting.xml";
        //public const string DBLocation = @"C:\Users\saites\Documents\Visual Studio 2012\Projects\Musagetes\DB\SongDBTesting.xml";
        //public const string DBLocation = @"C:\Users\ajsaites\Documents\Musagetes\Musagetes\Musagetes\Collaterals\SongDB.xml";

        public const int DataGridColumnOffset = 5;

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
    }
}
