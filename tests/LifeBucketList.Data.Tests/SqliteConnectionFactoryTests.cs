using LifeBucketList.Domain.Models;
using Microsoft.Data.Sqlite;
using Xunit;

namespace LifeBucketList.Data.Tests;

public class SqliteConnectionFactoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"lbl-test-{Guid.NewGuid()}.db");

    [Fact]
    public async Task InitializeAsync_SeedsDefaultCategoriesOnFirstRun()
    {
        var factory = new SqliteConnectionFactory(_dbPath);
        await factory.InitializeAsync();

        var repository = new SqliteCategoryRepository(factory);
        var categories = await repository.GetAllAsync();

        Assert.Equal(DefaultCategories.Names.Count, categories.Count);
        Assert.Equal(DefaultCategories.Names, categories.OrderBy(c => c.SortOrder).Select(c => c.Name));
    }

    [Fact]
    public async Task InitializeAsync_RenamesLegacySpieleCategoryToVideospiele_PreservingIdAndEntries()
    {
        // Simulate a pre-existing database from an earlier app version that still has "Spiele".
        var legacyCategoryId = Guid.NewGuid();
        var legacyEntryId = Guid.NewGuid();
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString()))
        {
            connection.Open();
            var setup = connection.CreateCommand();
            setup.CommandText =
                """
                CREATE TABLE Categories (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, SortOrder INTEGER NOT NULL);
                CREATE TABLE Entries (
                    Id TEXT PRIMARY KEY, CategoryId TEXT NOT NULL, Title TEXT NOT NULL,
                    OccurredOn TEXT NULL, Rating INTEGER NULL, Note TEXT NULL,
                    CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL
                );
                """;
            setup.ExecuteNonQuery();

            var insertCategory = connection.CreateCommand();
            insertCategory.CommandText = "INSERT INTO Categories (Id, Name, SortOrder) VALUES ($id, 'Spiele', 2)";
            insertCategory.Parameters.AddWithValue("$id", legacyCategoryId.ToString());
            insertCategory.ExecuteNonQuery();

            var insertEntry = connection.CreateCommand();
            insertEntry.CommandText =
                "INSERT INTO Entries (Id, CategoryId, Title, CreatedAt, UpdatedAt) VALUES ($id, $categoryId, 'Zelda', $now, $now)";
            insertEntry.Parameters.AddWithValue("$id", legacyEntryId.ToString());
            insertEntry.Parameters.AddWithValue("$categoryId", legacyCategoryId.ToString());
            insertEntry.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("o"));
            insertEntry.ExecuteNonQuery();
        }
        SqliteConnection.ClearAllPools();

        var factory = new SqliteConnectionFactory(_dbPath);
        await factory.InitializeAsync();

        var categories = await new SqliteCategoryRepository(factory).GetAllAsync();
        var renamed = Assert.Single(categories, c => c.Id == legacyCategoryId);
        Assert.Equal(DefaultCategories.VideoGames, renamed.Name);

        var entries = await new SqliteEntryRepository(factory).GetAllAsync();
        var entry = Assert.Single(entries, e => e.Id == legacyEntryId);
        Assert.Equal(legacyCategoryId, entry.CategoryId);
    }

    [Fact]
    public async Task InitializeAsync_ReordersCategoriesFromAnEarlierTabOrder_PreservingIds()
    {
        // Simulate a pre-existing database seeded under the old tab order
        // (Filme, Serien, Videospiele, Reiseziele, Aktivitäten).
        var oldOrder = new[] { "Filme", "Serien", "Videospiele", "Reiseziele", "Aktivitäten" };
        var idsByName = oldOrder.ToDictionary(name => name, _ => Guid.NewGuid());
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString()))
        {
            connection.Open();
            var setup = connection.CreateCommand();
            setup.CommandText = "CREATE TABLE Categories (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, SortOrder INTEGER NOT NULL)";
            setup.ExecuteNonQuery();

            for (var i = 0; i < oldOrder.Length; i++)
            {
                var insert = connection.CreateCommand();
                insert.CommandText = "INSERT INTO Categories (Id, Name, SortOrder) VALUES ($id, $name, $sortOrder)";
                insert.Parameters.AddWithValue("$id", idsByName[oldOrder[i]].ToString());
                insert.Parameters.AddWithValue("$name", oldOrder[i]);
                insert.Parameters.AddWithValue("$sortOrder", i);
                insert.ExecuteNonQuery();
            }
        }
        SqliteConnection.ClearAllPools();

        var factory = new SqliteConnectionFactory(_dbPath);
        await factory.InitializeAsync();

        var categories = await new SqliteCategoryRepository(factory).GetAllAsync();
        Assert.Equal(DefaultCategories.Names, categories.OrderBy(c => c.SortOrder).Select(c => c.Name));
        // Ids must be preserved (same categories, just reordered), not replaced.
        foreach (var (name, originalId) in idsByName)
        {
            Assert.Contains(categories, c => c.Name == name && c.Id == originalId);
        }
    }

    [Fact]
    public async Task InitializeAsync_AddsNewlyIntroducedFixedCategory_ToAnExistingDatabaseMissingIt()
    {
        // Simulate a database created by an earlier app version that only had the original 5 fixed
        // categories (i.e. before "Konzerte" was added) — the table is non-empty, so the old
        // "seed only if empty" logic would never have added the new category.
        var oldNames = DefaultCategories.Names.Where(n => n != DefaultCategories.Concerts).ToArray();
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString()))
        {
            connection.Open();
            var setup = connection.CreateCommand();
            setup.CommandText = "CREATE TABLE Categories (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, SortOrder INTEGER NOT NULL)";
            setup.ExecuteNonQuery();

            for (var i = 0; i < oldNames.Length; i++)
            {
                var insert = connection.CreateCommand();
                insert.CommandText = "INSERT INTO Categories (Id, Name, SortOrder) VALUES ($id, $name, $sortOrder)";
                insert.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
                insert.Parameters.AddWithValue("$name", oldNames[i]);
                insert.Parameters.AddWithValue("$sortOrder", i);
                insert.ExecuteNonQuery();
            }
        }
        SqliteConnection.ClearAllPools();

        var factory = new SqliteConnectionFactory(_dbPath);
        await factory.InitializeAsync();

        var categories = await new SqliteCategoryRepository(factory).GetAllAsync();
        Assert.Equal(DefaultCategories.Names, categories.OrderBy(c => c.SortOrder).Select(c => c.Name));
        Assert.Contains(categories, c => c.Name == DefaultCategories.Concerts);
    }

    [Fact]
    public async Task InitializeAsync_RemovesLeftoverCustomCategory_AndItsEntries_KeepingFixedCategoriesIntact()
    {
        var factory = new SqliteConnectionFactory(_dbPath);
        await factory.InitializeAsync(); // seeds the 5 fixed categories

        // Simulate a leftover custom category from before categories became fixed (e.g. "Test",
        // created by the user back when the app still allowed custom categories).
        Guid strayCategoryId;
        Guid strayEntryId;
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString()))
        {
            connection.Open();
            strayCategoryId = Guid.NewGuid();
            strayEntryId = Guid.NewGuid();

            var insertCategory = connection.CreateCommand();
            insertCategory.CommandText = "INSERT INTO Categories (Id, Name, SortOrder) VALUES ($id, 'Test', 99)";
            insertCategory.Parameters.AddWithValue("$id", strayCategoryId.ToString());
            insertCategory.ExecuteNonQuery();

            var insertEntry = connection.CreateCommand();
            insertEntry.CommandText =
                "INSERT INTO Entries (Id, CategoryId, Title, CreatedAt, UpdatedAt) VALUES ($id, $categoryId, 'irgendwas', $now, $now)";
            insertEntry.Parameters.AddWithValue("$id", strayEntryId.ToString());
            insertEntry.Parameters.AddWithValue("$categoryId", strayCategoryId.ToString());
            insertEntry.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("o"));
            insertEntry.ExecuteNonQuery();
        }
        SqliteConnection.ClearAllPools();

        // Next app startup: the migration must purge the stray category and its entries.
        await factory.InitializeAsync();

        var categories = await new SqliteCategoryRepository(factory).GetAllAsync();
        Assert.Equal(DefaultCategories.Names, categories.OrderBy(c => c.SortOrder).Select(c => c.Name));
        Assert.DoesNotContain(categories, c => c.Id == strayCategoryId);

        var entries = await new SqliteEntryRepository(factory).GetAllAsync();
        Assert.DoesNotContain(entries, e => e.Id == strayEntryId);
    }

    [Fact]
    public async Task InitializeAsync_DoesNotDuplicateSeedOnSecondCall()
    {
        var factory = new SqliteConnectionFactory(_dbPath);
        await factory.InitializeAsync();
        await factory.InitializeAsync();

        var repository = new SqliteCategoryRepository(factory);
        var categories = await repository.GetAllAsync();

        Assert.Equal(DefaultCategories.Names.Count, categories.Count);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
