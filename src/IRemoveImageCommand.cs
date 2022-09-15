namespace port;

internal interface IRemoveImageCommand
{
    Task ExecuteAsync(string imageName, string? tag);
    Task<ImageRemovalResult> ExecuteAsync(string id);
}