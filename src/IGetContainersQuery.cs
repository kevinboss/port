namespace port;

public interface IGetContainersQuery
{
    Task<IEnumerable<Container>> QueryRunningAsync();
    Task<IEnumerable<Container>> QueryByImageNameAndTagAsync(string imageName, string? tag);
    Task<IEnumerable<Container>> QueryByImageIdAsync(string imageId);
    Task<IEnumerable<Container>> QueryByContainerNameAsync(string containerName);
}