using Docker.DotNet;
using Docker.DotNet.Models;
using DotNext.Threading;

namespace port;

internal class GetContainersQuery : IGetContainersQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly AsyncLazy<IList<ContainerListResponse>> _containerListResponses;

    public GetContainersQuery(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
        _containerListResponses = new AsyncLazy<IList<ContainerListResponse>>(token =>
            _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { Limit = long.MaxValue },
                token
            )
        );
    }

    public async IAsyncEnumerable<Container> QueryRunningAsync()
    {
        var containerListResponses = await _containerListResponses.WithCancellation(
            CancellationToken.None
        );
        foreach (var containerListResponse in containerListResponses)
        {
            var inspectContainerResponse = await _dockerClient.Containers.InspectContainerAsync(
                containerListResponse.ID
            );
            var container = new Container(containerListResponse, inspectContainerResponse);
            if (container.Running)
            {
                yield return container;
            }
        }
    }

    public async IAsyncEnumerable<Container> QueryByContainerIdentifierAndTagAsync(
        string containerIdentifier,
        string? tag
    )
    {
        var containerListResponses = await _containerListResponses.WithCancellation(
            CancellationToken.None
        );
        foreach (var containerListResponse in containerListResponses)
        {
            var inspectContainerResponse = await _dockerClient.Containers.InspectContainerAsync(
                containerListResponse.ID
            );
            var container = new Container(containerListResponse, inspectContainerResponse);
            if (
                containerIdentifier == container.ContainerIdentifier
                && tag == container.ContainerTag
            )
            {
                yield return container;
            }
        }
    }

    public async IAsyncEnumerable<Container> QueryByImageIdAsync(string imageId)
    {
        var containerListResponses = await _containerListResponses.WithCancellation(
            CancellationToken.None
        );
        foreach (var containerListResponse in containerListResponses)
        {
            if (containerListResponse.ImageID != imageId)
                continue;
            var inspectContainerResponse = await _dockerClient.Containers.InspectContainerAsync(
                containerListResponse.ID
            );
            var container = new Container(containerListResponse, inspectContainerResponse);
            yield return container;
        }
    }

    public async IAsyncEnumerable<Container> QueryByContainerNameAsync(string containerName)
    {
        var containerListResponses = await _containerListResponses.WithCancellation(
            CancellationToken.None
        );
        foreach (var containerListResponse in containerListResponses)
        {
            var inspectContainerResponse = await _dockerClient.Containers.InspectContainerAsync(
                containerListResponse.ID
            );
            var container = new Container(containerListResponse, inspectContainerResponse);
            if (containerName == container.ContainerName)
            {
                yield return container;
            }
        }
    }
}
