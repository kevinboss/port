namespace port;

public static class ImageNameHelper
{
    private const string Separator = ":";

    public static (string imageName, string? tag) GetImageNameAndTag(string imageName)
    {
        var idx = imageName.LastIndexOf(Separator, StringComparison.Ordinal);
        if (idx == -1)
        {
            throw new ArgumentException(
                $"Does not contain tag separator {Separator}",
                nameof(imageName)
            );
        }

        return (imageName[..idx], imageName[(idx + 1)..]);
    }

    public static bool TryGetImageNameAndTag(
        string imageName,
        out (string imageName, string tag) nameAndTag
    )
    {
        nameAndTag = (imageName, string.Empty);
        var idx = imageName.LastIndexOf(Separator, StringComparison.Ordinal);
        if (idx == -1)
        {
            return false;
        }

        nameAndTag = (imageName[..idx], imageName[(idx + 1)..]);
        return true;
    }

    public static string BuildImageName(string imageName, string? tag = null) =>
        tag == null ? imageName : $"{imageName}{Separator}{tag}";
}
