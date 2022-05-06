namespace dcma;

public interface IPromptHelper
{
    Task<(string identifier, string tag)> GetBaseIdentifierFromUserAsync(string command);
    Task<(string identifier, string tag)> GetIdentifierFromUserAsync(string command, bool hideMissing = false);
}