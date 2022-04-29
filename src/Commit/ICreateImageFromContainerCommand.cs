using Docker.DotNet.Models;

namespace dcma.Commit;

public interface ICreateImageFromContainerCommand
{
    Task ExecuteAsync(ContainerListResponse containerToCommit, string? tag);
}