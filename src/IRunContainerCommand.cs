namespace port;

public interface IRunContainerCommand
{
    Task ExecuteAsync(string identifier, string tag);
    Task ExecuteAsync(Container containerName);
}