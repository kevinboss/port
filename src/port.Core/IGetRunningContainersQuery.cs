namespace port;

public interface IGetRunningContainersQuery
{
    IAsyncEnumerable<Container> QueryAsync(CancellationToken cancellationToken = default);
}
