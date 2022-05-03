namespace dcma.Remove;

public interface IStopAndRemoveContainerCommand
{
    Task ExecuteAsync(string containerId);
}