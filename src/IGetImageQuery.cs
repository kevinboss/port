namespace port;

public interface IGetImageQuery
{
    Task<Image?> QueryAsync(string imageName, string? tag);
}
