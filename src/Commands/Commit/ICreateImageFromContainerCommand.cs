namespace port.Commands.Commit;

public interface ICreateImageFromContainerCommand
{
    Task<string> ExecuteAsync(string containerId, string imageName, string newTag);
}