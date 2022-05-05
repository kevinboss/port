namespace dcma.Run;

public interface ICreateImageCommand
{
    Task ExecuteAsync(string? imageName, string? imageTag);
}