using Docker.DotNet.Models;

namespace dcma.Commit;

public interface IGetRunningContainersQuery
{
    Task<ContainerListResponse?> QueryAsync();
}