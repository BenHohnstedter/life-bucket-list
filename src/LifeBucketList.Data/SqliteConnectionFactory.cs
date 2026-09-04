using LifeBucketList.Domain.Models;
using Microsoft.Data.Sqlite;

namespace LifeBucketList.Data;

/// <summary>Owns the SQLite connection string, schema creation and default-category seeding.</summary>
public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string databaseFilePath)
    {
        var directory = Path.GetDirectoryName(databaseFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder { DataSource = databaseFilePath }.ToString();
    }

    /// <summary>Default database location for the installed app: %AppData%\LifeBucketList\data.db.</summary>
    public static string GetDefaultDatabasePath()
    {
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataFolder, "LifeBucketList", "data.db");
    }

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>Creates the schema if missing, seeds the fixed categories on first run, and applies
    /// small one-off data migrations (e.g. renaming a category from an earlier app version).</summary>
    public async Task InitializeAsync()
    {
        using var connection = OpenConnection();

        var createTables = connection.CreateCommand();
        createTables.CommandText =
            """
            CREATE TABLE IF NOT EXISTS Categories (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                SortOrder INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Entries (
                Id TEXT PRIMARY KEY,
                CategoryId TEXT NOT NULL REFERENCES Categories(Id),
                Title TEXT NOT NULL,
                OccurredOn TEXT NULL,
                Rating INTEGER NULL,
                Note TEXT NULL,
                CoverImageUrl TEXT NULL,
                CountryCode TEXT NULL,
                Venue TEXT NULL,
                RegionCode TEXT NULL,
                IsFestival INTEGER NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Entries_CategoryId ON Entries(CategoryId);
            """;
        await createTables.ExecuteNonQueryAsync();

        await RenameCategoryIfPresentAsync(connection, "Spiele", DefaultCategories.VideoGames);
        await EnsureColumnExistsAsync(connection, "Entries", "CoverImageUrl", "TEXT NULL");
        await EnsureColumnExistsAsync(connection, "Entries", "CountryCode", "TEXT NULL");
        await EnsureColumnExistsAsync(connection, "Entries", "Venue", "TEXT NULL");
        await EnsureColumnExistsAsync(connection, "Entries", "RegionCode", "TEXT NULL");
        await EnsureColumnExistsAsync(connection, "Entries", "IsFestival", "INTEGER NULL");
        await RemoveNonFixedCategoriesAsync(connection);
        await EnsureAllFixedCategoriesExistAsync(connection);
        await ReorderFixedCategoriesAsync(connection);
    }

    /// <summary>Categories are fixed; this permanently removes any leftover category (and its
    /// entries) from when the app still allowed user-created categories. Safe to run on every
    /// startup — a no-op once only the fixed categories remain.</summary>
    private static async Task RemoveNonFixedCategoriesAsync(SqliteConnection connection)
    {
        var placeholders = string.Join(", ", DefaultCategories.Names.Select((_, i) => $"$name{i}"));

        var deleteEntries = connection.CreateCommand();
        deleteEntries.CommandText =
            $"DELETE FROM Entries WHERE CategoryId IN (SELECT Id FROM Categories WHERE Name NOT IN ({placeholders}))";
        AddNameParameters(deleteEntries);
        await deleteEntries.ExecuteNonQueryAsync();

        var deleteCategories = connection.CreateCommand();
        deleteCategories.CommandText = $"DELETE FROM Categories WHERE Name NOT IN ({placeholders})";
        AddNameParameters(deleteCategories);
        await deleteCategories.ExecuteNonQueryAsync();

        static void AddNameParameters(SqliteCommand command)
        {
            for (var i = 0; i < DefaultCategories.Names.Count; i++)
            {
                command.Parameters.AddWithValue($"$name{i}", DefaultCategories.Names[i]);
            }
        }
    }

    /// <summary>Enforces the current tab order for the fixed categories, by name. Safe to run on
    /// every startup; a no-op once the order already matches.</summary>
    private static async Task ReorderFixedCategoriesAsync(SqliteConnection connection)
    {
        for (var i = 0; i < DefaultCategories.Names.Count; i++)
        {
            var update = connection.CreateCommand();
            update.CommandText = "UPDATE Categories SET SortOrder = $sortOrder WHERE Name = $name";
            update.Parameters.AddWithValue("$sortOrder", i);
            update.Parameters.AddWithValue("$name", DefaultCategories.Names[i]);
            await update.ExecuteNonQueryAsync();
        }
    }

    /// <summary>Adds a column to an existing table if it isn't there yet. Safe to run on every
    /// startup; used to evolve the schema for databases created by an earlier app version.</summary>
    private static async Task EnsureColumnExistsAsync(SqliteConnection connection, string table, string column, string columnDefinition)
    {
        var checkColumns = connection.CreateCommand();
        checkColumns.CommandText = $"PRAGMA table_info({table})";
        using (var reader = await checkColumns.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
        }

        var addColumn = connection.CreateCommand();
        addColumn.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {columnDefinition}";
        await addColumn.ExecuteNonQueryAsync();
    }

    /// <summary>Inserts any fixed category (from <see cref="DefaultCategories.Names"/>) that doesn't
    /// exist yet — covers both the very first run (table empty) and upgrading an existing database
    /// created by an earlier app version that didn't have a category added since (e.g. Konzerte).
    /// Safe to run on every startup; a no-op once all fixed categories already exist. SortOrder is
    /// fixed up afterward by <see cref="ReorderFixedCategoriesAsync"/>.</summary>
    private static async Task EnsureAllFixedCategoriesExistAsync(SqliteConnection connection)
    {
        var existingNames = new HashSet<string>(StringComparer.Ordinal);
        var selectExisting = connection.CreateCommand();
        selectExisting.CommandText = "SELECT Name FROM Categories";
        using (var reader = await selectExisting.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                existingNames.Add(reader.GetString(0));
            }
        }

        using var transaction = connection.BeginTransaction();

        for (var i = 0; i < DefaultCategories.Names.Count; i++)
        {
            var name = DefaultCategories.Names[i];
            if (existingNames.Contains(name))
            {
                continue;
            }

            var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO Categories (Id, Name, SortOrder) VALUES ($id, $name, $sortOrder)";
            insert.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            insert.Parameters.AddWithValue("$name", name);
            insert.Parameters.AddWithValue("$sortOrder", i);
            await insert.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    /// <summary>Renames a category in place (its Id and all linked entries are preserved), if a
    /// category with the old name still exists. Safe to run on every startup.</summary>
    private static async Task RenameCategoryIfPresentAsync(SqliteConnection connection, string oldName, string newName)
    {
        var rename = connection.CreateCommand();
        rename.CommandText = "UPDATE Categories SET Name = $newName WHERE Name = $oldName";
        rename.Parameters.AddWithValue("$newName", newName);
        rename.Parameters.AddWithValue("$oldName", oldName);
        await rename.ExecuteNonQueryAsync();
    }
}
