using Docker.DotNet.Models;

namespace dcma.Run;

internal class RunContainerCommand : IRunContainerCommand
{
    public Task ExecuteAsync(string identifier, string tag)
    {
        return Services.DockerClient.Value.Containers.StartContainerAsync(
            $"{identifier}.{tag}",
            new ContainerStartParameters()
        );
    }
}