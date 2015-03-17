using System.Globalization;
using System.Threading.Tasks;
using System.Xml;

namespace Musagetes.DataObjects
{
    public class SongDbWriter
    {
        public string Filename { get; private set; }
        public SongDb SongDb { get; private set; }
        public SongDbWriter(string filename, SongDb songDb)
        {
            Filename = filename;
            SongDb = songDb;
        }

        public async Task WriteDb()
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Async = true
            };

            using (var writer = XmlWriter.Create(Filename, settings))
            {
                await writer.WriteStartDocumentAsync();
                await writer.WriteStartElementAsync(null, "MusagetesSongDb", null);

                await WriteCategoryTagsAsync(writer);
                await WriteSongsAsync(writer);

                await writer.WriteEndElementAsync(); //</SongsDB>
                await writer.WriteEndDocumentAsync();
                await writer.FlushAsync();
            }
        }

        private async Task WriteSongsAsync(XmlWriter writer)
        {
            await writer.WriteStartElementAsync(null, "Songs", null);
            foreach (var song in SongDb.Songs)
            {
                await writer.WriteStartElementAsync(null, "Song", null);
                await writer.WriteElementStringAsync(null, "SongTitle", null, song.SongTitle);
                await writer.WriteElementStringAsync(null, "Location", null, song.Location);
                await writer.WriteElementStringAsync(null, "Seconds", null, 
                    song.Seconds.ToString(CultureInfo.InvariantCulture));

                await writer.WriteStartElementAsync(null, "BPM", null);
                await writer.WriteAttributeStringAsync(null, "Guess", null,
                        song.Bpm.Guess.ToString(CultureInfo.InvariantCulture));
                await writer.WriteStringAsync(song.Bpm.Value.ToString(CultureInfo.InvariantCulture));
                await writer.WriteEndElementAsync(); //</BPM>

                await WriteSongTagsAsync(writer, song);

                await writer.WriteEndElementAsync(); //</Song>
            }
            await writer.WriteEndElementAsync(); //</Songs>
        }

        private async Task WriteSongTagsAsync(XmlWriter writer, Song song)
        {
            await writer.WriteStartElementAsync(null, "Tags", null);
            foreach (var tag in song.Tags)
            {
                await writer.WriteElementStringAsync(null, "Tag", null, 
                    tag.TagId.ToString(CultureInfo.InvariantCulture));
            }
            await writer.WriteEndElementAsync(); //</Tags>
        }

        private async Task WriteCategoryTagsAsync(XmlWriter writer)
        {
            await writer.WriteStartElementAsync(null, "CategoryTags", null);
            foreach (var cat in SongDb.Categories)
            {
                await writer.WriteStartElementAsync(null, "Category", null);
                await writer.WriteAttributeStringAsync(null, "name", null, cat.CategoryName);

                await WriteTagsAsync(writer, cat);

                await writer.WriteEndElementAsync(); //</Category>
            }
            await writer.WriteEndElementAsync(); //</CategoryTags>
        }

        private async Task WriteTagsAsync(XmlWriter writer, Category cat)
        {
            foreach (var tag in cat.Tags)
            {
                await writer.WriteStartElementAsync(null, "Tag", null);
                await writer.WriteAttributeStringAsync(null, "id", null, tag.TagId.ToString(CultureInfo.InvariantCulture));
                await writer.WriteAttributeStringAsync(null, "name", null, tag.TagName);
                await writer.WriteEndElementAsync(); //</Tag>
            }
        }
    }
}
