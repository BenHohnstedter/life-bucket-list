using LifeBucketList.Domain.Models;
using Microsoft.Data.Sqlite;
using Xunit;

namespace LifeBucketList.Data.Tests;

public class SqliteEntryRepositoryTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"lbl-test-{Guid.NewGuid()}.db");
    private SqliteEntryRepository _entries = null!;
    private SqliteCategoryRepository _categories = null!;
    private Guid _categoryId;

    public async Task InitializeAsync()
    {
        var factory = new SqliteConnectionFactory(_dbPath);
        await factory.InitializeAsync();
        _entries = new SqliteEntryRepository(factory);
        _categories = new SqliteCategoryRepository(factory);

        var existing = await _categories.GetAllAsync();
        _categoryId = existing[0].Id;
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        return Task.CompletedTask;
    }

    private Entry MakeEntry(string title = "Inception") => new()
    {
        Id = Guid.NewGuid(),
        CategoryId = _categoryId,
        Title = title,
        OccurredOn = new DateOnly(2024, 3, 15),
        Rating = 5,
        Note = "Grandios",
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task AddAsync_ThenGetAllAsync_RoundTripsAllFields()
    {
        var entry = MakeEntry();

        await _entries.AddAsync(entry);
        var all = await _entries.GetAllAsync();

        var loaded = Assert.Single(all);
        Assert.Equal(entry.Id, loaded.Id);
        Assert.Equal(entry.CategoryId, loaded.CategoryId);
        Assert.Equal(entry.Title, loaded.Title);
        Assert.Equal(entry.OccurredOn, loaded.OccurredOn);
        Assert.Equal(entry.Rating, loaded.Rating);
        Assert.Equal(entry.Note, loaded.Note);
    }

    [Fact]
    public async Task AddAsync_AllowsNullOptionalFields()
    {
        var entry = MakeEntry();
        entry.OccurredOn = null;
        entry.Rating = null;
        entry.Note = null;

        await _entries.AddAsync(entry);
        var loaded = Assert.Single(await _entries.GetAllAsync());

        Assert.Null(loaded.OccurredOn);
        Assert.Null(loaded.Rating);
        Assert.Null(loaded.Note);
    }

    [Fact]
    public async Task UpdateAsync_ChangesFields()
    {
        var entry = MakeEntry();
        await _entries.AddAsync(entry);

        entry.Title = "Interstellar";
        entry.Rating = 4;
        await _entries.UpdateAsync(entry);
        var loaded = Assert.Single(await _entries.GetAllAsync());

        Assert.Equal("Interstellar", loaded.Title);
        Assert.Equal(4, loaded.Rating);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntry()
    {
        var entry = MakeEntry();
        await _entries.AddAsync(entry);

        await _entries.DeleteAsync(entry.Id);

        Assert.Empty(await _entries.GetAllAsync());
    }

}
