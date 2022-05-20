namespace port;

public interface IGetContainersQuery
{
    Task<Container?> QueryByImageAsync(string imageName, string? tag);
    Task<IEnumerable<Container>> QueryByIdentifierAsync(string identifier, string? tag);
}