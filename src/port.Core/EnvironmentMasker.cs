using System.Text.RegularExpressions;

namespace port;

/// <summary>
/// Masks secret-looking values in <c>KEY=VALUE</c> environment entries.
/// Used when a container is serialised for the MCP transport, so credentials are
/// not handed to an agent. The in-memory values stay intact for container creation.
/// </summary>
public static partial class EnvironmentMasker
{
    private const string Mask = "***";

    private static readonly string[] SecretKeyFragments =
    [
        "PASSWORD",
        "PASSWD",
        "PWD",
        "SECRET",
        "TOKEN",
        "CREDENTIAL",
        "APIKEY",
        "API_KEY",
        "ACCESSKEY",
        "ACCESS_KEY",
        "PRIVATE_KEY",
    ];

    /// <summary>
    /// Returns the assignment with its value masked when the key names a secret,
    /// otherwise with any secret segments inside the value masked. Entries without
    /// a <c>=</c> are returned unchanged.
    /// </summary>
    public static string MaskAssignment(string assignment)
    {
        var separator = assignment.IndexOf('=');
        if (separator < 0)
            return assignment;

        var key = assignment[..separator];
        var value = assignment[(separator + 1)..];

        return IsSecretKey(key) && value.Length > 0
            ? $"{key}={Mask}"
            : $"{key}={MaskEmbeddedSecrets(value)}";
    }

    public static bool IsSecretKey(string key) =>
        SecretKeyFragments.Any(fragment =>
            key.Contains(fragment, StringComparison.OrdinalIgnoreCase)
        );

    /// <summary>
    /// Masks credentials carried inside a value, such as the password of a
    /// connection string, where the key itself gives no hint.
    /// </summary>
    private static string MaskEmbeddedSecrets(string value) =>
        EmbeddedSecretRegex().Replace(value, match => $"{match.Groups["key"].Value}={Mask}");

    [GeneratedRegex(
        @"(?<key>password|passwd|pwd|secret|token|api[_-]?key)\s*=\s*[^;]+",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex EmbeddedSecretRegex();
}
