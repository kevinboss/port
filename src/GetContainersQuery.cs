using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class GetContainersQuery : IGetContainersQuery
{
    private readonly IDockerClient _dockerClient;

    public GetContainersQuery(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task<Container?> QueryByImageAsync(string imageName, string? tag)
    {
        var containerListResponses = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        var container = containerListResponses.SingleOrDefault(e =>
            ImageNameHelper.TryGetImageNameAndTag(e.Image, out var imageNameAndTag)
            && imageName == imageNameAndTag.imageName && tag == imageNameAndTag.tag);
        return container != null ? new Container(container) : null;
    }

    public async Task<IEnumerable<Container>> QueryByIdentifierAsync(string identifier, string? tag)
    {
        var containerListResponses = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        return containerListResponses
            .Where(e =>
            {
                var containerName = e.Names.Single().Remove(0, 1);
                return ContainerNameHelper.TryGetContainerNameAndTag(containerName, out var containerNameAndTag)
                       && identifier == containerNameAndTag.identifier && tag == containerNameAndTag.tag;
            }).Select(e => new Container(e));
    }
}