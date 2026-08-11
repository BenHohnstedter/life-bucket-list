using LifeBucketList.Domain.Models;

namespace LifeBucketList.Domain.Repositories;

/// <summary>Read-only: categories are a fixed, predefined set and not user-editable.</summary>
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync();
}
