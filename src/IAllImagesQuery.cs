namespace port;

internal interface IAllImagesQuery
{
    IAsyncEnumerable<ImageGroup> QueryAsync();
    Task<List<Image>> QueryByImageConfigAsync(Config.Config.ImageConfig imageConfig);
}