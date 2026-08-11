using LifeBucketList.Domain.Models;
using Microsoft.Data.Sqlite;
using Xunit;

namespace LifeBucketList.Data.Tests;

public class SqliteCategoryRepositoryTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"lbl-test-{Guid.NewGuid()}.db");
    private SqliteCategoryRepository _repository = null!;

    public async Task InitializeAsync()
    {
        var factory = new SqliteConnectionFactory(_dbPath);
        await factory.InitializeAsync();
        _repository = new SqliteCategoryRepository(factory);
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

    [Fact]
    public async Task GetAllAsync_ReturnsFixedCategoriesInOrder()
    {
        var all = await _repository.GetAllAsync();

        Assert.Equal(DefaultCategories.Names, all.OrderBy(c => c.SortOrder).Select(c => c.Name));
    }
}
