namespace port;

internal interface IGetImageIdQuery
{
    Task<IEnumerable<string>> QueryAsync(string imageName, string? tag);
}
