using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma.Commands.Remove;

internal class RemoveImageCommand : IRemoveImageCommand
{
    private readonly IDockerClient _dockerClient;

    public RemoveImageCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public Task ExecuteAsync(string imageName, string tag)
    {
        return _dockerClient.Images.DeleteImageAsync(ImageNameHelper.JoinImageNameAndTag(imageName, tag), new ImageDeleteParameters());
    }
}