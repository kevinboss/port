namespace port;

public static class ContainerNameHelper
{
    private const string Separator = ".";

    public static bool TryGetContainerNameAndTag(string containerName, out (string containerName, string tag) nameAndTag)
    {
        nameAndTag = (containerName, string.Empty);
        var idx = containerName.LastIndexOf(Separator, StringComparison.Ordinal);
        if (idx == -1)
        {
            return false;
        }

        nameAndTag = (containerName[..idx], containerName[(idx + 1)..]);
        return true;
    }
    
    public static string BuildContainerName(string identifier, string? tag) => $"{identifier}{Separator}{tag}";
}