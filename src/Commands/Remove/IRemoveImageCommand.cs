namespace port.Commands.Remove;

public interface IRemoveImageCommand
{
    Task ExecuteAsync(string imageName, string tag);
}