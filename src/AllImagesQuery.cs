using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class AllImagesQuery : IAllImagesQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly port.Config.Config _config;
    private readonly IGetContainersQuery _getContainersQuery;

    public AllImagesQuery(IDockerClient dockerClient, port.Config.Config config, IGetContainersQuery getContainersQuery)
    {
        _dockerClient = dockerClient;
        _config = config;
        _getContainersQuery = getContainersQuery;
    }

    public async IAsyncEnumerable<ImageGroup> QueryAsync()
    {
        var imageConfigs = _config.ImageConfigs;

        foreach (var imageConfig in imageConfigs)
        {
            var images = await QueryByImageConfigAsync(imageConfig, imageConfigs);
            yield return CreateImageGroup(images, imageConfig);
        }
    }

    public async IAsyncEnumerable<(string Id, string ParentId)> QueryAllImagesWithParentAsync()
    {
        var imagesListResponses = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters());
        foreach (var imagesListResponse in imagesListResponses)
        {
            var imageInspectResult = await _dockerClient.Images.InspectImageAsync(imagesListResponse.ID);
            if (imageInspectResult.Parent is not null)
            {
                yield return (Id: imageInspectResult.ID, ParentId: imageInspectResult.Parent);
            }
        }
    }

    public async Task<List<Image>> QueryByImageConfigAsync(port.Config.Config.ImageConfig imageConfig) =>
        await QueryByImageConfigAsync(imageConfig, _config.ImageConfigs);

    private async Task<List<Image>> QueryByImageConfigAsync(port.Config.Config.ImageConfig imageConfig,
        IReadOnlyCollection<port.Config.Config.ImageConfig> imageConfigs)
    {
        var imagesListResponses = await GetImagesByNameAsync(imageConfig.ImageName);
        var images = await GetBaseImagesAsync(imageConfig, imagesListResponses).ToListAsync();
        images.AddRange(await GetSnapshotImagesAsync(imageConfigs, imageConfig, imagesListResponses));
        images.AddRange(await GetUntaggedImagesAsync(imageConfig, imagesListResponses).ToListAsync());
        return images;
    }

    private static ImageGroup CreateImageGroup(List<Image> images, port.Config.Config.ImageConfig imageConfig)
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
            if (imagesById.TryGetValue(image.ParentId!, out var parent)) image.Parent = parent;
        }
    }

    private async Task<IEnumerable<Image>> GetSnapshotImagesAsync(
        IReadOnlyCollection<Config.Config.ImageConfig> imageConfigs,
        Config.Config.ImageConfig imageConfig, IEnumerable<ImagesListResponse> imagesListResponses)
    {
        return (await Task.WhenAll(imagesListResponses
            .Where(HasRepoTags)
            .Where(imagesListResponse => IsNotBase(imageConfigs, imagesListResponse))
            .Select(async imagesListResponse =>
            {
                if (!await IsSnapshotOfBaseAsync(imageConfig, imagesListResponse)) return null;
                var imageInspectResponse = await _dockerClient.Images.InspectImageAsync(imagesListResponse.ID);
                var labels = imageInspectResponse.Config.Labels;
                var (imageName, tag) = ImageNameHelper.GetImageNameAndTag(imagesListResponse.RepoTags.Single());
                var tagPrefix = labels.Where(l => l.Key == Constants.TagPrefix)
                    .Select(l => l.Value)
                    .SingleOrDefault();
                if (tagPrefix is not null && tag?.StartsWith(tagPrefix) == true) tag = tag[tagPrefix.Length..];
                var containers = await _getContainersQuery.QueryByImageIdAsync(imagesListResponse.ID).ToListAsync();
                return new Image(labels)
                {
                    Name = imageName,
                    Tag = tag,
                    IsSnapshot = true,
                    Existing = true,
                    Created = imagesListResponse.Created,
                    Containers = containers
                        .Where(c =>
                            tag != null &&
                            c.ContainerName == ContainerNameHelper.BuildContainerName(imageConfig.Identifier, tag))
                        .ToList(),
                    Id = imagesListResponse.ID,
                    ParentId = string.IsNullOrEmpty(imageInspectResponse.Parent) ? null : imageInspectResponse.Parent
                };
            }))).OfType<Image>();
    }

    private async IAsyncEnumerable<Image> GetBaseImagesAsync(Config.Config.ImageConfig imageConfig,
        IList<ImagesListResponse> imagesListResponses)
    {
        foreach (var tag in imageConfig.ImageTags)
        {
            var imagesListResponse = imagesListResponses
                .SingleOrDefault(e =>
                    e.RepoTags != null &&
                    e.RepoTags.Contains(ImageNameHelper.BuildImageName(imageConfig.ImageName, tag)));
            List<Container> containers;
            if (imagesListResponse != null)
            {
                containers = await _getContainersQuery.QueryByImageIdAsync(imagesListResponse.ID).ToListAsync();
                if (containers.Count == 0)
                {
                    containers = await _getContainersQuery
                        .QueryByContainerNameAsync(ContainerNameHelper.BuildContainerName(imageConfig.Identifier, tag))
                        .ToListAsync();
                }
            }
            else
            {
                containers = [];
            }

            var cleanedTag = tag;
            var imageInspectResponse = imagesListResponse != null
                ? await _dockerClient.Images.InspectImageAsync(imagesListResponse.ID)
                : null;
            var labels = imageInspectResponse?.Config?.Labels ?? new Dictionary<string, string>();
            var tagPrefix = labels.Where(l => l.Key == Constants.TagPrefix)
                .Select(l => l.Value)
                .SingleOrDefault();
            if (tagPrefix is not null && cleanedTag.StartsWith(tagPrefix)) cleanedTag = cleanedTag[tagPrefix.Length..];
            yield return new Image(labels)
            {
                Name = imageConfig.ImageName,
                Tag = cleanedTag,
                IsSnapshot = false,
                Existing = imagesListResponse != null,
                Created = imagesListResponse?.Created,
                Containers = containers
                    .Where(c =>
                        c.ContainerName == ContainerNameHelper.BuildContainerName(imageConfig.Identifier, tag))
                    .ToList(),
                Id = imagesListResponse?.ID,
                ParentId = string.IsNullOrEmpty(imageInspectResponse?.Parent) ? null : imageInspectResponse.Parent
            };
        }
    }

    private async IAsyncEnumerable<Image> GetUntaggedImagesAsync(Config.Config.ImageConfig imageConfig,
        IEnumerable<ImagesListResponse> imagesListResponses)
    {
        foreach (var imagesListResponse in imagesListResponses.Where(e => !e.RepoTags.Any()))
        {
            var containers = await _getContainersQuery.QueryByImageIdAsync(imagesListResponse.ID).ToListAsync();
            var imageInspectResponse = await _dockerClient.Images.InspectImageAsync(imagesListResponse.ID);
            yield return new Image(imageInspectResponse.Config.Labels)
            {
                Name = imageConfig.ImageName,
                Tag = null,
                IsSnapshot = false,
                Existing = true,
                Created = imagesListResponse.Created,
                Containers = containers
                    .Where(c =>
                        c.ImageIdentifier == imageConfig.ImageName
                        && c.ImageTag == null).ToList(),
                Id = imagesListResponse.ID,
                ParentId = string.IsNullOrEmpty(imagesListResponse.ParentID) ? null : imagesListResponse.ParentID
            };
        }
    }

    private async Task<IList<ImagesListResponse>> GetImagesByNameAsync(string imageName)
    {
        var parameters = new ImagesListParameters { Filters = new Dictionary<string, IDictionary<string, bool>>() };
        parameters.Filters.Add("reference", new Dictionary<string, bool> { { imageName, true } });
        return await _dockerClient.Images.ListImagesAsync(parameters);
    }

    private static bool HasRepoTags(ImagesListResponse e)
    {
        return e.RepoTags != null;
    }

    private static bool IsNotBase(IEnumerable<port.Config.Config.ImageConfig> imageConfigs, ImagesListResponse e)
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

    private async Task<bool> IsSnapshotOfBaseAsync(port.Config.Config.ImageConfig imageConfig, ImagesListResponse e)
    {
        var imageNameAndTags = imageConfig.ImageTags.Select(tag => new
        {
            imageConfig.ImageName,
            tag
        }).Select(imageConfig1 => ImageNameHelper.BuildImageName(imageConfig1.ImageName, imageConfig1.tag));
        var imageInspectResponse = await _dockerClient.Images.InspectImageAsync(e.ID);
        var identifier = imageInspectResponse.Config.Labels?.Where(l => l.Key == Constants.IdentifierLabel)
            .Select(l => l.Value)
            .SingleOrDefault();
        if (identifier is not null) return imageConfig.Identifier == identifier;
        return e.RepoTags.Any(repoTag => imageNameAndTags.Any(repoTag.StartsWith));
    }
}