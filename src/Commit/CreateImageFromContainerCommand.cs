using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma.Commit;

internal class CreateImageFromContainerCommand : ICreateImageFromContainerCommand
{
    private readonly IDockerClient _dockerClient;

    public CreateImageFromContainerCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public Task ExecuteAsync(ContainerListResponse containerToCommit, string? tag) =>
        _dockerClient.Images.CommitContainerChangesAsync(new CommitContainerChangesParameters
        {
            ContainerID = containerToCommit.ID,
            RepositoryName = DockerHelper.GetImageNameAndTag(containerToCommit.Image).imageName,
            Tag = tag
        });
}