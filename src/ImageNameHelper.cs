namespace port;

public static class ImageNameHelper
{
    private const string TagSeparator = ":";
    private const string DigestSeparator = "@";

    public static (string imageName, string? tag) GetImageNameAndTag(string imageName)
    {
        if (!TryGetImageNameAndTag(imageName, out var result))
        {
            throw new ArgumentException($"Does not contain tag separator {TagSeparator}", nameof(imageName));
        }

        return result;
    }

    public static bool TryGetImageNameAndTag(string imageName, out (string imageName, string tag) nameAndTag)
    {
        nameAndTag = (imageName, string.Empty);

        // Check for digest reference (contains @)
        // Format: image@sha256:xxx or image:tag@sha256:xxx
        var digestIdx = imageName.IndexOf(DigestSeparator, StringComparison.Ordinal);
        if (digestIdx != -1)
        {
            // Has digest - extract base image name (part before @)
            var basePart = imageName[..digestIdx];
            var digest = imageName[(digestIdx + 1)..];

            // Base part might have a tag (image:tag) or not (image)
            var tagIdx = basePart.LastIndexOf(TagSeparator, StringComparison.Ordinal);
            if (tagIdx != -1)
            {
                // Has both tag and digest: image:tag@sha256:xxx
                // Return the image name and the full digest as the "tag"
                nameAndTag = (basePart[..tagIdx], digest);
            }
            else
            {
                // Just digest, no tag: image@sha256:xxx
                nameAndTag = (basePart, digest);
            }
            return true;
        }

        // No digest - look for tag separator
        var idx = imageName.LastIndexOf(TagSeparator, StringComparison.Ordinal);
        if (idx == -1)
        {
            return false;
        }

        nameAndTag = (imageName[..idx], imageName[(idx + 1)..]);
        return true;
    }

    public static string BuildImageName(string imageName, string? tag = null)
    {
        if (tag == null) return imageName;

        // Digests use @ separator, tags use : separator
        var separator = IsDigest(tag) ? DigestSeparator : TagSeparator;
        return $"{imageName}{separator}{tag}";
    }

    public static bool IsDigest(string tag) =>
        tag.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ||
        tag.StartsWith("sha512:", StringComparison.OrdinalIgnoreCase);
}