namespace port;

public interface IGetContainersQuery
{
    IAsyncEnumerable<Container> QueryRunningAsync();
    IAsyncEnumerable<Container> QueryByContainerIdentifierAndTagAsync(string containerIdentifier, string? tag);
    IAsyncEnumerable<Container> QueryByImageIdAsync(string imageId);
    IAsyncEnumerable<Container> QueryByContainerNameAsync(string containerName);
}