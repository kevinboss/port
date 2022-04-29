using Docker.DotNet.Models;

namespace dcma.Run;

public interface IGetContainerQuery
{
    Task<ContainerListResponse?> QueryAsync(string imageName, string tag);
}