namespace dcma;

internal interface IGetRunningContainersQuery
{
    Task<Container?> QueryAsync();
}