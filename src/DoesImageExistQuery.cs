using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class DoesImageExistQuery : IDoesImageExistQuery
{
    private readonly IDockerClient _dockerClient;

    public DoesImageExistQuery(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task<bool> QueryAsync(string imageName, string? tag)
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
            .Any(e =>
                tag == null && !e.RepoTags.Any()
                || e.RepoTags != null && e.RepoTags.Contains(ImageNameHelper.BuildImageName(imageName, tag)));
    }
}