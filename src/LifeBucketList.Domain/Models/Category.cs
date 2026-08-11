namespace LifeBucketList.Domain.Models;

/// <summary>One of the fixed, predefined categories. Categories are not user-editable.</summary>
public sealed class Category
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required int SortOrder { get; init; }
}
