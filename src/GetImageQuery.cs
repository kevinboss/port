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
                tag == null && e.RepoTags == null
                || e.RepoTags != null && e.RepoTags.Contains(ImageNameHelper.BuildImageName(imageName, tag)));
        if (imagesListResponse == null)
        {
            return null;
        }

        var containers = await _getContainersQuery.QueryByImageIdAsync(imagesListResponse.ID).ToListAsync();
        return await ConvertToImage(imageName, tag, imagesListResponse, containers);
    }

    private async Task<Image?> ConvertToImage(string imageName, string? tag, ImagesListResponse imagesListResponse,
        IReadOnlyCollection<Container> containers)
    {
        var container = containers.SingleOrDefault(c => imageName == c.ImageIdentifier && tag == c.ImageTag);
        return new Image
        {
            Name = imageName,
            Tag = tag,
            IsSnapshot = false,
            Existing = true,
            Created = imagesListResponse.Created,
            Container = container,
            Id = imagesListResponse.ID,
            ParentId = string.IsNullOrEmpty(imagesListResponse.ParentID) ? null : imagesListResponse.ParentID,
            Parent = string.IsNullOrEmpty(imagesListResponse.ParentID)
                ? null
                : await QueryParent(imagesListResponse.ParentID, containers)
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

        if (imagesListResponse.RepoTags != null)
        {
            var (imageName1, tag) = ImageNameHelper.GetImageNameAndTag(imagesListResponse.RepoTags.Single());
            return await ConvertToImage(imageName1, tag, imagesListResponse, containers);
        }

        var digest = imagesListResponse.RepoDigests?.SingleOrDefault();
        if (digest != null && DigestHelper.TryGetImageNameAndId(digest, out var nameNameAndId))
            return await ConvertToImage(nameNameAndId.imageName, null, imagesListResponse, containers);

        return null;
    }
}