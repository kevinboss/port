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
            var imagesById = images.Where(e => e.Id != null).ToDictionary(e => e.Id!, image => image);
            var imageGroup = new ImageGroup(imageConfig.Identifier);
            foreach (var image in images)
            {
                imageGroup.AddImage(image);
                if (image.ParentId == null) continue;
                image.Parent = imagesById[image.ParentId!];
            }

            yield return imageGroup;
        }
    }

    private async Task<IEnumerable<Image>> GetSnapshotImagesAsync(
        IReadOnlyCollection<Config.Config.ImageConfig> imageConfigs,
        Config.Config.ImageConfig imageConfig)
    {
        var runningContainer = await _getRunningContainerQuery.QueryAsync();
        var imagesListResponses = await GetImagesByNameAsync(imageConfig.ImageName);
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
                var runningUntaggedImage
                    = running
                      && runningContainer != null
                      && runningContainer.ImageTag != tag;
                return new Image
                {
                    Name = imageName,
                    Tag = tag,
                    IsSnapshot = true,
                    Existing = true,
                    Created = e.Created,
                    Running = running,
                    RunningUntaggedImage = runningUntaggedImage,
                    Id = e.ID,
                    ParentId = string.IsNullOrEmpty(e?.ParentID) ? null : e.ParentID
                };
            });
    }

    private async IAsyncEnumerable<Image> GetBaseImagesAsync(Config.Config.ImageConfig imageConfig)
    {
        var runningContainer = await _getRunningContainerQuery.QueryAsync();
        foreach (var tag in imageConfig.ImageTags)
        {
            var parameters = new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>()
            };
            parameters.Filters.Add("reference", new Dictionary<string, bool>
            {
                { imageConfig.ImageName, true }
            });
            var imagesListResponses = await _dockerClient.Images.ListImagesAsync(parameters);
            var imagesListResponse = imagesListResponses
                .SingleOrDefault(e =>
                    e.RepoTags != null && e.RepoTags.Contains(ImageNameHelper.JoinImageNameAndTag(imageConfig.ImageName, tag)));
            var running
                = runningContainer != null
                  && imageConfig.Identifier == runningContainer.ContainerName
                  && tag == runningContainer.ContainerTag;
            var runningUntaggedImage
                = running
                  && runningContainer != null
                  && runningContainer.ImageTag != tag;
            yield return new Image
            {
                Name = imageConfig.ImageName,
                Tag = tag,
                IsSnapshot = false,
                Existing = imagesListResponse != null,
                Created = imagesListResponse?.Created,
                Running = running,
                RunningUntaggedImage = runningUntaggedImage,
                Id = imagesListResponse?.ID,
                ParentId = string.IsNullOrEmpty(imagesListResponse?.ParentID) ? null : imagesListResponse.ParentID

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
                Name = imageConfig.ImageName,
                Tag = null,
                IsSnapshot = false,
                Existing = true,
                Created = imagesListResponse.Created,
                Running = running,
                RunningUntaggedImage = false,
                Id = imagesListResponse.ID,
                ParentId = string.IsNullOrEmpty(imagesListResponse.ParentID) ? null : imagesListResponse.ParentID
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