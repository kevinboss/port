using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma.Commands.Run;

internal class TerminateContainersCommand : ITerminateContainersCommand
{
    private readonly IDockerClient _dockerClient;

    public TerminateContainersCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task ExecuteAsync(IEnumerable<(string imageName, string tag)> imageNames)
    {
        var containers = await _dockerClient.Containers
            .ListContainersAsync(new ContainersListParameters
            {
                Limit = long.MaxValue
            });

        foreach (var containerListResponse in containers
                     .Where(e => DockerHelper.TryGetImageNameAndTag(e.Image, out var nameAndTag)
                                 && imageNames.Any(imageNameAndTag =>
                                     imageNameAndTag.imageName == nameAndTag.imageName &&
                                     imageNameAndTag.tag == nameAndTag.tag)))
        {
            await _dockerClient.Containers.StopContainerAsync(containerListResponse.ID,
                new ContainerStopParameters());
        }
    }
}