using System.Linq;
using System.Threading.Tasks;
using ChurchAI.Core.Entities;
using ChurchAI.Infrastructure.Data;
using ChurchAI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Xunit;

namespace ChurchAI.Tests;

public class HymnRepositoryTests
{
    private (BibleDbContext Context, SqliteConnection Connection) CreateInMemoryDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<BibleDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new BibleDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }

    [Fact]
    public async Task SeedHymns_PopulatesDatabaseCorrectly()
    {
        var (context, connection) = CreateInMemoryDbContext();
        try
        {
            var seeder = new DatabaseSeeder(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseSeeder>.Instance);

            // Seed
            await seeder.SeedHymnsAsync();

            // Assert
            Assert.True(await context.Hymns.AnyAsync());
        }
        finally
        {
            connection.Close();
        }
    }

    [Fact]
    public async Task MatchHymnByLyrics_ExactAndPartialMatching_ReturnsCorrectHymn()
    {
        var (context, connection) = CreateInMemoryDbContext();
        try
        {
            var repository = new HymnRepository(context);

            // Manually seed a test hymn
            var hymn = new Hymn
            {
                Number = 10,
                Title = "Blessed Assurance",
                Lyrics = "Blessed assurance, Jesus is mine!\nOh, what a foretaste of glory divine!\nHeir of salvation, purchase of God,\nBorn of His Spirit, washed in His blood."
            };
            context.Hymns.Add(hymn);
            await context.SaveChangesAsync();

            // 1. Test exact title matching in input
            var match1 = await repository.MatchHymnByLyricsAsync("blessed assurance");
            Assert.NotNull(match1);
            Assert.Equal(10, match1.Number);

            // 2. Test exact lyric phrase matching (direct substring check)
            var match2 = await repository.MatchHymnByLyricsAsync("foretaste of glory");
            Assert.NotNull(match2);
            Assert.Equal(10, match2.Number);

            // 3. Test sliding word window check (4 consecutive words match with minor surrounding text)
            var match3 = await repository.MatchHymnByLyricsAsync("we sing that heir of salvation purchase of God today");
            Assert.NotNull(match3);
            Assert.Equal(10, match3.Number);

            // 4. Test non-matching lyrics
            var match4 = await repository.MatchHymnByLyricsAsync("some completely unrelated random text");
            Assert.Null(match4);
        }
        finally
        {
            connection.Close();
        }
    }
}
