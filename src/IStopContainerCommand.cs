namespace port;

public interface IStopContainerCommand
{
    public Task ExecuteAsync(string containerId);
}