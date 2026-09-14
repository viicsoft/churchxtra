using ChurchAI.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChurchAI.Infrastructure.Data;

public class BibleDbContext : DbContext
{
    public DbSet<Translation> Translations { get; set; } = null!;
    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Verse> Verses { get; set; } = null!;
    public DbSet<Hymn> Hymns { get; set; } = null!;
    public DbSet<SpokenPhrase> SpokenPhrases { get; set; } = null!;
    public DbSet<MediaItem> MediaItems { get; set; } = null!;
    public DbSet<LowerThirdItem> LowerThirds { get; set; } = null!;

    public BibleDbContext(DbContextOptions<BibleDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<Translation>()
            .HasMany(t => t.Books)
            .WithOne(b => b.Translation)
            .HasForeignKey(b => b.TranslationId);

        modelBuilder.Entity<Book>()
            .HasMany(b => b.Verses)
            .WithOne(v => v.Book)
            .HasForeignKey(v => v.BookId);

        // Indexes for faster lookups
        modelBuilder.Entity<Verse>()
            .HasIndex(v => new { v.BookId, v.Chapter, v.VerseNumber });
            
        modelBuilder.Entity<Book>()
            .HasIndex(b => new { b.TranslationId, b.Name });

        modelBuilder.Entity<SpokenPhrase>()
            .HasIndex(sp => sp.NormalizedPhrase);
    }
}
