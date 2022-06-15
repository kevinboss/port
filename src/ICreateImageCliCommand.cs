namespace port;

public interface ICreateImageCliCommand
{
    Task ExecuteAsync(string imageName, string? tag);
}