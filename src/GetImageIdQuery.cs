using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class GetImageIdQuery : IGetImageIdQuery
{
    private readonly IDockerClient _dockerClient;

    public GetImageIdQuery(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task<IEnumerable<string>> QueryAsync(string imageName, string? tag)
    {
        var parameters = new ImagesListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>()
        };
        parameters.Filters.Add("reference", new Dictionary<string, bool>
        {
            { imageName, true }
        });
        var imagesListResponses = await _dockerClient.Images.ListImagesAsync(parameters);
        return imagesListResponses
            .Where(e =>
                tag == null && e.RepoTags == null
                || e.RepoTags != null && e.RepoTags.Contains(ImageNameHelper.BuildImageName(imageName, tag)))
            .Select(e => e.ID);
    }
}