namespace dcma.Remove;

public interface IRemoveImageCommand
{
    Task ExecuteAsync(string imageName, string tag);
}