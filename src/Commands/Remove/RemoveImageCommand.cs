using Docker.DotNet;
using Docker.DotNet.Models;

namespace port.Commands.Remove;

internal class RemoveImageCommand : IRemoveImageCommand
{
    private readonly IDockerClient _dockerClient;

    public RemoveImageCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public Task ExecuteAsync(string imageName, string? tag)
    {
        if (tag == null)
        {
            throw new ArgumentException("Can not remove untagged images");
        }

        return _dockerClient.Images.DeleteImageAsync(ImageNameHelper.BuildImageName(imageName, tag),
            new ImageDeleteParameters());
    }

    public Task ExecuteAsync(string id)
    {
        return _dockerClient.Images.DeleteImageAsync(id,
            new ImageDeleteParameters());
    }
}