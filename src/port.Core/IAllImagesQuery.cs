using Docker.DotNet.Models;

namespace port;

public interface IAllImagesQuery
{
    IAsyncEnumerable<ImageGroup> QueryAsync();
    IAsyncEnumerable<(string Id, string ParentId)> QueryAllImagesWithParentAsync();
    Task<List<Image>> QueryByImageConfigAsync(port.Config.Config.ImageConfig imageConfig);
}
