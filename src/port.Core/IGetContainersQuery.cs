namespace port;

public interface IGetContainersQuery
{
    IAsyncEnumerable<Container> QueryRunningAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<Container> QueryByContainerIdentifierAndTagAsync(
        string containerIdentifier,
        string? tag,
        CancellationToken cancellationToken = default
    );
    IAsyncEnumerable<Container> QueryByImageIdAsync(
        string imageId,
        CancellationToken cancellationToken = default
    );
    IAsyncEnumerable<Container> QueryByContainerNameAsync(
        string containerName,
        CancellationToken cancellationToken = default
    );
}
