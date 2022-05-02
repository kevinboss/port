using dcma.Config;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma.Commit;

internal class GetRunningContainersQuery : IGetRunningContainersQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly IConfig _config;

    public GetRunningContainersQuery(IDockerClient dockerClient, IConfig config)
    {
        _dockerClient = dockerClient;
        _config = config;
    }

    public async Task<ContainerListResponse?> QueryAsync()
    {
        var images = _config.Images;
        var imageNames = images.Select(image => image.ImageName).ToList();

        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        return containers.SingleOrDefault(e =>
            e.State == "running" && imageNames.Contains(DockerHelper.GetImageNameAndTag(e.Image).imageName));
    }
}