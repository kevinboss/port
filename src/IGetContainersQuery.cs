namespace port;

public interface IGetContainersQuery
{
    IAsyncEnumerable<Container> QueryRunningAsync();
    IAsyncEnumerable<Container> QueryByImageNameAndTagAsync(string imageName, string? tag);
    IAsyncEnumerable<Container> QueryByImageIdAsync(string imageId);
    IAsyncEnumerable<Container> QueryByContainerNameAsync(string containerName);
}