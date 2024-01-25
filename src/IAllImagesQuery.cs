namespace port;

internal interface IAllImagesQuery
{
    IAsyncEnumerable<ImageGroup> QueryAsync();
    Task<List<Image>> QueryByImageConfigAsync(port.Config.Config.ImageConfig imageConfig);
}