using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class GetImageQuery : IGetImageQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly IGetContainersQuery _getContainersQuery;

    public GetImageQuery(IDockerClient dockerClient, IGetContainersQuery getContainersQuery)
    {
        _dockerClient = dockerClient;
        _getContainersQuery = getContainersQuery;
    }

    public async Task<Image?> QueryAsync(string imageName, string? tag)
    {
        var parameters = new ImagesListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>()
        };
        parameters.Filters.Add("reference", new Dictionary<string, bool>
        {
            { imageName, true }
        });
        var imagesListResponses = await _dockerClient.Images.ListImagesAsync(parameters);
        var imagesListResponse = imagesListResponses
            .SingleOrDefault(e =>
                tag == null && !e.RepoTags.Any()
                || e.RepoTags != null && e.RepoTags.Any(repoTag =>
                    repoTag.Contains(ImageNameHelper.BuildImageName(imageName, tag))));
        if (imagesListResponse == null)
        {
            return null;
        }

        var containers = await _getContainersQuery.QueryByImageIdAsync(imagesListResponse.ID).ToListAsync();
        var imageInspectResponse = await _dockerClient.Images.InspectImageAsync(imagesListResponse.ID);
        return await ConvertToImage(imageInspectResponse.Config.Labels, imageName, tag, imagesListResponse, containers);
    }

    private async Task<Image?> ConvertToImage(IDictionary<string, string>? labels, string imageName, string? tag,
        ImagesListResponse imagesListResponse,
        IReadOnlyCollection<Container> containers)
    {
        var imageInspectResult = await _dockerClient.Images.InspectImageAsync(imagesListResponse.ID);
        return new Image(labels ?? new Dictionary<string, string>())
        {
            Name = imageName,
            Tag = tag,
            IsSnapshot = false,
            Existing = true,
            Created = imagesListResponse.Created,
            Containers = containers.Where(c => imageName == c.ImageIdentifier && tag == c.ImageTag).ToList(),
            Id = imagesListResponse.ID,
            ParentId = string.IsNullOrEmpty(imageInspectResult.Parent) ? null : imageInspectResult.Parent,
            Parent = string.IsNullOrEmpty(imageInspectResult.Parent)
                ? null
                : await QueryParent(imageInspectResult.Parent, containers)
        };
    }

    private async Task<Image?> QueryParent(string id, IReadOnlyCollection<Container> containers)
    {
        var imagesListResponses = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters());

        var imagesListResponse = imagesListResponses.SingleOrDefault(e => e.ID == id);

        if (imagesListResponse == null)
        {
            return null;
        }

        var imageInspectResponse = await _dockerClient.Images.InspectImageAsync(imagesListResponse.ID);
        var labels = imageInspectResponse.Config.Labels;

        if (imagesListResponse.RepoTags != null)
        {
            if (imagesListResponse.RepoTags.Count == 1)
            {
                var (imageName1, tag) = ImageNameHelper.GetImageNameAndTag(imagesListResponse.RepoTags.Single());
                return await ConvertToImage(labels, imageName1, tag, imagesListResponse,
                    containers);
            }

            var baseTag = labels.SingleOrDefault(l => l.Key == Constants.BaseTagLabel).Value;
            foreach (var repoTag in imagesListResponse.RepoTags)
            {
                var (imageName1, tag) = ImageNameHelper.GetImageNameAndTag(repoTag);
                if (tag == baseTag)
                    return await ConvertToImage(labels, imageName1, tag, imagesListResponse,
                        containers);
            }
        }

        var digest = imagesListResponse.RepoDigests?.SingleOrDefault();
        if (digest != null && DigestHelper.TryGetImageNameAndId(digest, out var nameNameAndId))
            return await ConvertToImage(labels, nameNameAndId.imageName, null, imagesListResponse,
                containers);

        return null;
    }
}