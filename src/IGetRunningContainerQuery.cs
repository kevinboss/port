namespace port;

internal interface IGetRunningContainerQuery
{
    Task<Container?> QueryAsync();
}