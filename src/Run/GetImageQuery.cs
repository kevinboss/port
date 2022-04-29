using Docker.DotNet.Models;

namespace dcma.Run;

internal class GetImageQuery : IGetImageQuery
{
    public async Task<ImagesListResponse?> QueryAsync(string imageName, string tag)
    {
        var imagesListResponses = await Services.DockerClient.Value.Images.ListImagesAsync(new ImagesListParameters
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
        return imagesListResponses.SingleOrDefault(e => e.RepoTags.Contains(DockerHelper
            .JoinImageNameAndTag(imageName, tag)));
    }
}