using System;
using System.Threading.Tasks;
using System.Xml;
using Musagetes.DataObjects;
using NLog;

namespace Musagetes.DataAccess
{
    public class SongDbReader
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
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
                IgnoreWhitespace = true
            };

            try
            {
                Logger.Debug("Attempting to create XML reader and read file");
                using (var reader = XmlReader.Create(Filename, settings))
                {
                    Logger.Debug("Looking for MusagetesSongDb element");
                    await reader.ReadAsync();
                    await reader.ReadAsync();
                    reader.ConfirmElement("MusagetesSongDb");
                    await reader.ReadAsync();

                    Logger.Debug("Looking for CategoryTags element");
                    reader.ConfirmElement("CategoryTags");
                    if (reader.IsEmptyElement)
                    {
                        Logger.Debug("CategoryTags is empty");
                        await reader.ReadAsync();
                    }
                    else
                    {
                        reader.ConfirmElement("CategoryTags");
                        await ReadCategoryTagsAsync(reader);
                        reader.ReadEndElement();
                    }
                    SongDb.CategoriesRead.Set();

                    Logger.Debug("Looking for Songs element");
                    reader.ConfirmElement("Songs");
                    if (reader.IsEmptyElement)
                        Logger.Debug("Songs is empty");
                    else
                        await ReadSongsAsync(reader);
                }
                Logger.Debug("Done reading XML");
            }
            catch(Exception e)
            {
                Logger.Error("Unabled to read XML: {0}", e.Message);
                Logger.Error("Stack: {0}", e.StackTrace);
            }
        }

        

        private async Task ReadSongsAsync(XmlReader reader)
        {
            await reader.ReadAsync();
            while (reader.LocalName.Equals("Song"))
            {
                Logger.Debug("Reading a song");
                if (reader.IsEmptyElement)
                {
                    Logger.Debug("Song element is empty");
                    await reader.ReadAsync();
                    continue;
                }

                await reader.ReadAsync();
                if (reader.IsEndElement("Song"))
                {
                    Logger.Debug("Song element is followed by end element");
                    reader.ReadEndElement();
                    continue;
                }

                reader.ConfirmElement("SongTitle");
                var title = await reader.TryGetContentAsync();

                reader.ConfirmElement("Location");
                var location = await reader.TryGetContentAsync();

                reader.ConfirmElement("Seconds");
                long seconds;
                if(!long.TryParse(await reader.TryGetContentAsync(), out seconds))
                    Logger.Error("Song {0} has a missing or unreadable timespan", title);

                reader.ConfirmElement("BPM");
                var guess = Convert.ToBoolean(reader.GetAttribute("Guess"));
                Int32 bpmValue;
                if(!Int32.TryParse(await reader.TryGetContentAsync(), out bpmValue)); 
                    Logger.Error("Song {0} has a missing or unreadable BPM value", title);

                var song = new Song(title, location, seconds, new BPM(bpmValue, guess), SongDb);
                SongDb.AddSong(song);

                reader.ConfirmElement("Tags");
                if (reader.IsEmptyElement)
                {
                    await reader.ReadAsync();
                }
                else
                {
                    await ReadSongTagsAsync(reader, song);
                    reader.ReadEndElement();
                }

                reader.ReadEndElement();
            } 
        }

        private async Task ReadSongTagsAsync(XmlReader reader, Song song)
        {
            await reader.ReadAsync();
            while (reader.LocalName.Equals("Tag"))
            {
                Int32 id;
                if (!Int32.TryParse(await reader.TryGetContentAsync(), out id))
                {
                    Logger.Error("Song {0} has empty or unreadable tag id", song.SongTitle);
                    continue;
                }
                if (SongDb.TagIds.ContainsKey(id))
                    song.TagSong(SongDb.TagIds[id]);
                else
                    Logger.Error("Song {0} trying to add missing tag id {1}", song.SongTitle, id);
            } 
        }

        private async Task ReadCategoryTagsAsync(XmlReader reader)
        {
            await reader.ReadAsync();
            while (reader.LocalName.Equals("Category"))
            {
                var cat = new Category(reader.GetAttribute("name"));
                Logger.Debug("Reading category {0}", cat.CategoryName);
                if (reader.IsEmptyElement)
                {
                    Logger.Debug("Category element is empty");
                    await ReadTagsAsync(reader, cat);
                    continue;
                }

                await ReadTagsAsync(reader, cat);
                SongDb.AddCategory(cat);
                reader.ReadEndElement();
            } 
        }

        private async Task ReadTagsAsync(XmlReader reader, Category cat)
        {
            await reader.ReadAsync();
            while (reader.LocalName.Equals("Tag"))
            {
                Logger.Debug("Reading tag");

                Int32 id;
                if (!Int32.TryParse(reader.GetAttribute("id"), out id))
                {
                    Logger.Debug("Could not read tag id");
                    await reader.ReadAsync();
                    continue;
                }

                var name = reader.GetAttribute("name");
                var t = new Tag(name, cat, id);
                SongDb.AddTag(t);

                if (!reader.IsEmptyElement)
                    await reader.ReadAsync();
                await reader.ReadAsync();
            } 
        }
    }
}
