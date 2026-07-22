namespace port;

public interface IRemoveImageCommand
{
    Task ExecuteAsync(string imageName, string? tag, CancellationToken cancellationToken);
    Task<ImageRemovalResult> ExecuteAsync(string id, CancellationToken cancellationToken);
}
