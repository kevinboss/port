using Docker.DotNet;
using Docker.DotNet.Models;

namespace port.Commands.Run;

internal class TerminateContainersCommand : ITerminateContainersCommand
{
    private readonly IDockerClient _dockerClient;

    public TerminateContainersCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task ExecuteAsync(IEnumerable<(string imageName, string? tag)> imageNames)
    {
        var containers = (await _dockerClient.Containers
            .ListContainersAsync(new ContainersListParameters
            {
                Limit = long.MaxValue
            })).Select(e => new Container(e));

        foreach (var container in containers
                     .Where(e => imageNames.Any(imageNameAndTag =>
                         imageNameAndTag.imageName == e.ImageName &&
                         imageNameAndTag.tag == e.ImageTag)))
        {
            await _dockerClient.Containers.StopContainerAsync(container.Id,
                new ContainerStopParameters());
        }
    }
}