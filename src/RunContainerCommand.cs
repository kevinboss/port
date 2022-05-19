using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma;

internal class RunContainerCommand : IRunContainerCommand
{
    private readonly IDockerClient _dockerClient;

    public RunContainerCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public Task ExecuteAsync(string identifier, string? tag)
    {
        return _dockerClient.Containers.StartContainerAsync(
            $"{identifier}.{tag}",
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