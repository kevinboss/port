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
        var identifiers = images.Select(image => image.Identifier).ToList();

        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        return containers
            .Select(e => new Container(e))
            .Where(e => e.IsPortContainer)
            .Where(e => e.Running)
            .Where(e => identifiers.Contains(e.Identifier))
            .ToList();
    }
}