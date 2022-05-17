using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma;

internal class StopAndRemoveContainerCommand : IStopAndRemoveContainerCommand
{
    private readonly IDockerClient _dockerClient;

    public StopAndRemoveContainerCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task ExecuteAsync(string containerId)
    {
        await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters());
        await _dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters());
    }
}