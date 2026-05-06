namespace port;

public interface IDoesImageExistQuery
{
    Task<bool> QueryAsync(string imageName, string? tag);
}
