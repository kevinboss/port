using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

public class GetImageIdQuery : IGetImageIdQuery
{
    private readonly IDockerClient _dockerClient;

    public GetImageIdQuery(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task<IEnumerable<string>> QueryAsync(string imageName, string? tag)
    {
        if (tag != null && ImageNameHelper.IsDigest(tag))
        {
            try
            {
                var inspectResponse = await _dockerClient.Images.InspectImageAsync(tag);
                return [inspectResponse.ID];
            }
            catch (DockerImageNotFoundException)
            {
                return [];
            }
        }

        var parameters = new ImagesListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>(),
        };
        parameters.Filters.Add("reference", new Dictionary<string, bool> { { imageName, true } });
        var imagesListResponses = await _dockerClient.Images.ListImagesAsync(parameters);
        return imagesListResponses
            .Where(e =>
                tag == null && !e.RepoTags.Any()
                || e.RepoTags != null
                    && e.RepoTags.Any(repoTag =>
                        repoTag.Contains(ImageNameHelper.BuildImageName(imageName, tag))
                    )
            )
            .Select(e => e.ID);
    }
}
