using Docker.DotNet.Models;

namespace dcma.Run;

public interface IGetImageQuery
{
    Task<ImagesListResponse?> QueryAsync(string imageName, string tag);
}