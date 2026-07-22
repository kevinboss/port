using Docker.DotNet.Models;

namespace port;

public interface IAllImagesQuery
{
    IAsyncEnumerable<ImageGroup> QueryAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<(string Id, string ParentId)> QueryAllImagesWithParentAsync(
        CancellationToken cancellationToken = default
    );
    Task<List<Image>> QueryByImageConfigAsync(
        port.Config.Config.ImageConfig imageConfig,
        CancellationToken cancellationToken = default
    );
}
