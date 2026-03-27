using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class AllImagesQuery : IAllImagesQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly port.Config.Config _config;
    private readonly IGetContainersQuery _getContainersQuery;

    public AllImagesQuery(
        IDockerClient dockerClient,
        port.Config.Config config,
        IGetContainersQuery getContainersQuery
    )
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
        var imagesListResponses = await _dockerClient.Images.ListImagesAsync(
            new ImagesListParameters()
        );
        foreach (var imagesListResponse in imagesListResponses)
        {
            var imageInspectResult = await _dockerClient.Images.InspectImageAsync(
                imagesListResponse.ID
            );
            if (imageInspectResult.Parent is not null)
            {
                yield return (Id: imageInspectResult.ID, ParentId: imageInspectResult.Parent);
            }
        }
    }

    public async Task<List<Image>> QueryByImageConfigAsync(
        port.Config.Config.ImageConfig imageConfig
    ) => await QueryByImageConfigAsync(imageConfig, _config.ImageConfigs);

    private async Task<List<Image>> QueryByImageConfigAsync(
        port.Config.Config.ImageConfig imageConfig,
        IReadOnlyCollection<port.Config.Config.ImageConfig> imageConfigs
    )
    {
        var imagesListResponses = await GetImagesByNameAsync(imageConfig.ImageName);
        var danglingImages = await GetDanglingImagesByNameAsync(
            imageConfig.ImageName,
            imageConfig.Identifier
        );
        var allImagesListResponses = imagesListResponses
            .Concat(danglingImages.Where(d => imagesListResponses.All(i => i.ID != d.ID)))
            .ToList();
        var images = await GetAllTagsAsync(imageConfig, imageConfigs, allImagesListResponses)
            .ToListAsync();
        images.AddRange(
            await GetSnapshotImagesAsync(imageConfigs, imageConfig, allImagesListResponses)
        );
        images.AddRange(
            await GetUntaggedImagesAsync(imageConfig, allImagesListResponses).ToListAsync()
        );
        return images;
    }

    private static ImageGroup CreateImageGroup(
        List<Image> images,
        port.Config.Config.ImageConfig imageConfig
    )
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
            if (image.ParentId == null)
                continue;
            if (imagesById.TryGetValue(image.ParentId!, out var parent))
                image.Parent = parent;
        }
    }

    private async Task<IEnumerable<Image>> GetSnapshotImagesAsync(
        IReadOnlyCollection<Config.Config.ImageConfig> imageConfigs,
        Config.Config.ImageConfig imageConfig,
        IEnumerable<ImagesListResponse> imagesListResponses
    )
    {
        return (
            await Task.WhenAll(
                imagesListResponses
                    .Where(HasRepoTags)
                    .Where(imagesListResponse => IsNotBase(imageConfigs, imagesListResponse))
                    .Select(async imagesListResponse =>
                    {
                        if (!await IsSnapshotOfBaseAsync(imageConfig, imagesListResponse))
                            return null;
                        var imageInspectResponse = await _dockerClient.Images.InspectImageAsync(
                            imagesListResponse.ID
                        );
                        var labels = imageInspectResponse.Config.Labels;
                        var (imageName, tag) = ImageNameHelper.GetImageNameAndTag(
                            imagesListResponse.RepoTags.Single()
                        );
                        var tagPrefix = labels
                            .Where(l => l.Key == Constants.TagPrefix)
                            .Select(l => l.Value)
                            .SingleOrDefault();
                        if (tagPrefix is not null && tag?.StartsWith(tagPrefix) == true)
                            tag = tag[tagPrefix.Length..];
                        var containers = await _getContainersQuery
                            .QueryByImageIdAsync(imagesListResponse.ID)
                            .ToListAsync();
                        return new Image(labels)
                        {
                            Name = imageName,
                            Tag = tag,
                            IsSnapshot = true,
                            Existing = true,
                            Created = imageInspectResponse.Created,
                            Containers = containers
                                .Where(c =>
                                    tag != null
                                    && c.ContainerName
                                        == ContainerNameHelper.BuildContainerName(
                                            imageConfig.Identifier,
                                            tag
                                        )
                                )
                                .ToList(),
                            Id = imagesListResponse.ID,
                            ParentId = string.IsNullOrEmpty(imageInspectResponse.Parent)
                                ? null
                                : imageInspectResponse.Parent,
                        };
                    })
            )
        ).OfType<Image>();
    }

    private async IAsyncEnumerable<Image> GetAllTagsAsync(
        Config.Config.ImageConfig imageConfig,
        IReadOnlyCollection<Config.Config.ImageConfig> imageConfigs,
        IList<ImagesListResponse> imagesListResponses
    )
    {
        var yieldedTags = new HashSet<string>();
        var yieldedImageIds = new HashSet<string>();

        foreach (var tag in imageConfig.ImageTags)
        {
            yieldedTags.Add(ImageNameHelper.BuildImageName(imageConfig.ImageName, tag));

            var expectedImageRef = ImageNameHelper.BuildImageName(imageConfig.ImageName, tag);
            var imagesListResponse = imagesListResponses.SingleOrDefault(e =>
                (e.RepoTags != null && e.RepoTags.Any(t => t == expectedImageRef))
                || (e.RepoDigests != null && e.RepoDigests.Any(d => d == expectedImageRef))
            );

            List<Container> containers;
            if (imagesListResponse != null)
            {
                containers = await _getContainersQuery
                    .QueryByImageIdAsync(imagesListResponse.ID)
                    .ToListAsync();
                yieldedImageIds.Add(imagesListResponse.ID);
            }
            else
            {
                containers = [];
            }

            var cleanedTag = tag;
            var imageInspectResponse =
                imagesListResponse != null
                    ? await _dockerClient.Images.InspectImageAsync(imagesListResponse.ID)
                    : null;
            var labels = imageInspectResponse?.Config?.Labels ?? new Dictionary<string, string>();
            var tagPrefix = labels
                .Where(l => l.Key == Constants.TagPrefix)
                .Select(l => l.Value)
                .SingleOrDefault();
            if (tagPrefix is not null && cleanedTag.StartsWith(tagPrefix))
                cleanedTag = cleanedTag[tagPrefix.Length..];

            yield return new Image(labels)
            {
                Name = imageConfig.ImageName,
                Tag = cleanedTag,
                IsSnapshot = false,
                Existing = imagesListResponse != null,
                Created = imageInspectResponse?.Created,
                Containers = containers
                    .Where(c =>
                        c.ContainerName
                        == ContainerNameHelper.BuildContainerName(imageConfig.Identifier, tag)
                    )
                    .ToList(),
                Id = imagesListResponse?.ID,
                ParentId = string.IsNullOrEmpty(imageInspectResponse?.Parent)
                    ? null
                    : imageInspectResponse.Parent,
            };
        }

        foreach (var imagesListResponse in imagesListResponses.Where(HasRepoTags))
        {
            if (IsNotBase(imageConfigs, imagesListResponse))
            {
                var imageInspectResponse = await _dockerClient.Images.InspectImageAsync(
                    imagesListResponse.ID
                );
                var snapshotIdentifier = imageInspectResponse
                    .Config.Labels?.Where(l => l.Key == Constants.IdentifierLabel)
                    .Select(l => l.Value)
                    .SingleOrDefault();
                if (snapshotIdentifier is not null && snapshotIdentifier != imageConfig.Identifier)
                    continue;
                if (snapshotIdentifier == imageConfig.Identifier)
                    continue;
            }

            if (yieldedImageIds.Contains(imagesListResponse.ID))
                continue;

            foreach (var repoTag in imagesListResponse.RepoTags)
            {
                if (yieldedTags.Contains(repoTag))
                    continue;

                var (imageName, tag) = ImageNameHelper.GetImageNameAndTag(repoTag);

                if (imageName != imageConfig.ImageName)
                    continue;

                yieldedTags.Add(repoTag);
                yieldedImageIds.Add(imagesListResponse.ID);

                var containers = await _getContainersQuery
                    .QueryByImageIdAsync(imagesListResponse.ID)
                    .ToListAsync();
                var imageInspectResponse = await _dockerClient.Images.InspectImageAsync(
                    imagesListResponse.ID
                );
                var labels =
                    imageInspectResponse.Config?.Labels ?? new Dictionary<string, string>();

                yield return new Image(labels)
                {
                    Name = imageName,
                    Tag = tag,
                    IsSnapshot = false,
                    Existing = true,
                    Created = imageInspectResponse.Created,
                    Containers = containers
                        .Where(c =>
                            c.ContainerName
                            == ContainerNameHelper.BuildContainerName(imageConfig.Identifier, tag)
                        )
                        .ToList(),
                    Id = imagesListResponse.ID,
                    ParentId = string.IsNullOrEmpty(imageInspectResponse.Parent)
                        ? null
                        : imageInspectResponse.Parent,
                };
            }
        }
    }

    private async IAsyncEnumerable<Image> GetUntaggedImagesAsync(
        Config.Config.ImageConfig imageConfig,
        IEnumerable<ImagesListResponse> imagesListResponses
    )
    {
        foreach (var imagesListResponse in imagesListResponses.Where(IsUntagged))
        {
            var digest =
                imagesListResponse
                    .RepoDigests?.Where(d => d.StartsWith($"{imageConfig.ImageName}@"))
                    .Select(d =>
                    {
                        ImageNameHelper.TryGetImageNameAndTag(d, out var parsed);
                        return parsed.tag;
                    })
                    .FirstOrDefault()
                ?? imagesListResponse.ID;

            var containers = await _getContainersQuery
                .QueryByImageIdAsync(imagesListResponse.ID)
                .ToListAsync();
            var imageInspectResponse = await _dockerClient.Images.InspectImageAsync(
                imagesListResponse.ID
            );
            var originalTag = containers
                .Select(c => c.GetLabel(Constants.BaseTagLabel))
                .FirstOrDefault(t => t != null && !ImageNameHelper.IsDigest(t));
            yield return new Image(imageInspectResponse.Config.Labels)
            {
                Name = imageConfig.ImageName,
                Tag = digest,
                OriginalTag = originalTag,
                IsSnapshot = false,
                Existing = true,
                Created = imageInspectResponse.Created,
                Containers = containers.ToList(),
                Id = imagesListResponse.ID,
                ParentId = string.IsNullOrEmpty(imagesListResponse.ParentID)
                    ? null
                    : imagesListResponse.ParentID,
            };
        }
    }

    private async Task<IList<ImagesListResponse>> GetImagesByNameAsync(string imageName)
    {
        var parameters = new ImagesListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>(),
        };
        parameters.Filters.Add("reference", new Dictionary<string, bool> { { imageName, true } });
        return await _dockerClient.Images.ListImagesAsync(parameters);
    }

    private async Task<IList<ImagesListResponse>> GetDanglingImagesByNameAsync(
        string imageName,
        string identifier
    )
    {
        // Query all images (not just dangling) to also catch untagged images that still have RepoDigests
        var allImages = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters());

        var result = new List<ImagesListResponse>();
        foreach (var image in allImages.Where(IsUntagged))
        {
            // Match by RepoDigests
            if (
                image.RepoDigests != null
                && image.RepoDigests.Any(d => d.StartsWith($"{imageName}@"))
            )
            {
                result.Add(image);
                continue;
            }

            // Match by containers with matching identifier
            var containers = await _getContainersQuery.QueryByImageIdAsync(image.ID).ToListAsync();
            if (containers.Any(c => c.ContainerIdentifier == identifier))
            {
                result.Add(image);
            }
        }

        return result;
    }

    private static bool HasRepoTags(ImagesListResponse e)
    {
        return e.RepoTags != null && !IsUntagged(e);
    }

    private static bool IsUntagged(ImagesListResponse e)
    {
        return e.RepoTags == null || !e.RepoTags.Any() || e.RepoTags.All(t => t == "<none>:<none>");
    }

    private static bool IsNotBase(
        IEnumerable<port.Config.Config.ImageConfig> imageConfigs,
        ImagesListResponse e
    )
    {
        return !imageConfigs
            .SelectMany(imageConfig =>
                imageConfig.ImageTags.Select(tag => new { imageConfig.ImageName, tag })
            )
            .Any(imageConfig =>
            {
                var imageNameAndTag = ImageNameHelper.BuildImageName(
                    imageConfig.ImageName,
                    imageConfig.tag
                );
                return e.RepoTags.Any(repoTag => repoTag.Contains(imageNameAndTag));
            });
    }

    private async Task<bool> IsSnapshotOfBaseAsync(
        port.Config.Config.ImageConfig imageConfig,
        ImagesListResponse e
    )
    {
        var imageNameAndTags = imageConfig
            .ImageTags.Select(tag => new { imageConfig.ImageName, tag })
            .Select(imageConfig1 =>
                ImageNameHelper.BuildImageName(imageConfig1.ImageName, imageConfig1.tag)
            );
        var imageInspectResponse = await _dockerClient.Images.InspectImageAsync(e.ID);
        var identifier = imageInspectResponse
            .Config.Labels?.Where(l => l.Key == Constants.IdentifierLabel)
            .Select(l => l.Value)
            .SingleOrDefault();
        if (identifier is not null)
            return imageConfig.Identifier == identifier;
        return e.RepoTags.Any(repoTag => imageNameAndTags.Any(repoTag.StartsWith));
    }
}
