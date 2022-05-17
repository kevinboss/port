using Docker.DotNet.Models;

namespace dcma.Commands.Run;

public interface IGetImageQuery
{
    Task<ImagesListResponse?> QueryAsync(string imageName, string tag);
}