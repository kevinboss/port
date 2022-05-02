using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma.Commit;

internal class GetRunningContainersQuery : IGetRunningContainersQuery
{
    private readonly IDockerClient _dockerClient;

    public GetRunningContainersQuery(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task<ContainerListResponse?> QueryAsync()
    {
        var images = Services.Config.Value.Images;
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