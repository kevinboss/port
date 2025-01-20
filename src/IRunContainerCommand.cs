namespace port;

public interface IRunContainerCommand
{
    Task ExecuteAsync(string id);
    Task ExecuteAsync(Container container);
}