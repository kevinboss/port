namespace port;

public static class ContainerNameHelper
{
    private const string Separator = ".";

    public static string BuildContainerName(string identifier, string tag) => $"{identifier}{Separator}{tag}";
}