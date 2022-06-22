namespace port;

internal interface IGetRunningContainersQuery
{
    Task<IReadOnlyCollection<Container>> QueryAsync();
}