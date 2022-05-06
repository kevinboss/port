using dcma.Config;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma;

internal class AllImagesQuery : IAllImagesQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly Config.Config _config;

    public AllImagesQuery(IDockerClient dockerClient, Config.Config config)
    {
        _dockerClient = dockerClient;
        _config = config;
    }

    public async IAsyncEnumerable<ImageGroup> QueryAsync()
    {
        var imageConfigs = _config.ImageConfigs;
        foreach (var imageConfig in imageConfigs)
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
                    .Where(e => IsNotBase(imageConfigs, e))
                    .Where(e => IsSnapshotOfBase(imageConfig, e))
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
                    .Concat(imageConfig.ImageTags.Select(tag => new Image
                    {
                        Identifier = imageConfig.Identifier,
                        Name = imageConfig.ImageName,
                        Tag = tag,
                        IsSnapshot = false
                    })).ToList()
            };
        }
    }

    private static bool IsNotBase(IEnumerable<Config.Config.ImageConfig> imageConfigs, ImagesListResponse e)
    {
        return !imageConfigs
            .SelectMany(imageConfig => imageConfig.ImageTags.Select(tag => new
            {
                imageConfig.ImageName,
                tag
            }))
            .Any(imageConfig =>
            {
                var imageNameAndTag = DockerHelper.JoinImageNameAndTag(imageConfig.ImageName, imageConfig.tag);
                return e.RepoTags.Contains(imageNameAndTag);
            });
    }

    private static bool IsSnapshotOfBase(Config.Config.ImageConfig imageConfig, ImagesListResponse e)
    {
        var imageNameAndTags = imageConfig.ImageTags.Select(tag => new
        {
            imageConfig.ImageName,
            tag
        }).Select(imageConfig1 => DockerHelper.JoinImageNameAndTag(imageConfig1.ImageName, imageConfig1.tag));
        return e.RepoTags.Any(repoTag => imageNameAndTags.Any(repoTag.StartsWith));
    }
}