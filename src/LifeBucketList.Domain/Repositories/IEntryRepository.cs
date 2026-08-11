using LifeBucketList.Domain.Models;

namespace LifeBucketList.Domain.Repositories;

public interface IEntryRepository
{
    Task<IReadOnlyList<Entry>> GetAllAsync();
    Task AddAsync(Entry entry);
    Task UpdateAsync(Entry entry);
    Task DeleteAsync(Guid entryId);
}
