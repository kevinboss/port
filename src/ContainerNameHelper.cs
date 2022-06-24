namespace port;

public static class ContainerNameHelper
{
    private const string Separator = ".";
    
    public static (string containerName, string? tag) GetContainerIdentifierAndTag(string nameAndIdentifier)
    {
        var idx = nameAndIdentifier.LastIndexOf(Separator, StringComparison.Ordinal);
        if (idx == -1)
        {
            throw new ArgumentException($"Does not contain tag separator {Separator}", nameof(nameAndIdentifier));
        }

        return (nameAndIdentifier[..idx], nameAndIdentifier[(idx + 1)..]);
    }

    public static bool TryGetContainerIdentifierAndTag(string nameAndIdentifier, out (string identifier, string tag) nameAndTag)
    {
        nameAndTag = (nameAndIdentifier, string.Empty);
        var idx = nameAndIdentifier.LastIndexOf(Separator, StringComparison.Ordinal);
        if (idx == -1)
        {
            return false;
        }

        nameAndTag = (nameAndIdentifier[..idx], nameAndIdentifier[(idx + 1)..]);
        return true;
    }

    public static string JoinContainerNameAndTag(string containerName, string? tag) => $"{containerName}{Separator}{tag}";
}