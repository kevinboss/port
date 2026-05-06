namespace port;

public interface IGetRunningContainersQuery
{
    IAsyncEnumerable<Container> QueryAsync();
}
