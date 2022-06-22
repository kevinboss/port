using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class GetImageQuery : IGetImageQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly IGetRunningContainersQuery _getRunningContainersQuery;

    public GetImageQuery(IDockerClient dockerClient, IGetRunningContainersQuery getRunningContainersQuery)
    {
        _dockerClient = dockerClient;
        _getRunningContainersQuery = getRunningContainersQuery;
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
                || e.RepoTags != null && e.RepoTags.Contains(ImageNameHelper.JoinImageNameAndTag(imageName, tag)));
        if (imagesListResponse == null)
        {
            return null;
        }

        var runningContainers = await _getRunningContainersQuery.QueryAsync();
        return await ConvertToImage(imageName, tag, imagesListResponse, runningContainers);
    }

    private async Task<Image?> QueryAsync(string imageName, string id, IReadOnlyCollection<Container> runningContainers)
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

        var imagesListResponse = imagesListResponses.SingleOrDefault(e => e.ID == id);

        if (imagesListResponse == null)
        {
            return null;
        }

        var (imageName1, tag) = ImageNameHelper.GetImageNameAndTag(imagesListResponse.RepoTags.Single());
        return await ConvertToImage(imageName1, tag, imagesListResponse, runningContainers);
    }

    private async Task<Image?> ConvertToImage(string imageName, string? tag, ImagesListResponse imagesListResponse,
        IReadOnlyCollection<Container> runningContainers)
    {
        var runningContainer = runningContainers
            .SingleOrDefault(c =>
                imageName == c.ImageName
                && tag == c.Tag);
        var running = runningContainer != null;
        var runningUntaggedImage
            = runningContainer != null && runningContainer.ImageTag != tag;
        return new Image
        {
            Name = imageName,
            Tag = tag,
            IsSnapshot = false,
            Existing = true,
            Created = imagesListResponse.Created,
            Running = running,
            RunningUntaggedImage = runningUntaggedImage,
            Id = imagesListResponse.ID,
            ParentId = string.IsNullOrEmpty(imagesListResponse.ParentID) ? null : imagesListResponse.ParentID,
            Parent = string.IsNullOrEmpty(imagesListResponse.ParentID)
                ? null
                : await QueryAsync(imageName,imagesListResponse.ParentID, runningContainers)
        };
    }
}