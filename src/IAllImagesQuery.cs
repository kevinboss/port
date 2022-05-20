namespace port;

public interface IAllImagesQuery
{
    IAsyncEnumerable<ImageGroup> QueryAsync();
}