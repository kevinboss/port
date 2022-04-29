namespace dcma;

public interface IPromptHelper
{
    Task<(string identifier, string? tag)> GetIdentifierFromUserAsync(string command);
}