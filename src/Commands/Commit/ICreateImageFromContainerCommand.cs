namespace port.Commands.Commit;

public interface ICreateImageFromContainerCommand
{
    Task ExecuteAsync(string containerId, string imageName, string? baseTag, string tag);
}