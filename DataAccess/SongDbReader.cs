using System;
using System.Threading.Tasks;
using System.Xml;
using Musagetes.DataObjects;
using NLog;

namespace Musagetes.DataAccess
{
    public class SongDbReader
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        public string Filename { get; private set; }
        public SongDb SongDb { get; private set; }

        public SongDbReader(string filename, SongDb songDb)
        {
            Filename = filename;
            SongDb = songDb;
        }

        public async Task ReadDb()
        {
            var settings = new XmlReaderSettings
            {
                Async = true,
                IgnoreComments = true,
                //IgnoreWhitespace = true
            };

            try
            {
                _logger.Debug("Attempting to create XML read and read file");
                using (var reader = XmlReader.Create(Filename, settings))
                {
                    await ReadToStringAsync(reader, "MusagetesSongDb");
                    await ReadToStringAsync(reader, "CategoryTags");
                    if (!reader.IsEmptyElement) await ReadCategoryTagsAsync(reader);
                    SongDb.CategoriesRead.Set();
                    await ReadToStringAsync(reader, "Songs");
                    if (!reader.IsEmptyElement) await ReadSongsAsync(reader);
                }
                _logger.Debug("Done reading XML");
            }
            catch(Exception e)
            {
                _logger.Error("Unabled to read XML: {0}", e.Message);
                _logger.Error("Stack: {0}", e.StackTrace);
            }
        }

        private async Task ReadSongsAsync(XmlReader reader)
        {
            await ReadToStringAsync(reader, "Song");
            do
            {
                _logger.Debug("Reading a song");
                await ReadToStringAsync(reader, "SongTitle");
                var title = await reader.ReadElementContentAsStringAsync();
                await ReadToStringAsync(reader, "Location");
                var location = await reader.ReadElementContentAsStringAsync();
                await ReadToStringAsync(reader, "Seconds");
                var seconds = (Int32)await reader.ReadElementContentAsAsync(typeof(Int32), null);
                await ReadToStringAsync(reader, "BPM");
                var guess = Convert.ToBoolean(reader.GetAttribute("Guess"));
                var bpmValue = (Int32)await reader.ReadElementContentAsAsync(typeof(Int32), null);

                var song = new Song(title, location, seconds, new BPM(bpmValue, guess), SongDb);
                SongDb.AddSong(song);

                await ReadToStringAsync(reader, "Tags");
                if (!reader.IsEmptyElement) await ReadSongTagsAsync(reader, song);

                await reader.ReadAsync();
                await reader.ReadAsync();
                await reader.ReadAsync();
                await reader.ReadAsync();
            } while (reader.LocalName.Equals("Song"));
        }

        private async Task ReadSongTagsAsync(XmlReader reader, Song song)
        {
            await ReadToStringAsync(reader, "Tag");
            do
            {
                var id = (Int32)await reader.ReadElementContentAsAsync(typeof(Int32), null);
                song.TagSong(SongDb.TagIds[id]);
                await reader.ReadAsync();
            } while (reader.LocalName.Equals("Tag"));
        }

        private async Task ReadCategoryTagsAsync(XmlReader reader)
        {
            await ReadToStringAsync(reader, "Category");
            do
            {
                var cat = new Category(reader.GetAttribute("name"));
                await reader.ReadAsync();
                if (!reader.IsEmptyElement)
                    await ReadTagsAsync(reader, cat);
                SongDb.AddCategory(cat);
                await reader.ReadAsync();
                await reader.ReadAsync();
            } while (reader.LocalName.Equals("Category"));
        }

        private async Task ReadTagsAsync(XmlReader reader, Category cat)
        {
            await ReadToStringAsync(reader, "Tag");
            do
            {
                var id = Convert.ToInt32(reader.GetAttribute("id"));
                var name = reader.GetAttribute("name");
                var t = new Tag(name, cat, id);
                SongDb.AddTag(t);
                await reader.ReadAsync();
                await reader.ReadAsync();
            } while (reader.LocalName.Equals("Tag"));
        }

        private async Task ReadToStringAsync(XmlReader reader, string s)
        {
            while (reader.LocalName != s)
                if (!(await reader.ReadAsync()))
                    throw new FormatException("Invalid XML Format");
        }
    }
}
