namespace port;

public interface IRunContainerCommand
{
    Task ExecuteAsync(string containerName);
    Task ExecuteAsync(Container container);
}