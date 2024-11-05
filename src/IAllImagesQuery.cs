using Docker.DotNet.Models;

namespace port;

internal interface IAllImagesQuery
{
    IAsyncEnumerable<ImageGroup> QueryAsync();
    Task<IEnumerable<(string Id, string ParentId)>> QueryAllImagesWithParentAsync();
    Task<List<Image>> QueryByImageConfigAsync(port.Config.Config.ImageConfig imageConfig);
}