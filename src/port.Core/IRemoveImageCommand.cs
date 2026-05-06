namespace port;

public interface IRemoveImageCommand
{
    Task ExecuteAsync(string imageName, string? tag);
    Task<ImageRemovalResult> ExecuteAsync(string id);
}
