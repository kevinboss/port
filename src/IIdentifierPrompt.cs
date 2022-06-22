namespace port;

public interface IIdentifierPrompt
{
    Task<(string identifier, string? tag)> GetBaseIdentifierFromUserAsync(string command);
    Task<(string identifier, string? tag)> GetDownloadedIdentifierFromUserAsync(string command);

    Task<(string identifier, string? tag)> GetRunnableIdentifierFromUserAsync(string command);

    (string identifier, string? tag) GetIdentifierOfContainerFromUser(
        IEnumerable<Container> readOnlyCollection,
        string command);

    Task<string> GetUntaggedIdentifierFromUserAsync(string command);
}