using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma.Commands.Run;

internal class GetImageQuery : IGetImageQuery
{
    private readonly IDockerClient _dockerClient;

    public GetImageQuery(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task<ImagesListResponse?> QueryAsync(string imageName, string tag)
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
            .Where(HasRepoTags)
            .SingleOrDefault(e =>
            e.RepoTags.Contains(DockerHelper.JoinImageNameAndTag(imageName, tag)));
    }

    private static bool HasRepoTags(ImagesListResponse e)
    {
        return e.RepoTags != null;
    }
}