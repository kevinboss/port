namespace port.Commands.Commit;

internal interface IGetDigestsByIdQuery
{
    Task<IList<string>?> QueryAsync(string imageId);
}