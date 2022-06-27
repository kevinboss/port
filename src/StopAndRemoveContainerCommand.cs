using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class StopAndRemoveContainerCommand : IStopAndRemoveContainerCommand
{
    private readonly IStopContainerCommand _stopContainerCommand;
    private readonly IDockerClient _dockerClient;

    public StopAndRemoveContainerCommand(IDockerClient dockerClient, IStopContainerCommand stopContainerCommand)
    {
        _dockerClient = dockerClient;
        _stopContainerCommand = stopContainerCommand;
    }

    public async Task ExecuteAsync(string containerId)
    {
        await _stopContainerCommand.ExecuteAsync(containerId);
        await _dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters());
    }
}