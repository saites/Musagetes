namespace ConsoleApplication2
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Category")]
    public partial class Category
    {
        public Category()
        {
            Tags = new HashSet<Tag>();
        }

        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string CateogryName { get; set; }

        public virtual ICollection<Tag> Tags { get; set; }
    }
}
