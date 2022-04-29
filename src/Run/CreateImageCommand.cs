using Docker.DotNet.Models;

namespace dcma.Run;

internal class CreateImageCommand : ICreateImageCommand
{
    public Task ExecuteAsync(string? imageName, string imageTag)
    {
        return Services.DockerClient.Value.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = imageName,
                Tag = imageTag
            },
            null,
            new Progress<JSONMessage>());
    }
}