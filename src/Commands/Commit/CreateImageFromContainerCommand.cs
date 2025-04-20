using Docker.DotNet;
using Docker.DotNet.Models;

namespace port.Commands.Commit;

internal class CreateImageFromContainerCommand(IDockerClient dockerClient) : ICreateImageFromContainerCommand
{
    public async Task<string> ExecuteAsync(Container container, string imageName, string tagPrefix, string newTag)
    {
        var labels = new Dictionary<string, string>();
        var identifier = container.GetLabel(Constants.IdentifierLabel);
        if (identifier is not null) labels.Add(Constants.IdentifierLabel, identifier);
        var baseTag = container.GetLabel(Constants.BaseTagLabel);
        if (baseTag is not null) labels.Add(Constants.BaseTagLabel, baseTag);
        labels.Add(Constants.TagPrefix, tagPrefix);
        if (baseTag == newTag) throw new InvalidOperationException("Can not overwrite base tags");
        await dockerClient.Images.CommitContainerChangesAsync(new CommitContainerChangesParameters
        {
            ContainerID = container.Id,
            RepositoryName = imageName,
            Tag = newTag,
            Config = new Docker.DotNet.Models.Config
            {
                Labels = labels
            }
        });
        return newTag;
    }
}