namespace port;

public interface IGetImageIdQuery
{
    Task<IEnumerable<string>> QueryAsync(string imageName, string? tag);
}
