namespace dcma;

public interface IStopAndRemoveContainerCommand
{
    Task ExecuteAsync(string containerId);
}