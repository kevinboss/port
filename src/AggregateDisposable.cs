namespace port;

internal class AggregateDisposable : IDisposable
{
    private readonly IDisposable[] _disposables;

    public AggregateDisposable(params IDisposable[] disposables)
    {
        _disposables = disposables;
    }
    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }
}