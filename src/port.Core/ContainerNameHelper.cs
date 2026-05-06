namespace port;

public static class ContainerNameHelper
{
    private const string Separator = ".";

    public static bool TryGetContainerNameAndTag(
        string containerName,
        string? tagPrefix,
        out (string containerName, string tag) nameAndTag
    )
    {
        nameAndTag = (containerName, string.Empty);

        var idx = string.IsNullOrEmpty(tagPrefix)
            ? containerName.LastIndexOf(Separator, StringComparison.Ordinal)
            : containerName.LastIndexOf($"{Separator}{tagPrefix}", StringComparison.Ordinal);
        if (idx == -1)
        {
            return false;
        }

        nameAndTag = (containerName[..idx], containerName[(idx + 1)..]);
        return true;
    }

    public static string BuildContainerName(string identifier, string? tag) =>
        $"{identifier}{Separator}{SanitizeTag(tag)}";

    private static string? SanitizeTag(string? tag)
    {
        if (tag == null)
            return null;

        var sanitized = tag.Replace(':', '-');

        // Truncate very long tags (like digests) to keep container names reasonable
        const int maxLength = 20;
        if (sanitized.Length > maxLength)
            sanitized = sanitized[..maxLength];

        return sanitized;
    }
}
