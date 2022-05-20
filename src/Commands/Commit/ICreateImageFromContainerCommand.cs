namespace port.Commands.Commit;

public interface ICreateImageFromContainerCommand
{
    Task ExecuteAsync(Container container, string tag);
}