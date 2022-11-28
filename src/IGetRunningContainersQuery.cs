namespace port;

internal interface IGetRunningContainersQuery
{
    IAsyncEnumerable<Container> QueryAsync();
}