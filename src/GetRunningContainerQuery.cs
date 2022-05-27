using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class GetRunningContainerQuery : IGetRunningContainerQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly Config.Config _config;

    public GetRunningContainerQuery(IDockerClient dockerClient, Config.Config config)
    {
        _dockerClient = dockerClient;
        _config = config;
    }

    public async Task<Container?> QueryAsync()
    {
        var images = _config.ImageConfigs;
        var imageNames = images.Select(image => image.ImageName).ToList();

        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        var container = containers.SingleOrDefault(e =>
            e.State == "running" && imageNames.Contains(ImageNameHelper.GetImageNameAndTag(e.Image).imageName));
        return container != null ? new Container(container) : null;
    }
}