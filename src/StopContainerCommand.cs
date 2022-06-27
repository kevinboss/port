using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class StopContainerCommand : IStopContainerCommand
{
    private readonly IDockerClient _dockerClient;

    public StopContainerCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task ExecuteAsync(string containerId) =>
        await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters());
}