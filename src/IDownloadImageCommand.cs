namespace port;

public interface IDownloadImageCommand
{
    Task ExecuteAsync(string imageName, string? tag);
}