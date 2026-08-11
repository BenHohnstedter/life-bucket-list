using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Repositories;

namespace LifeBucketList.Data;

public sealed class SqliteCategoryRepository : ICategoryRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteCategoryRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        using var connection = _connectionFactory.OpenConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, SortOrder FROM Categories ORDER BY SortOrder";

        var categories = new List<Category>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            categories.Add(new Category
            {
                Id = Guid.Parse(reader.GetString(0)),
                Name = reader.GetString(1),
                SortOrder = reader.GetInt32(2),
            });
        }

        return categories;
    }
}
