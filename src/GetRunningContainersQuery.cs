using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class GetRunningContainersQuery : IGetRunningContainersQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly port.Config.Config _config;

    public GetRunningContainersQuery(IDockerClient dockerClient, port.Config.Config config)
    {
        _dockerClient = dockerClient;
        _config = config;
    }

    public async IAsyncEnumerable<Container> QueryAsync()
    {
        var images = _config.ImageConfigs;
        var containerNames = images.SelectMany(image
            => image.ImageTags.Select(tag => ContainerNameHelper.BuildContainerName(image.Identifier, tag)))
            .ToList();

        var containerListResponses = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        foreach (var containerListResponse in containerListResponses)
        {
            var inspectContainerResponse =
                await _dockerClient.Containers.InspectContainerAsync(containerListResponse.ID);
            var container = new Container(containerListResponse, inspectContainerResponse);
            if (container.Running && containerNames.Any(cn => container.ContainerName.StartsWith(cn)))
            {
                yield return container;
            }
        }
    }
}