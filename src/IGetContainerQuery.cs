using Docker.DotNet.Models;

namespace dcma;

public interface IGetContainerQuery
{
    Task<ContainerListResponse?> QueryAsync(string imageName, string tag);
}