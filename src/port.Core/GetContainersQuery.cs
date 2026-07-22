using System.Net;
using System.Runtime.CompilerServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNext.Threading;

namespace port;

public class GetContainersQuery : IGetContainersQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly AsyncLazy<IList<ContainerListResponse>> _containerListResponses;

    public GetContainersQuery(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
        _containerListResponses = new AsyncLazy<IList<ContainerListResponse>>(async token =>
        {
            var containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { Limit = long.MaxValue },
                token
            );
            return containers.Where(clr => clr.State != "dead").ToList();
        });
    }

    public async IAsyncEnumerable<Container> QueryRunningAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var containerListResponses = await _containerListResponses.WithCancellation(
            cancellationToken
        );
        foreach (var containerListResponse in containerListResponses)
        {
            ContainerInspectResponse inspectContainerResponse;
            try
            {
                inspectContainerResponse = await _dockerClient.Containers.InspectContainerAsync(
                    containerListResponse.ID,
                    cancellationToken
                );
            }
            catch (DockerApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                continue;
            }

            var container = new Container(containerListResponse, inspectContainerResponse);
            if (container.Running)
            {
                yield return container;
            }
        }
    }

    public async IAsyncEnumerable<Container> QueryByContainerIdentifierAndTagAsync(
        string containerIdentifier,
        string? tag,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var containerListResponses = await _containerListResponses.WithCancellation(
            cancellationToken
        );
        foreach (var containerListResponse in containerListResponses)
        {
            ContainerInspectResponse inspectContainerResponse;
            try
            {
                inspectContainerResponse = await _dockerClient.Containers.InspectContainerAsync(
                    containerListResponse.ID,
                    cancellationToken
                );
            }
            catch (DockerApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                continue;
            }

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

    public async IAsyncEnumerable<Container> QueryByImageIdAsync(
        string imageId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var containerListResponses = await _containerListResponses.WithCancellation(
            cancellationToken
        );
        foreach (var containerListResponse in containerListResponses)
        {
            if (containerListResponse.ImageID != imageId)
                continue;
            ContainerInspectResponse inspectContainerResponse;
            try
            {
                inspectContainerResponse = await _dockerClient.Containers.InspectContainerAsync(
                    containerListResponse.ID,
                    cancellationToken
                );
            }
            catch (DockerApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                continue;
            }

            var container = new Container(containerListResponse, inspectContainerResponse);
            yield return container;
        }
    }

    public async IAsyncEnumerable<Container> QueryByContainerNameAsync(
        string containerName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var containerListResponses = await _containerListResponses.WithCancellation(
            cancellationToken
        );
        foreach (var containerListResponse in containerListResponses)
        {
            ContainerInspectResponse inspectContainerResponse;
            try
            {
                inspectContainerResponse = await _dockerClient.Containers.InspectContainerAsync(
                    containerListResponse.ID,
                    cancellationToken
                );
            }
            catch (DockerApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                continue;
            }

            var container = new Container(containerListResponse, inspectContainerResponse);
            if (containerName == container.ContainerName)
            {
                yield return container;
            }
        }
    }
}
