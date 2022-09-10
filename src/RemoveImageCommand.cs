using System.Net;
using Docker.DotNet;
using Docker.DotNet.Models;
using port.Commands;

namespace port;

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

    public async Task<ImageRemovalResult> ExecuteAsync(string id)
    {
        try
        {
            await _dockerClient.Images.DeleteImageAsync(id,
                new ImageDeleteParameters());
            return new ImageRemovalResult(id, true);
        }
        catch (DockerApiException e) when (e.StatusCode == HttpStatusCode.Conflict)
        {
            return new ImageRemovalResult(id, false);
        }
    }
}