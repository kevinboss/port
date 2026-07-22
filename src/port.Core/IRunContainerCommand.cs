namespace port;

public interface IRunContainerCommand
{
    Task ExecuteAsync(string id, CancellationToken cancellationToken);
    Task ExecuteAsync(Container container, CancellationToken cancellationToken);
}
