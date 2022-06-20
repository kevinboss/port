using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class GetImageQuery : IGetImageQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly IGetRunningContainerQuery _getRunningContainerQuery;

    public GetImageQuery(IDockerClient dockerClient, IGetRunningContainerQuery getRunningContainerQuery)
    {
        _dockerClient = dockerClient;
        _getRunningContainerQuery = getRunningContainerQuery;
    }

    public async Task<Image?> QueryAsync(string imageName, string? tag)
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
        var imagesListResponse = imagesListResponses
            .SingleOrDefault(e =>
                tag == null && e.RepoTags == null
                || e.RepoTags != null && e.RepoTags.Contains(ImageNameHelper.JoinImageNameAndTag(imageName, tag)));
        if (imagesListResponse == null)
        {
            return null;
        }

        var runningContainer = await _getRunningContainerQuery.QueryAsync();
        return await ConvertToImage(imageName, tag, imagesListResponse, runningContainer);
    }

    private async Task<Image?> QueryAsync(string imageName, string id, Container? runningContainer)
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
        return await ConvertToImage(imageName1, tag, imagesListResponse, runningContainer);
    }

    private async Task<Image?> ConvertToImage(string imageName, string? tag, ImagesListResponse imagesListResponse,
        Container? runningContainer)
    {
        var running
            = runningContainer != null
              && imageName == runningContainer.ImageName
              && tag == runningContainer.ContainerTag;
        var runningUntaggedImage
            = running
              && runningContainer != null
              && runningContainer.ImageTag != tag;
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
                : await QueryAsync(imageName,imagesListResponse.ParentID, runningContainer)
        };
    }
}