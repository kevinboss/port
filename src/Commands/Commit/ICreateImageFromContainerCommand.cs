using Docker.DotNet.Models;

namespace dcma.Commands.Commit;

public interface ICreateImageFromContainerCommand
{
    Task ExecuteAsync(Container container, string tag);
}