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

    public Task ExecuteAsync(string identifier, string tag)
    {
        return _dockerClient.Containers.StartContainerAsync(
            ContainerNameHelper.JoinContainerNameAndTag(identifier, tag),
            new ContainerStartParameters()
        );
    }

    public Task ExecuteAsync(Container container)
    {
        return _dockerClient.Containers.StartContainerAsync(
            ContainerNameHelper.JoinContainerNameAndTag(container.Identifier, container.Tag),
            new ContainerStartParameters()
        );
    }
}