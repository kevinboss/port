using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma;

internal class GetContainerQuery : IGetContainerQuery
{
    private readonly IDockerClient _dockerClient;

    public GetContainerQuery(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task<ContainerListResponse?> QueryAsync(string imageName, string? tag)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        return containers.SingleOrDefault(e =>
            DockerHelper.TryGetImageNameAndTag(e.Image, out var imageNameAndTag)
            && imageName == imageNameAndTag.imageName && tag == imageNameAndTag.tag);
    }
}