namespace port;

public interface IIdentifierPrompt
{
    Task<(string identifier, string? tag)> GetBaseIdentifierFromUserAsync(string command);

    Task<(string identifier, string? tag)> GetIdentifierFromUserAsync(string command,
        bool hideMissing);
}