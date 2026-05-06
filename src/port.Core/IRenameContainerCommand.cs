namespace port;

public interface IRenameContainerCommand
{
    Task ExecuteAsync(string containerId, string newName);
}
