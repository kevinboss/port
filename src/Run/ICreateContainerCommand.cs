namespace dcma.Run;

public interface ICreateContainerCommand
{
    Task ExecuteAsync(string identifier, string imageName, string tag, int portFrom, int portTo);
}