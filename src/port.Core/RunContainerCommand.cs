using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

public class RunContainerCommand : IRunContainerCommand
{
    private readonly IDockerClient _dockerClient;

    public RunContainerCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public Task ExecuteAsync(string id, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.StartContainerAsync(
            id,
            new ContainerStartParameters(),
            cancellationToken
        );
    }

    public Task ExecuteAsync(Container container, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.StartContainerAsync(
            container.Id,
            new ContainerStartParameters(),
            cancellationToken
        );
    }
}
