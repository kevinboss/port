using Docker.DotNet;
using Docker.DotNet.Models;

namespace port.Commands.Commit;

internal class CreateImageFromContainerCommand : ICreateImageFromContainerCommand
{
    private readonly IDockerClient _dockerClient;

    public CreateImageFromContainerCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task<string> ExecuteAsync(string containerId, string imageName, string newTag)
    {
        await _dockerClient.Images.CommitContainerChangesAsync(new CommitContainerChangesParameters
        {
            ContainerID = containerId,
            RepositoryName = imageName,
            Tag = newTag,
            Config = new Docker.DotNet.Models.Config()
        });
        return newTag;
    }
}