using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class GetRunningContainersQuery : IGetRunningContainersQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly Config.Config _config;

    public GetRunningContainersQuery(IDockerClient dockerClient, Config.Config config)
    {
        _dockerClient = dockerClient;
        _config = config;
    }

    public async Task<IReadOnlyCollection<Container>> QueryAsync()
    {
        var images = _config.ImageConfigs;
        var containerNames = images.SelectMany(image
            => image.ImageTags.Select(tag => ContainerNameHelper.BuildContainerName(image.Identifier, tag)))
            .ToList();

        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        return containers
            .Select(e => new Container(e))
            .Where(e => e.Running)
            .Where(e => containerNames.Any(cn => e.ContainerName.StartsWith(cn)))
            .ToList();
    }
}