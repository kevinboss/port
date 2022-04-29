using Docker.DotNet.Models;

namespace dcma.Commit;

internal class GetRunningContainersQuery : IGetRunningContainersQuery
{
    public async Task<ContainerListResponse?> QueryAsync()
    {
        var images = Services.Config.Value.Images;
        var imageNames = images.Select(image => image.ImageName).ToList();

        var containers = await Services.DockerClient.Value.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        return containers.SingleOrDefault(e =>
            e.State == "running" && imageNames.Contains(DockerHelper.GetImageNameAndTag(e.Image).imageName));
    }
}