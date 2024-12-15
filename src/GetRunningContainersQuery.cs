using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class GetRunningContainersQuery(IDockerClient dockerClient, port.Config.Config config) : IGetRunningContainersQuery
{

    public async IAsyncEnumerable<Container> QueryAsync()
    {
        var images = config.ImageConfigs;
        var containerNames = images.SelectMany(image
            => image.ImageTags.Select(tag => ContainerNameHelper.BuildContainerName(image.Identifier, tag)))
            .ToList();

        var containerListResponses = await dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        foreach (var containerListResponse in containerListResponses)
        {
            var inspectContainerResponse =
                await dockerClient.Containers.InspectContainerAsync(containerListResponse.ID);
            var container = new Container(containerListResponse, inspectContainerResponse);
            if (container.Running && containerNames.Any(cn => container.ContainerName.StartsWith(cn)))
            {
                yield return container;
            }
        }
    }
}
