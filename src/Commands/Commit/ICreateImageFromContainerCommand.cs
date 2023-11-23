namespace port.Commands.Commit;

public interface ICreateImageFromContainerCommand
{
    Task<string> ExecuteAsync(Container container, string imageName, string newTag);
}