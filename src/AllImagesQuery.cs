using dcma.Config;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma;

internal class AllImagesQuery : IAllImagesQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly IConfig _config;

    public AllImagesQuery(IDockerClient dockerClient, IConfig config)
    {
        _dockerClient = dockerClient;
        _config = config;
    }

    public async IAsyncEnumerable<ImageGroup> QueryAsync()
    {
        var images = _config.Images;
        foreach (var imageConfig in images)
        {
            var imagesListResponses = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "reference", new Dictionary<string, bool>
                        {
                            { imageConfig.ImageName, true }
                        }
                    }
                }
            });
            yield return new ImageGroup
            {
                Identifier = imageConfig.Identifier,
                Images = imagesListResponses
                    .Where(e => !images.Any(imageConfig1 => e.RepoTags.Contains(DockerHelper
                        .JoinImageNameAndTag(imageConfig1.ImageName, imageConfig1.ImageTag))))
                    .Where(e => e.RepoTags.Any(repoTag => repoTag.StartsWith(DockerHelper
                        .JoinImageNameAndTag(imageConfig.ImageName, imageConfig.ImageTag))))
                    .Select(e =>
                    {
                        var (imageName, tag) = DockerHelper.GetImageNameAndTag(e.RepoTags.First());
                        return new Image
                        {
                            Identifier = imageConfig.Identifier,
                            Name = imageName,
                            Tag = tag,
                            IsSnapshot = true
                        };
                    })
                    .Concat(new List<Image>
                    {
                        new()
                        {
                            Identifier = imageConfig.Identifier,
                            Name = imageConfig.ImageName,
                            Tag = imageConfig.ImageTag,
                            IsSnapshot = false
                        }
                    }).ToList()
            };
        }
    }
}