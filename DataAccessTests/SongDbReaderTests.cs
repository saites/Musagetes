using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musagetes.DataAccess;
using Musagetes.DataObjects;

namespace DataAccessTests
{
    [TestClass]
    public class SongDbReaderTests
    {
        [TestMethod]
        public void ConstructorTest()
        {
            const string filename = "Filename";
            var songDb = new SongDb(null);
            var reader = new SongDbReader(filename, songDb);
            Assert.AreEqual(reader.Filename, filename, 
                "Filename doesn't match");
            Assert.AreEqual(reader.SongDb, songDb,
                "SongDb doesn't match");
        }

        [TestMethod]
        public void EmptyDbTest()
        {
            const string filename = "../../../Musagetes/Collaterals/Testing/EmptyDb.xml";
            var songDb = new SongDb(null);
            var reader = new SongDbReader(filename, songDb);
            reader.ReadDb().Wait();
            Assert.IsTrue(reader.ReadSuccessful,
                "Reading threw an exception");
            Assert.IsTrue(songDb.Categories.Count == 0,
                "Categories count was not zero");
            Assert.IsTrue(songDb.Songs.Count == 0,
                "Songs count was not zero");
            Assert.IsTrue(songDb.TagIds.Count == 0,
                "Tag ids is not zero");
            Assert.IsTrue(songDb.Columns.Count != 0,
                "Columns was zero");
        }

        [TestMethod]
        public void BasicReadTest()
        {
            const string filename = "../../../Musagetes/Collaterals/Testing/BasicDb.xml";
            var songDb = new SongDb(null);
            var reader = new SongDbReader(filename, songDb);
            reader.ReadDb().Wait();
            Assert.IsTrue(reader.ReadSuccessful, 
                "Reading threw an exception");
            Assert.IsTrue(songDb.Categories.Count == 4,
                string.Format("Categories count was {0} instead of 4",
                songDb.Categories.Count));
            Assert.IsTrue(songDb.Songs.Count == 3,
                string.Format("Songs count was {0}, not 3",
                songDb.Songs.Count));
            Assert.IsTrue(songDb.TagIds.Count == 4,
                string.Format("Tag ids was {0}, not 4",
                songDb.TagIds.Count));
            Assert.IsTrue(songDb.Columns.Count == 9,
                string.Format("Columns was {0}, not 7",
                songDb.Columns.Count));

            var song = FindSong(songDb, "Mud on the Tires");
            CheckSong(song, 140000, 2, 0, false, 140);
            HasTag(song, "Brad Paisley", songDb.ArtistCategory);
            HasTag(song, "Country", songDb.GenreCategory);

            song = FindSong(songDb, "Throttleneck");
            CheckSong(song, 1000, 2, 0, true, 1);
            HasTag(song, "Brad Paisley", songDb.ArtistCategory);
            HasTag(song, "Country", songDb.GenreCategory);

            song = FindSong(songDb, "When We All Get To Heaven");
            CheckSong(song, 1000, 3, 5, true, 1);
            HasTag(song, "Brad Paisley", songDb.ArtistCategory);
            HasTag(song, "Kenny Chesney", songDb.ArtistCategory);
            HasTag(song, "Rock", songDb.GenreCategory);
        }

        public Song FindSong(SongDb songDb, string title)
        {
            var song = songDb.Songs.FirstOrDefault(
                s => s.SongTitle.Equals(title, 
                StringComparison.InvariantCultureIgnoreCase));
            Assert.IsNotNull(song, 
                string.Format("Couldn't find song {0}", title));
            return song;
        }

        public void CheckSong(Song song, int ms,
            int tagCount, uint playCount, bool guess, int bpm)
        {
            CheckValue(song.Milliseconds, ms, "Milliseconds");
            CheckValue(song.Tags.Count(), tagCount, "TagCount");
            CheckValue(song.PlayCount, playCount, "PlayCount");
            CheckValue(song.Bpm.Guess, guess, "BPM guess");
            CheckValue(song.Bpm.Value, bpm, "BPM Value");
        }

        public void CheckValue(Object o1, Object o2, string name)
        {
            Assert.AreEqual(o1, o2, 
                string.Format("{0} was {1} instead of {2}",
                name, o1, o2));
        }

        public void HasTag(Song s, string tagName, Category c)
        {
            var tag = s.Tags.FirstOrDefault(t =>
                t.TagName.Equals(tagName, 
                StringComparison.InvariantCultureIgnoreCase));
            Assert.IsNotNull(tag, 
                string.Format("song {0} is missing tag {1}",
                s.SongTitle, tagName));
            Assert.IsTrue(tag.Category == c,
                string.Format("Tag {0} is not a {1} tag", 
                tag.TagName, c.CategoryName));
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void MissingFileTest()
        {
            const string filename = "../../../Musagetes/Collaterals/Testing/doesntExist.xml";
            var songDb = new SongDb(null);
            var reader = new SongDbReader(filename, songDb);
            reader.ReadDb().Wait();
            Assert.IsFalse(reader.ReadSuccessful,
                "Reading was successful, though it shouldn't have been");
        }
    }
}
