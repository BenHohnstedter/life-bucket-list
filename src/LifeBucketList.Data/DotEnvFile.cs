namespace LifeBucketList.Data;

/// <summary>Minimal ".env" file reader (KEY=VALUE per line, "#" comments). Looks for a ".env" file
/// starting in the given directory and walking up through its ancestors, so it's found both when
/// running from the repo root and from a build output folder inside it.</summary>
public static class DotEnvFile
{
    private const int MaxAncestorLevels = 8;

    public static IReadOnlyDictionary<string, string> LoadFromNearestAncestor(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        for (var i = 0; i < MaxAncestorLevels && directory is not null; i++)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                return Parse(File.ReadAllLines(candidate));
            }

            directory = directory.Parent;
        }

        return new Dictionary<string, string>();
    }

    public static IReadOnlyDictionary<string, string> Parse(IEnumerable<string> lines)
    {
        var result = new Dictionary<string, string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim().Trim('"');
            result[key] = value;
        }

        return result;
    }
}
