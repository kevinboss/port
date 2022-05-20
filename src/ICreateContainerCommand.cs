namespace port;

public interface ICreateContainerCommand
{
    Task ExecuteAsync(string identifier, string imageName, string tag, List<string> ports);
    Task ExecuteAsync(Container container);
}