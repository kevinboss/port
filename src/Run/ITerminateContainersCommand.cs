namespace dcma.Run;

public interface ITerminateContainersCommand
{
    Task ExecuteAsync(IEnumerable<(string imageName, string tag)> imageNames);
}