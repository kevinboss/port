using Docker.DotNet;
using Docker.DotNet.Models;

namespace port.Commands.Commit;

internal class GetDigestsByIdQuery : IGetDigestsByIdQuery
{
    private readonly IDockerClient _dockerClient;

    public GetDigestsByIdQuery(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task<IList<string>?> QueryAsync(string imageId)
    {
        var parameters = new ImagesListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>(),
        };
        var imagesListResponses = await _dockerClient.Images.ListImagesAsync(parameters);
        var imagesListResponse = imagesListResponses.SingleOrDefault(e => e.ID == imageId);
        return imagesListResponse?.RepoDigests;
    }
}
