namespace port.Commands.Run;

public interface ITerminateContainersCommand
{
    public Task ExecuteAsync(IEnumerable<(string imageName, string? tag)> imageNames);
}