namespace port;

public interface ICreateImageCommand
{
    Task ExecuteAsync(string imageName, string? tag, CancellationToken cancellationToken);

    IObservable<Progress> ProgressObservable { get; }
}
