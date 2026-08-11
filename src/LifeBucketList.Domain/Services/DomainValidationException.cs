namespace LifeBucketList.Domain.Services;

/// <summary>Thrown when a domain rule is violated. Message is user-facing (German).</summary>
public sealed class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message)
    {
    }
}
