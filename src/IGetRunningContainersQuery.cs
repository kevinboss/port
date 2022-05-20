namespace port;

internal interface IGetRunningContainersQuery
{
    Task<Container?> QueryAsync();
}