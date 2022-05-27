namespace port;

internal interface IAllImagesQuery
{
    IAsyncEnumerable<ImageGroup> QueryAsync();
}