namespace port;

public interface IStopAndRemoveContainerCommand
{
    Task ExecuteAsync(string containerId, CancellationToken cancellationToken);
}
