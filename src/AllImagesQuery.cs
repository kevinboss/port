using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class AllImagesQuery : IAllImagesQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly Config.Config _config;
    private readonly IGetRunningContainersQuery _getRunningContainersQuery;

    public AllImagesQuery(IDockerClient dockerClient, Config.Config config,
        IGetRunningContainersQuery getRunningContainersQuery)
    {
        _dockerClient = dockerClient;
        _config = config;
        _getRunningContainersQuery = getRunningContainersQuery;
    }

    public async IAsyncEnumerable<ImageGroup> QueryAsync()
    {
        var imageConfigs = _config.ImageConfigs;
        foreach (var imageConfig in imageConfigs)
        {
            var images = await GetBaseImagesAsync(imageConfig).ToListAsync();
            images.AddRange(await GetSnapshotImagesAsync(imageConfigs, imageConfig));
            images.AddRange(await GetUntaggedImagesAsync(imageConfig).ToListAsync());
            yield return CreateImageGroup(images, imageConfig);
        }
    }

    private static ImageGroup CreateImageGroup(List<Image> images, Config.Config.ImageConfig imageConfig)
    {
        var imageGroup = new ImageGroup(imageConfig.Identifier);
        SetParents(images, imageGroup);
        return imageGroup;
    }

    private static void SetParents(List<Image> images, ImageGroup imageGroup)
    {
        var imagesWithIds = images.Where(e => e.Id != null).ToList();
        var imageIds = imagesWithIds.Select(i => i.Id).ToList();
        if (imageIds.Count != imageIds.Distinct().Count())
            throw new InvalidOperationException("Multiple images with the same image id exist");
        var imagesById = imagesWithIds.ToDictionary(e => e.Id!, image => image);
        foreach (var image in images)
        {
            imageGroup.AddImage(image);
            if (image.ParentId == null) continue;
            image.Parent = imagesById[image.ParentId!];
        }
    }

    private async Task<IEnumerable<Image>> GetSnapshotImagesAsync(
        IReadOnlyCollection<Config.Config.ImageConfig> imageConfigs,
        Config.Config.ImageConfig imageConfig)
    {
        var runningContainers = await _getRunningContainersQuery.QueryAsync();
        var imagesListResponses = await GetImagesByNameAsync(imageConfig.ImageName);
        return imagesListResponses
            .Where(HasRepoTags)
            .Where(e => IsNotBase(imageConfigs, e))
            .Where(e => IsSnapshotOfBase(imageConfig, e))
            .Select(e =>
            {
                var (imageName, tag) = ImageNameHelper.GetImageNameAndTag(e.RepoTags.Single());
                var runningContainer = runningContainers
                    .SingleOrDefault(c =>
                        tag != null &&
                        c.ContainerName == ContainerNameHelper.BuildContainerName(imageConfig.Identifier, tag));
                var running = runningContainer != null;
                var runningUntaggedImage
                    = runningContainer != null && runningContainer.ImageTag != tag;
                return new Image
                {
                    Name = imageName,
                    Tag = tag,
                    IsSnapshot = true,
                    Existing = true,
                    Created = e.Created,
                    Running = running,
                    RelatedContainerIsRunningUntaggedImage = runningUntaggedImage,
                    Id = e.ID,
                    ParentId = string.IsNullOrEmpty(e.ParentID) ? null : e.ParentID
                };
            });
    }

    private async IAsyncEnumerable<Image> GetBaseImagesAsync(Config.Config.ImageConfig imageConfig)
    {
        var runningContainers = await _getRunningContainersQuery.QueryAsync();
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
                    e.RepoTags != null &&
                    e.RepoTags.Contains(ImageNameHelper.BuildImageName(imageConfig.ImageName, tag)));
            var runningContainer = runningContainers
                .SingleOrDefault(c =>
                    c.ContainerName == ContainerNameHelper.BuildContainerName(imageConfig.Identifier, tag));
            var running = runningContainer != null;
            var runningUntaggedImage
                = runningContainer != null && runningContainer.ImageTag != tag;
            yield return new Image
            {
                Name = imageConfig.ImageName,
                Tag = tag,
                IsSnapshot = false,
                Existing = imagesListResponse != null,
                Created = imagesListResponse?.Created,
                Running = running,
                RelatedContainerIsRunningUntaggedImage = runningUntaggedImage,
                Id = imagesListResponse?.ID,
                ParentId = string.IsNullOrEmpty(imagesListResponse?.ParentID) ? null : imagesListResponse.ParentID
            };
        }
    }

    private async IAsyncEnumerable<Image> GetUntaggedImagesAsync(Config.Config.ImageConfig imageConfig)
    {
        var runningContainers = await _getRunningContainersQuery.QueryAsync();
        var imagesListResponses = await GetImagesByNameAsync(imageConfig.ImageName);
        foreach (var imagesListResponse in imagesListResponses.Where(e => e.RepoTags == null))
        {
            var runningContainer = runningContainers
                .SingleOrDefault(c =>
                    c.ImageIdentifier == imageConfig.ImageName
                    && c.ImageTag == null);
            var running = runningContainer != null;
            yield return new Image
            {
                Name = imageConfig.ImageName,
                Tag = null,
                IsSnapshot = false,
                Existing = true,
                Created = imagesListResponse.Created,
                Running = running,
                RelatedContainerIsRunningUntaggedImage = false,
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
                var imageNameAndTag = ImageNameHelper.BuildImageName(imageConfig.ImageName, imageConfig.tag);
                return e.RepoTags.Contains(imageNameAndTag);
            });
    }

    private static bool IsSnapshotOfBase(Config.Config.ImageConfig imageConfig, ImagesListResponse e)
    {
        var imageNameAndTags = imageConfig.ImageTags.Select(tag => new
        {
            imageConfig.ImageName,
            tag
        }).Select(imageConfig1 => ImageNameHelper.BuildImageName(imageConfig1.ImageName, imageConfig1.tag));
        return e.RepoTags.Any(repoTag => imageNameAndTags.Any(repoTag.StartsWith));
    }
}