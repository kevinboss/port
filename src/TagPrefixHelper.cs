using System.Text.RegularExpressions;

namespace port;

internal static partial class TagPrefixHelper
{
    public static string GetTagPrefix(string identifier) =>
        ContainerIdentifierSanitizerRegex().Replace(identifier, string.Empty).ToLower();

    [GeneratedRegex(@"[^a-zA-Z0-9_-]")]
    private static partial Regex ContainerIdentifierSanitizerRegex();
}
