using Docker.DotNet.Models;

namespace dcma.Run;

internal class TerminateContainersCommand : ITerminateContainersCommand
{
    public async Task ExecuteAsync(IEnumerable<(string imageName, string tag)> imageNames)
    {
        var containers = await Services.DockerClient.Value.Containers
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
            await Services.DockerClient.Value.Containers.StopContainerAsync(containerListResponse.ID,
                new ContainerStopParameters());
        }
    }
}