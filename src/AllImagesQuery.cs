using Docker.DotNet;
using Docker.DotNet.Models;
using port.Commands.Run;

namespace port;

internal class AllImagesQuery : IAllImagesQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly Config.Config _config;
    private readonly IGetImageQuery _getImageQuery;
    private readonly IGetRunningContainerQuery _getRunningContainerQuery;

    public AllImagesQuery(IDockerClient dockerClient, Config.Config config, IGetImageQuery getImageQuery,
        IGetRunningContainerQuery getRunningContainerQuery)
    {
        _dockerClient = dockerClient;
        _config = config;
        _getImageQuery = getImageQuery;
        _getRunningContainerQuery = getRunningContainerQuery;
    }

    public async IAsyncEnumerable<ImageGroup> QueryAsync()
    {
        var imageConfigs = _config.ImageConfigs;
        foreach (var imageConfig in imageConfigs)
        {
            var images = await GetBaseImagesAsync(imageConfig).ToListAsync();
            images.AddRange(await GetSnapshotImagesAsync(imageConfigs, imageConfig));
            images.AddRange(await GetUntaggedImagesAsync(imageConfig).ToListAsync());
            yield return new ImageGroup
            {
                Identifier = imageConfig.Identifier,
                Images = images.ToList()
            };
        }
    }

    private async Task<IEnumerable<Image>> GetSnapshotImagesAsync(
        IReadOnlyCollection<Config.Config.ImageConfig> imageConfigs,
        Config.Config.ImageConfig imageConfig)
    {
        var runningContainer = await _getRunningContainerQuery.QueryAsync();
        var imageName = imageConfig.ImageName;
        var imagesListResponses = await GetImagesByNameAsync(imageName);
        return imagesListResponses
            .Where(HasRepoTags)
            .Where(e => IsNotBase(imageConfigs, e))
            .Where(e => IsSnapshotOfBase(imageConfig, e))
            .Select(e =>
            {
                var (imageName, tag) = ImageNameHelper.GetImageNameAndTag(e.RepoTags.Single());
                var running
                    = runningContainer != null
                      && imageConfig.Identifier == runningContainer.ContainerName
                      && tag == runningContainer.ContainerTag;
                var correspondingContainerUsingDifferentImage
                    = running
                      && runningContainer != null
                      && runningContainer.ContainerTag != runningContainer.ImageTag;
                return new Image
                {
                    Identifier = imageConfig.Identifier,
                    Name = imageName,
                    Tag = tag,
                    IsSnapshot = true,
                    Existing = true,
                    Created = e.Created,
                    Running = running
                };
            });
    }

    private async IAsyncEnumerable<Image> GetBaseImagesAsync(Config.Config.ImageConfig imageConfig)
    {
        var runningContainer = await _getRunningContainerQuery.QueryAsync();
        foreach (var tag in imageConfig.ImageTags)
        {
            var imagesListResponse = await _getImageQuery.QueryAsync(imageConfig.ImageName, tag);
            var running
                = runningContainer != null
                  && imageConfig.Identifier == runningContainer.ContainerName
                  && tag == runningContainer.ContainerTag;
            yield return new Image
            {
                Identifier = imageConfig.Identifier,
                Name = imageConfig.ImageName,
                Tag = tag,
                IsSnapshot = false,
                Existing = imagesListResponse != null,
                Created = imagesListResponse?.Created,
                Running = running
            };
        }
    }

    private async IAsyncEnumerable<Image> GetUntaggedImagesAsync(Config.Config.ImageConfig imageConfig)
    {
        var runningContainer = await _getRunningContainerQuery.QueryAsync();
        var imagesListResponses = await GetImagesByNameAsync(imageConfig.ImageName);
        foreach (var imagesListResponse in imagesListResponses.Where(e => e.RepoTags == null))
        {
            var running
                = runningContainer != null
                  && imageConfig.Identifier == runningContainer.ContainerName
                  && runningContainer.ContainerTag == null;
            yield return new Image
            {
                Identifier = imageConfig.Identifier,
                Name = imageConfig.ImageName,
                Tag = null,
                IsSnapshot = false,
                Existing = true,
                Created = imagesListResponse.Created,
                Running = running
            };
        }
    }

    private async Task<IList<ImagesListResponse>> GetImagesByNameAsync(string imageName)
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
        return imagesListResponses;
    }

    private static bool HasRepoTags(ImagesListResponse e)
    {
        return e.RepoTags != null;
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
                var imageNameAndTag = ImageNameHelper.JoinImageNameAndTag(imageConfig.ImageName, imageConfig.tag);
                return e.RepoTags.Contains(imageNameAndTag);
            });
    }

    private static bool IsSnapshotOfBase(Config.Config.ImageConfig imageConfig, ImagesListResponse e)
    {
        var imageNameAndTags = imageConfig.ImageTags.Select(tag => new
        {
            imageConfig.ImageName,
            tag
        }).Select(imageConfig1 => ImageNameHelper.JoinImageNameAndTag(imageConfig1.ImageName, imageConfig1.tag));
        return e.RepoTags.Any(repoTag => imageNameAndTags.Any(repoTag.StartsWith));
    }
}