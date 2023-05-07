using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class GetContainersQuery : IGetContainersQuery
{
    private readonly IDockerClient _dockerClient;

    public GetContainersQuery(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async IAsyncEnumerable<Container> QueryRunningAsync()
    {
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
            if (container.Running)
            {
                yield return container;
            }
        }
    }

    public async IAsyncEnumerable<Container> QueryByContainerIdentifierAndTagAsync(string containerIdentifier, string? tag)
    {
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
            if (containerIdentifier == container.ContainerIdentifier && tag == container.ContainerTag)
            {
                yield return container;
            }
        }
    }

    public async IAsyncEnumerable<Container> QueryByImageIdAsync(string imageId)
    {
        var containerListResponses = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        foreach (var containerListResponse in containerListResponses)
        {
            if (containerListResponse.ImageID != imageId) continue;
            var inspectContainerResponse =
                await _dockerClient.Containers.InspectContainerAsync(containerListResponse.ID);
            var container = new Container(containerListResponse, inspectContainerResponse);
            yield return container;
        }
    }

    public async IAsyncEnumerable<Container> QueryByContainerNameAsync(string containerName)
    {
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
            if (containerName == container.ContainerName)
            {
                yield return container;
            }
        }
    }
}