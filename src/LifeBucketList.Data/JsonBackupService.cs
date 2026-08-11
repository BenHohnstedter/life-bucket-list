using System.Text.Json;
using System.Text.Json.Serialization;
using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Services;

namespace LifeBucketList.Data;

public sealed class JsonBackupService : IBackupService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task ExportAsync(Stream destination, BackupData data)
    {
        await JsonSerializer.SerializeAsync(destination, data, SerializerOptions);
    }

    public async Task<BackupData> ImportAsync(Stream source)
    {
        var data = await JsonSerializer.DeserializeAsync<BackupData>(source, SerializerOptions);
        if (data is null)
        {
            throw new InvalidDataException("Die Backup-Datei konnte nicht gelesen werden.");
        }

        return data;
    }
}
