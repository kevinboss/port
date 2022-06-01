namespace port;

public static class ContainerNameHelper
{
    private const string Separator = ".";
    
    public static (string containerName, string? tag) GetContainerNameAndTag(string containerName)
    {
        var idx = containerName.LastIndexOf(Separator, StringComparison.Ordinal);
        if (idx == -1)
        {
            throw new ArgumentException($"Does not contain tag separator {Separator}", nameof(containerName));
        }

        return (containerName[..idx], containerName[(idx + 1)..]);
    }

    public static bool TryGetContainerNameAndTag(string containerName, out (string identifier, string tag) nameAndTag)
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

    public static string JoinContainerNameAndTag(string containerName, string? tag)
    {
        return $"{containerName}{Separator}{tag}";
    }
}