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

    public async Task<IEnumerable<Container>> QueryByImageNameAndTagAsync(string imageName, string? tag)
    {
        var containerListResponses = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        return containerListResponses
            .Select(e => new Container(e))
            .Where(e => imageName == e.ImageName && tag == e.ImageTag);
    }

    public async Task<IEnumerable<Container>> QueryByImageIdAsync(string imageId)
    {
        var containerListResponses = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        return containerListResponses
            .Where(e => e.ImageID == imageId)
            .Select(e => new Container(e));
    }

    public async Task<IEnumerable<Container>> QueryByContainerNameAndTagAsync(string containerName, string? tag)
    {
        var containerListResponses = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        return containerListResponses
            .Select(e => new Container(e))
            .Where(e => containerName == e.ContainerName && tag == e.ContainerTag);
    }
}