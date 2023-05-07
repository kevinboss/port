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

    public async Task<string> ExecuteAsync(string containerId, string imageName, string? baseTag, string tag)
    {
        if (tag.Contains('.')) throw new ArgumentException("only [a-zA-Z0-9][a-zA-Z0-9_-] are allowed");
        string newTag;
        if (tag.StartsWith(":"))
        {
            newTag = tag.Substring(1, tag.Length - 1);
        }
        else
        {
            newTag = baseTag == null ? tag : $"{baseTag}-{tag}";
        }

        await _dockerClient.Images.CommitContainerChangesAsync(new CommitContainerChangesParameters
        {
            ContainerID = containerId,
            RepositoryName = imageName,
            Tag = newTag
        });
        return newTag;
    }
}