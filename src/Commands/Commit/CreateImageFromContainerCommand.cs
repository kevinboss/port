using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma.Commands.Commit;

internal class CreateImageFromContainerCommand : ICreateImageFromContainerCommand
{
    private readonly IDockerClient _dockerClient;

    public CreateImageFromContainerCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public Task ExecuteAsync(Container container, string tag)
    {
        return _dockerClient.Images.CommitContainerChangesAsync(new CommitContainerChangesParameters
        {
            ContainerID = container.Id,
            RepositoryName = container.ImageName,
            Tag = $"{container.Tag}.{tag}"
        });
    }
}