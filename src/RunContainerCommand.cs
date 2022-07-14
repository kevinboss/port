using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class RunContainerCommand : IRunContainerCommand
{
    private readonly IDockerClient _dockerClient;

    public RunContainerCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public Task ExecuteAsync(string containerName)
    {
        return _dockerClient.Containers.StartContainerAsync(
            containerName,
            new ContainerStartParameters()
        );
    }

    public Task ExecuteAsync(Container container)
    {
        return _dockerClient.Containers.StartContainerAsync(
            container.ContainerName,
            new ContainerStartParameters()
        );
    }
}