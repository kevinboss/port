using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma.Run;

internal class CreateImageCommand : ICreateImageCommand
{
    private readonly IDockerClient _dockerClient;

    public CreateImageCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public Task ExecuteAsync(string? imageName, string? imageTag)
    {
        return _dockerClient.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = imageName,
                Tag = imageTag
            },
            null,
            new Progress<JSONMessage>());
    }
}