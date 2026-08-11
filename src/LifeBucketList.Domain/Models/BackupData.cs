namespace LifeBucketList.Domain.Models;

/// <summary>Full snapshot of all user data, as written to / read from a JSON backup file.</summary>
public sealed record BackupData(DateTimeOffset ExportedAt, IReadOnlyList<Category> Categories, IReadOnlyList<Entry> Entries);
