using Docker.DotNet.Models;

namespace dcma.Run;

internal class GetContainerQuery : IGetContainerQuery
{
    public async Task<ContainerListResponse?> QueryAsync(string imageName, string tag)
    {
        var containers = await Services.DockerClient.Value.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        return containers.SingleOrDefault(e =>
            DockerHelper.TryGetImageNameAndTag(e.Image, out var imageNameAndTag)
            && imageName == imageNameAndTag.imageName && tag == imageNameAndTag.tag);
    }
}