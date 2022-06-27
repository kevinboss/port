namespace port;

public interface IImageIdentifierPrompt
{
    Task<(string identifier, string? tag)> GetBaseIdentifierFromUserAsync(string command);
    Task<(string identifier, string? tag)> GetDownloadedIdentifierFromUserAsync(string command);

    Task<(string identifier, string? tag)> GetRunnableIdentifierFromUserAsync(string command);

    Task<string> GetUntaggedIdentifierFromUserAsync(string command);
}