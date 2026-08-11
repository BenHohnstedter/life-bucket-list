using LifeBucketList.Domain.Models;

namespace LifeBucketList.Domain.Services;

/// <summary>Serializes/deserializes the full dataset to/from a JSON stream for manual backup and transfer.</summary>
public interface IBackupService
{
    Task ExportAsync(Stream destination, BackupData data);
    Task<BackupData> ImportAsync(Stream source);
}
