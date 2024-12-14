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
                || e.RepoTags != null && e.RepoTags.Contains(ImageNameHelper.BuildImageName(imageName, tag)));
        if (imagesListResponse == null)
        {
            return null;
        }

        var containers = await _getContainersQuery.QueryByImageIdAsync(imagesListResponse.ID).ToListAsync();
        return await ConvertToImage(imagesListResponse.Labels, imageName, tag, imagesListResponse, containers);
    }

    private async Task<Image?> ConvertToImage(IDictionary<string, string>? labels, string imageName, string? tag,
        ImagesListResponse imagesListResponse,
        IReadOnlyCollection<Container> containers)
    {
        var image = new Image(labels ?? new Dictionary<string, string>())
        {
            Name = imageName,
            Tag = tag,
            IsSnapshot = false,
            Existing = true,
            Created = imagesListResponse.Created,
            Containers = containers.Where(c => imageName == c.ImageIdentifier && tag == c.ImageTag).ToList(),
            Id = imagesListResponse.ID,
            ParentId = string.IsNullOrEmpty(imagesListResponse.ParentID) ? null : imagesListResponse.ParentID
        };

        if (!string.IsNullOrEmpty(imagesListResponse.ParentID))
        {
            image.Parent = await QueryParent(imagesListResponse.ParentID, containers);
        }

        return image;
    }

    private async Task<Image?> QueryParent(string id, IReadOnlyCollection<Container> containers)
    {
        var imagesListResponses = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters());
        var imagesListResponse = imagesListResponses.SingleOrDefault(e => e.ID == id);

        if (imagesListResponse == null)
        {
            return null;
        }

        string? imageName = null;
        string? tag = null;

        if (imagesListResponse.RepoTags != null)
        {
            if (imagesListResponse.RepoTags.Count == 1)
            {
                (imageName, tag) = ImageNameHelper.GetImageNameAndTag(imagesListResponse.RepoTags.Single());
            }
            else
            {
                var baseTag = imagesListResponse.Labels.SingleOrDefault(l => l.Key == Constants.BaseTagLabel).Value;
                foreach (var repoTag in imagesListResponse.RepoTags)
                {
                    var (name, repoTag) = ImageNameHelper.GetImageNameAndTag(repoTag);
                    if (repoTag == baseTag)
                    {
                        imageName = name;
                        tag = repoTag;
                        break;
                    }
                }
            }
        }

        if (imageName == null)
        {
            var digest = imagesListResponse.RepoDigests?.SingleOrDefault();
            if (digest != null && DigestHelper.TryGetImageNameAndId(digest, out var nameNameAndId))
            {
                imageName = nameNameAndId.imageName;
            }
        }

        if (imageName != null)
        {
            return await ConvertToImage(imagesListResponse.Labels, imageName, tag, imagesListResponse, containers);
        }

        return null;
    }
}
