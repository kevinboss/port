namespace port.Commands.Run;

public interface IStopContainerCommand
{
    public Task ExecuteAsync(string imageNames);
}