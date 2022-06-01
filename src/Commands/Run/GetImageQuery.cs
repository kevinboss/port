using Docker.DotNet;
using Docker.DotNet.Models;

namespace port.Commands.Run;

internal class GetImageQuery : IGetImageQuery
{
    private readonly IDockerClient _dockerClient;

    public GetImageQuery(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task<ImagesListResponse?> QueryAsync(string imageName, string? tag)
    {
        var imagesListResponses = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                {
                    "reference", new Dictionary<string, bool>
                    {
                        { imageName, true }
                    }
                }
            }
        });
        return imagesListResponses
            .SingleOrDefault(e =>
                tag == null && e.RepoTags == null
                || e.RepoTags != null && e.RepoTags.Contains(ImageNameHelper.JoinImageNameAndTag(imageName, tag)));
    }
}