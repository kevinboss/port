using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

public class RenameContainerCommand : IRenameContainerCommand
{
    private readonly IDockerClient _dockerClient;

    public RenameContainerCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task ExecuteAsync(string containerId, string newName) =>
        await _dockerClient.Containers.RenameContainerAsync(
            containerId,
            new ContainerRenameParameters { NewName = newName },
            CancellationToken.None
        );
}
