namespace port;

internal interface IDoesImageExistQuery
{
    Task<bool> QueryAsync(string imageName, string? tag);
}
