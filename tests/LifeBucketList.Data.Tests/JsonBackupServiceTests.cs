using LifeBucketList.Domain.Models;
using Xunit;

namespace LifeBucketList.Data.Tests;

public class JsonBackupServiceTests
{
    [Fact]
    public async Task ExportThenImport_RoundTripsCategoriesAndEntries()
    {
        var service = new JsonBackupService();
        var category = new Category { Id = Guid.NewGuid(), Name = "Filme", SortOrder = 0 };
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Title = "Inception",
            OccurredOn = new DateOnly(2024, 3, 15),
            Rating = 5,
            Note = "Grandios",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var data = new BackupData(DateTimeOffset.UtcNow, new[] { category }, new[] { entry });

        using var stream = new MemoryStream();
        await service.ExportAsync(stream, data);
        stream.Position = 0;
        var imported = await service.ImportAsync(stream);

        var importedCategory = Assert.Single(imported.Categories);
        Assert.Equal(category.Id, importedCategory.Id);
        Assert.Equal(category.Name, importedCategory.Name);

        var importedEntry = Assert.Single(imported.Entries);
        Assert.Equal(entry.Id, importedEntry.Id);
        Assert.Equal(entry.Title, importedEntry.Title);
        Assert.Equal(entry.OccurredOn, importedEntry.OccurredOn);
        Assert.Equal(entry.Rating, importedEntry.Rating);
        Assert.Equal(entry.Note, importedEntry.Note);
    }

    [Fact]
    public async Task ImportAsync_ThrowsForGarbageInput()
    {
        var service = new JsonBackupService();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("not json"));

        await Assert.ThrowsAnyAsync<Exception>(() => service.ImportAsync(stream));
    }
}
