namespace port;

public interface IImageIdentifierPrompt
{
    string GetBaseIdentifierFromUser(string command);
    Task<(string identifier, string? tag)> GetBaseIdentifierAndTagFromUserAsync(string command);
    Task<(string identifier, string? tag)> GetDownloadedIdentifierAndTagFromUserAsync(
        string command
    );

    Task<(string identifier, string? tag)> GetRunnableIdentifierAndTagFromUserAsync(string command);
}
