namespace dcma;

public interface IPromptHelper
{
    Task<(string imageName, string? tag)> GetIdentifierFromUserAsync(string command);
}