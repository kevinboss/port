namespace dcma.Run;

public interface IRunContainerCommand
{
    Task ExecuteAsync(string identifier, string tag);
}