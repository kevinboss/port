namespace dcma;

public interface IAllImagesQuery
{
    IAsyncEnumerable<ImageGroup> QueryAsync();
}