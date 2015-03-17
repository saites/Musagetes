namespace ConsoleApplication2
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("SongTag")]
    public partial class SongTag
    {
        public int Id { get; set; }

        public int TagId { get; set; }

        public int SongId { get; set; }

        public virtual Song Song { get; set; }

        public virtual Tag Tag { get; set; }
    }
}
