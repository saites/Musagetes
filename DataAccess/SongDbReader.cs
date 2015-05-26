using System;
using System.Collections;
using System.Collections.Generic;
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
        private readonly SortedList<int, GridColumn> _columns 
            = new SortedList<int, GridColumn>();
        public bool ReadSuccessful { get; private set; }

        public SongDbReader(string filename, SongDb songDb)
        {
            Filename = filename;
            SongDb = songDb;
            ReadSuccessful = true;
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
                    reader.ConfirmElement(Constants.Db.MusagetesSongDb);
                    await reader.ReadAsync();

                    Logger.Debug("Looking for Columns element");
                    reader.ConfirmElement(Constants.Db.Columns);
                    if (reader.IsEmptyElement)
                    {
                        Logger.Debug("Columns element is empty");
                        await reader.ReadAsync();
                    }
                    else
                    {
                        await ReadColumnsAsync(reader);
                        reader.ReadEndElement();
                    }

                    Logger.Debug("Looking for CategoryTags element");
                    reader.ConfirmElement(Constants.Db.CategoryTags);
                    if (reader.IsEmptyElement)
                    {
                        Logger.Debug("CategoryTags is empty");
                        await reader.ReadAsync();
                    }
                    else
                    {
                        await ReadCategoryTagsAsync(reader);
                        reader.ReadEndElement();
                    }
                    AddColumns();
                    SongDb.CategoriesRead.Set();

                    Logger.Debug("Looking for Songs element");
                    reader.ConfirmElement(Constants.Db.Songs);
                    if (reader.IsEmptyElement)
                        Logger.Debug("Songs is empty");
                    else
                        await ReadSongsAsync(reader);
                }
                Logger.Debug("Done reading XML");
            }
            catch(Exception e)
            {
                Logger.Error("Unable to read XML: {0}", e.Message);
                Logger.Error("Stack: {0}", e.StackTrace);
                Console.WriteLine(e.Message);
                ReadSuccessful = false;
                throw;
            }
        }

        private void AddColumns()
        {
            lock (((ICollection) SongDb.Columns).SyncRoot)
            {
                foreach (var col in _columns.Values)
                    SongDb.Columns.Add(col);
            }
        }

        private async Task ReadColumnsAsync(XmlReader reader)
        {
            await reader.ReadAsync();
            while (reader.LocalName.Equals(Constants.Db.Column))
            {
                Logger.Debug("Reading a column");
                var header = reader.GetAttribute(Constants.Db.Header);

                var type = reader.GetAttribute(Constants.Db.Type);
                GridColumn.ColumnTypeEnum cType;
                if (!Enum.TryParse(type, out cType))
                {
                    Logger.Error("Unable to read column type for {0}", header);
                    await reader.ReadAsync();
                    continue;
                }

                var displayStr = reader.GetAttribute(Constants.Db.Display);
                bool display;
                if (!bool.TryParse(displayStr, out display))
                {
                    Logger.Error("Unable to read display for {0}", header);
                    await reader.ReadAsync();
                    continue;
                }

                var orderStr = reader.GetAttribute(Constants.Db.Order);
                int order;
                if (!int.TryParse(orderStr, out order))
                {
                    Logger.Error("Unable to read order for {0}", header);
                    await reader.ReadAsync();
                    continue;
                }

                Logger.Debug("Adding {0} column {1} at {2}",
                    type, header, orderStr);

                if (_columns.ContainsKey(order))
                {
                    Logger.Error("Column {0} and {1} both have order {2}",
                        header, _columns[order].Header, order);
                    await reader.ReadAsync();
                    continue;
                }

                var binding = reader.GetAttribute(Constants.Db.Binding);
                switch (cType)
                {
                    case GridColumn.ColumnTypeEnum.BasicText:
                        _columns.Add(order,
                            new GridColumn(header: header, binding: binding,
                                isVisible: display));
                        break;
                    case GridColumn.ColumnTypeEnum.Bpm:
                        _columns.Add(order,
                            new GridColumn(GridColumn.ColumnTypeEnum.Bpm, header: header,
                                isVisible: display, binding: binding));
                        break;
                }

                await reader.ReadAsync();
            }
        }

        private async Task ReadSongsAsync(XmlReader reader)
        {
            await reader.ReadAsync();
            while (reader.LocalName.Equals(Constants.Db.Song))
            {
                Logger.Debug("Reading a song");

                UInt32 id;
                if (!UInt32.TryParse(reader.GetAttribute(Constants.Db.Id), out id))
                {
                    Logger.Debug("Could not read song id");
                    await reader.ReadAsync();
                    continue;
                }

                if (reader.IsEmptyElement)
                {
                    Logger.Debug("Song element is empty");
                    await reader.ReadAsync();
                    continue;
                }

                await reader.ReadAsync();
                if (reader.IsEndElement(Constants.Db.Song))
                {
                    Logger.Debug("Song element is followed by end element");
                    reader.ReadEndElement();
                    continue;
                }

                reader.ConfirmElement(Constants.Db.SongTitle);
                var title = await reader.TryGetContentAsync();

                reader.ConfirmElement(Constants.Db.Location);
                var location = await reader.TryGetContentAsync();

                reader.ConfirmElement(Constants.Db.Milliseconds);
                int milliseconds;
                if(!int.TryParse(await reader.TryGetContentAsync(), out milliseconds))
                    Logger.Error("Song {0} has a missing or unreadable millisecond value", title);

                reader.ConfirmElement(Constants.Db.PlayCount);
                uint playCount;
                if(!uint.TryParse(await reader.TryGetContentAsync(), out playCount))
                    Logger.Error("Song {0} has a missing or unreadable playcount", title);

                reader.ConfirmElement(Constants.Db.Bpm);
                var guess = Convert.ToBoolean(reader.GetAttribute(Constants.Db.Guess));
                int bpmValue;
                if(!int.TryParse(await reader.TryGetContentAsync(), out bpmValue))
                    Logger.Error("Song {0} has a missing or unreadable BPM value", title);

                var song = new Song(title, location, milliseconds, new Bpm(bpmValue, guess), 
                    SongDb, playCount, id);
                SongDb.AddSong(song);

                reader.ConfirmElement(Constants.Db.Tags);
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
            while (reader.LocalName.Equals(Constants.Db.Tag))
            {
                UInt32 id;
                if (!UInt32.TryParse(await reader.TryGetContentAsync(), out id))
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
            while (reader.LocalName.Equals(Constants.Db.Category))
            {
                var cat = new Category(reader.GetAttribute(Constants.Db.Name));
                Logger.Debug("Reading category {0}", cat.CategoryName);
                if (reader.IsEmptyElement)
                {
                    Logger.Debug("Category element is empty");
                    await ReadTagsAsync(reader, cat);
                    continue;
                }

                var displayStr = reader.GetAttribute(Constants.Db.Display);
                bool display;
                if (!bool.TryParse(displayStr, out display))
                {
                    Logger.Error("Unable to read display for {0}", cat.CategoryName);
                    await reader.ReadAsync();
                    continue;
                }

                var orderStr = reader.GetAttribute(Constants.Db.Order);
                int order;
                if (!int.TryParse(orderStr, out order))
                {
                    Logger.Error("Unable to read order for {0}", cat.CategoryName);
                    await reader.ReadAsync();
                    continue;
                }

                _columns.Add(order,
                        new GridColumn(GridColumn.ColumnTypeEnum.Category,
                            isVisible: display, cateogry: cat));

                await ReadTagsAsync(reader, cat);
                SongDb.AddCategory(cat);
                reader.ReadEndElement();
            } 
        }

        private async Task ReadTagsAsync(XmlReader reader, Category cat)
        {
            await reader.ReadAsync();
            while (reader.LocalName.Equals(Constants.Db.Tag))
            {
                Logger.Debug("Reading tag");

                UInt32 id;
                if (!UInt32.TryParse(reader.GetAttribute(Constants.Db.Id), out id))
                {
                    Logger.Debug("Could not read tag id");
                    await reader.ReadAsync();
                    continue;
                }

                var name = reader.GetAttribute(Constants.Db.Name);
                var t = new Tag(name, cat, id);
                SongDb.AddTag(t);

                if (!reader.IsEmptyElement)
                    await reader.ReadAsync();
                await reader.ReadAsync();
            } 
        }
    }
}
