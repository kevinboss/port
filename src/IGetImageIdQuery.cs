namespace port;

internal interface IGetImageIdQuery
{
    Task<string?> QueryAsync(string imageName, string? tag);
}