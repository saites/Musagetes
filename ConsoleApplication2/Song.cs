namespace ConsoleApplication2
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Song")]
    public partial class Song
    {
        public Song()
        {
            SongTags = new HashSet<SongTag>();
        }

        public int SongId { get; set; }

        [Required]
        public string SongTitle { get; set; }

        public string PrimaryArtist { get; set; }

        public int Seconds { get; set; }

        [Column(TypeName = "text")]
        [Required]
        public string Location { get; set; }

        public int BPM { get; set; }

        public bool BPMGuess { get; set; }

        public virtual ICollection<SongTag> SongTags { get; set; }
    }
}
