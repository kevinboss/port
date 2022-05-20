using Docker.DotNet.Models;

namespace port.Commands.Run;

public interface IGetImageQuery
{
    Task<ImagesListResponse?> QueryAsync(string imageName, string tag);
}