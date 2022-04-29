using Docker.DotNet.Models;

namespace dcma.Commit;

internal class CreateImageFromContainerCommand : ICreateImageFromContainerCommand
{
    public Task ExecuteAsync(ContainerListResponse containerToCommit, string? tag) =>
        Services.DockerClient.Value.Images.CommitContainerChangesAsync(new CommitContainerChangesParameters
        {
            ContainerID = containerToCommit.ID,
            RepositoryName = DockerHelper.GetImageNameAndTag(containerToCommit.Image).imageName,
            Tag = tag
        });
}