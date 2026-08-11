namespace LifeBucketList.Domain.Services;

public static class EntryValidator
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int MaxTitleLength = 200;
    public const int MaxNoteLength = 4000;

    /// <summary>Validates and returns the trimmed title, or throws if invalid.</summary>
    public static string ValidateTitle(string? title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new DomainValidationException("Der Titel darf nicht leer sein.");
        }

        if (trimmed.Length > MaxTitleLength)
        {
            throw new DomainValidationException($"Der Titel darf höchstens {MaxTitleLength} Zeichen lang sein.");
        }

        return trimmed;
    }

    public static void ValidateRating(int? rating)
    {
        if (rating is < MinRating or > MaxRating)
        {
            throw new DomainValidationException($"Die Bewertung muss zwischen {MinRating} und {MaxRating} Sternen liegen.");
        }
    }

    public static string? ValidateNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();
        if (trimmed.Length > MaxNoteLength)
        {
            throw new DomainValidationException($"Die Notiz darf höchstens {MaxNoteLength} Zeichen lang sein.");
        }

        return trimmed;
    }
}
