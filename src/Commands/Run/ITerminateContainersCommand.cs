namespace dcma.Commands.Run;

public interface ITerminateContainersCommand
{
    Task ExecuteAsync(IEnumerable<(string imageName, string tag)> imageNames);
}