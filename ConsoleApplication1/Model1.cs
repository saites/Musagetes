namespace ConsoleApplication1
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class Model1 : DbContext
    {
        public Model1()
            : base("name=Model1")
        {
        }

        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<CategoryTag> CategoryTags { get; set; }
        public virtual DbSet<Song> Songs { get; set; }
        public virtual DbSet<SongTag> SongTags { get; set; }
        public virtual DbSet<Tag> Tags { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>()
                .Property(e => e.CateogryName)
                .IsUnicode(false);

            modelBuilder.Entity<Song>()
                .Property(e => e.SongTitle)
                .IsUnicode(false);

            modelBuilder.Entity<Song>()
                .Property(e => e.PrimaryArtist)
                .IsUnicode(false);

            modelBuilder.Entity<Song>()
                .Property(e => e.Location)
                .IsUnicode(false);

            modelBuilder.Entity<Song>()
                .HasMany(e => e.SongTags)
                .WithRequired(e => e.Song)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Tag>()
                .Property(e => e.TagName)
                .IsUnicode(false);

            modelBuilder.Entity<Tag>()
                .HasMany(e => e.SongTags)
                .WithRequired(e => e.Tag)
                .WillCascadeOnDelete(false);
        }
    }
}
