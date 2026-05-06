namespace port.Commands.Commit;

public interface IGetDigestsByIdQuery
{
    Task<IList<string>?> QueryAsync(string imageId);
}
