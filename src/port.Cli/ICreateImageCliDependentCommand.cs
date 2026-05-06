namespace port;

public interface ICreateImageCliChildCommand
{
    Task ExecuteAsync(string imageName, string? tag);
}
