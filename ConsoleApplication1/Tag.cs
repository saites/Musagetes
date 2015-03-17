namespace ConsoleApplication1
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Tag")]
    public partial class Tag
    {
        public Tag()
        {
            SongTags = new HashSet<SongTag>();
        }

        public int TagId { get; set; }

        [Required]
        [StringLength(255)]
        public string TagName { get; set; }

        public int? CategoryId { get; set; }

        public virtual Category Category { get; set; }

        public virtual ICollection<SongTag> SongTags { get; set; }
    }
}
