using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml;
using Musagetes.DataObjects;
using NLog;

namespace Musagetes.DataAccess
{
    public class SongDbWriter
    {
        public string Filename { get; private set; }
        public SongDb SongDb { get; private set; }
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
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
                await writer.WriteStartElementAsync(null, Constants.Db.MusagetesSongDb, null);

                await WriteColumnsAsync(writer);
                await WriteCategoryTagsAsync(writer);
                await WriteSongsAsync(writer);

                await writer.WriteEndElementAsync(); //</MusagetesSongsDb>
                await writer.WriteEndDocumentAsync();
                await writer.FlushAsync();
            }
        }

        private async Task WriteColumnsAsync(XmlWriter writer)
        {
            await writer.WriteStartElementAsync(null, Constants.Db.Columns, null);
            for (var i = 0; i < SongDb.Columns.Count; i++)
            {
                var col = SongDb.Columns[i];
                if (col.ColumnType == GridColumn.ColumnTypeEnum.Category)
                    continue;
                await writer.WriteStartElementAsync(null, Constants.Db.Column, null);
                await writer.WriteAttributeStringAsync(null, Constants.Db.Header, null, col.Header);
                await writer.WriteAttributeStringAsync(null, Constants.Db.Type, null, col.ColumnType.ToString());
                await writer.WriteAttributeStringAsync(null, Constants.Db.Display, null, col.IsVisible.ToString());
                await writer.WriteAttributeStringAsync(null, Constants.Db.Order, null, i.ToString());
                await writer.WriteEndElementAsync(); //</Column>
            }
            await writer.WriteEndElementAsync(); //</Columns>
        }

        private async Task WriteSongsAsync(XmlWriter writer)
        {
            await writer.WriteStartElementAsync(null, Constants.Db.Songs, null);
            foreach (var song in SongDb.Songs)
            {
                await writer.WriteStartElementAsync(null, Constants.Db.Song, null);
                await writer.WriteElementStringAsync(null, Constants.Db.SongTitle, null, song.SongTitle);
                await writer.WriteElementStringAsync(null, Constants.Db.Location, null, song.Location);
                await writer.WriteElementStringAsync(null, Constants.Db.Timespan, null, 
                    song.Seconds.ToString(CultureInfo.InvariantCulture));

                await writer.WriteStartElementAsync(null, Constants.Db.Bpm, null);
                await writer.WriteAttributeStringAsync(null, Constants.Db.Guess, null,
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
            await writer.WriteStartElementAsync(null, Constants.Db.Tags, null);
            foreach (var tag in song.Tags)
            {
                await writer.WriteElementStringAsync(null, Constants.Db.Tag, null, 
                    tag.TagId.ToString(CultureInfo.InvariantCulture));
            }
            await writer.WriteEndElementAsync(); //</Tags>
        }

        private async Task WriteCategoryTagsAsync(XmlWriter writer)
        {
            await writer.WriteStartElementAsync(null, Constants.Db.CategoryTags, null);
            foreach (var cat in SongDb.Categories)
            {
                await writer.WriteStartElementAsync(null, Constants.Db.Category, null);
                await writer.WriteAttributeStringAsync(null, Constants.Db.Name, null, cat.CategoryName);
                var col = SongDb.Columns.FirstOrDefault(c => c.Category == cat);
                if (col != null)
                {
                    await writer.WriteAttributeStringAsync(null, Constants.Db.Display, null, col.IsVisible.ToString());
                    await writer.WriteAttributeStringAsync(null, Constants.Db.Order, null, SongDb.Columns.IndexOf(col).ToString());
                }
                else
                {
                    Logger.Error("Cannot find column for cat {0}", cat.CategoryName);
                    await writer.WriteAttributeStringAsync(null, Constants.Db.Display, null, "False");
                    await writer.WriteAttributeStringAsync(null, Constants.Db.Order, null, int.MaxValue.ToString());
                }
                await WriteTagsAsync(writer, cat);

                await writer.WriteEndElementAsync(); //</Category>
            }
            await writer.WriteEndElementAsync(); //</CategoryTags>
        }

        private async Task WriteTagsAsync(XmlWriter writer, Category cat)
        {
            foreach (var tag in cat.Tags)
            {
                await writer.WriteStartElementAsync(null, Constants.Db.Tag, null);
                await writer.WriteAttributeStringAsync(null, Constants.Db.Id, null, tag.TagId.ToString(CultureInfo.InvariantCulture));
                await writer.WriteAttributeStringAsync(null, Constants.Db.Name, null, tag.TagName);
                await writer.WriteEndElementAsync(); //</Tag>
            }
        }
    }
}
