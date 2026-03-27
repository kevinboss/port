namespace port;

public static class DigestHelper
{
    private const string Separator = "@";

    public static bool TryGetImageNameAndId(
        string digest,
        out (string imageName, string tag) nameAndId
    )
    {
        nameAndId = (digest, string.Empty);
        var idx = digest.LastIndexOf(Separator, StringComparison.Ordinal);
        if (idx == -1)
        {
            return false;
        }

        nameAndId = (digest[..idx], digest[(idx + 1)..]);
        return true;
    }
}
