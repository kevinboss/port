using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma;

internal class GetRunningContainersQuery : IGetRunningContainersQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly Config.Config _config;

    public GetRunningContainersQuery(IDockerClient dockerClient, Config.Config config)
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
            e.State == "running" && imageNames.Contains(DockerHelper.GetImageNameAndTag(e.Image).imageName));

        if (container == null) return null;

        var containerName = container.Names.Single().Remove(0, 1);
        var imageNameAndTag = DockerHelper.GetImageNameAndTag(container.Image);
        return new Container(container.ID, containerName, imageNameAndTag.imageName, imageNameAndTag.tag, container.Ports);
    }
}