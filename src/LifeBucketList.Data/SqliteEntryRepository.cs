using System.Globalization;
using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Repositories;
using Microsoft.Data.Sqlite;

namespace LifeBucketList.Data;

public sealed class SqliteEntryRepository : IEntryRepository
{
    private const string DateFormat = "yyyy-MM-dd";

    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteEntryRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Entry>> GetAllAsync()
    {
        using var connection = _connectionFactory.OpenConnection();
        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, CategoryId, Title, OccurredOn, Rating, Note, CoverImageUrl, CountryCode,
                   Venue, RegionCode, IsFestival, CreatedAt, UpdatedAt
            FROM Entries ORDER BY CreatedAt
            """;

        var entries = new List<Entry>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(ReadEntry(reader));
        }

        return entries;
    }

    public async Task AddAsync(Entry entry)
    {
        using var connection = _connectionFactory.OpenConnection();
        var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO Entries (Id, CategoryId, Title, OccurredOn, Rating, Note, CoverImageUrl, CountryCode,
                                  Venue, RegionCode, IsFestival, CreatedAt, UpdatedAt)
            VALUES ($id, $categoryId, $title, $occurredOn, $rating, $note, $coverImageUrl, $countryCode,
                    $venue, $regionCode, $isFestival, $createdAt, $updatedAt)
            """;
        BindParameters(command, entry);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(Entry entry)
    {
        using var connection = _connectionFactory.OpenConnection();
        var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE Entries
            SET CategoryId = $categoryId, Title = $title, OccurredOn = $occurredOn,
                Rating = $rating, Note = $note, CoverImageUrl = $coverImageUrl, CountryCode = $countryCode,
                Venue = $venue, RegionCode = $regionCode, IsFestival = $isFestival,
                UpdatedAt = $updatedAt
            WHERE Id = $id
            """;
        BindParameters(command, entry);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(Guid entryId)
    {
        using var connection = _connectionFactory.OpenConnection();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Entries WHERE Id = $id";
        command.Parameters.AddWithValue("$id", entryId.ToString());
        await command.ExecuteNonQueryAsync();
    }

    private static void BindParameters(SqliteCommand command, Entry entry)
    {
        command.Parameters.AddWithValue("$id", entry.Id.ToString());
        command.Parameters.AddWithValue("$categoryId", entry.CategoryId.ToString());
        command.Parameters.AddWithValue("$title", entry.Title);
        command.Parameters.AddWithValue("$occurredOn", (object?)entry.OccurredOn?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$rating", (object?)entry.Rating ?? DBNull.Value);
        command.Parameters.AddWithValue("$note", (object?)entry.Note ?? DBNull.Value);
        command.Parameters.AddWithValue("$coverImageUrl", (object?)entry.CoverImageUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("$countryCode", (object?)entry.CountryCode ?? DBNull.Value);
        command.Parameters.AddWithValue("$venue", (object?)entry.Venue ?? DBNull.Value);
        command.Parameters.AddWithValue("$regionCode", (object?)entry.RegionCode ?? DBNull.Value);
        command.Parameters.AddWithValue("$isFestival", (object?)entry.IsFestival ?? DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", entry.CreatedAt.ToString("o", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$updatedAt", entry.UpdatedAt.ToString("o", CultureInfo.InvariantCulture));
    }

    private static Entry ReadEntry(SqliteDataReader reader) => new()
    {
        Id = Guid.Parse(reader.GetString(0)),
        CategoryId = Guid.Parse(reader.GetString(1)),
        Title = reader.GetString(2),
        OccurredOn = reader.IsDBNull(3) ? null : DateOnly.ParseExact(reader.GetString(3), DateFormat, CultureInfo.InvariantCulture),
        Rating = reader.IsDBNull(4) ? null : reader.GetInt32(4),
        Note = reader.IsDBNull(5) ? null : reader.GetString(5),
        CoverImageUrl = reader.IsDBNull(6) ? null : reader.GetString(6),
        CountryCode = reader.IsDBNull(7) ? null : reader.GetString(7),
        Venue = reader.IsDBNull(8) ? null : reader.GetString(8),
        RegionCode = reader.IsDBNull(9) ? null : reader.GetString(9),
        IsFestival = reader.IsDBNull(10) ? null : reader.GetBoolean(10),
        CreatedAt = DateTimeOffset.ParseExact(reader.GetString(11), "o", CultureInfo.InvariantCulture),
        UpdatedAt = DateTimeOffset.ParseExact(reader.GetString(12), "o", CultureInfo.InvariantCulture),
    };
}
