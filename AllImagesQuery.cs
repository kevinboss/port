using Docker.DotNet.Models;

namespace dcma;

internal class AllImagesQuery : IAllImagesQuery
{
    public async IAsyncEnumerable<ImageGroup> QueryAsync()
    {
        var images = Services.Config.Value.Images;
        foreach (var imageConfig in images)
        {
            if (imageConfig.Identifier == null)
            {
                throw new InvalidOperationException();
            }

            if (imageConfig.ImageName == null)
            {
                throw new InvalidOperationException();
            }

            var imagesListResponses = await Services.DockerClient.Value.Images.ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "reference", new Dictionary<string, bool>
                        {
                            { imageConfig.ImageName, true }
                        }
                    }
                }
            });
            yield return new ImageGroup
            {
                Identifier = imageConfig.Identifier,
                Images = imagesListResponses
                    .Where(e => !e.RepoTags.Contains(DockerHelper
                        .JoinImageNameAndTag(imageConfig.ImageName, imageConfig.ImageTag)))
                    .Select(e =>
                    {
                        var (imageName, tag) = DockerHelper.GetImageNameAndTag(e.RepoTags.First());
                        return new Image
                        {
                            Identifier = imageConfig.Identifier,
                            Name = imageName,
                            Tag = tag,
                            IsSnapshot = true
                        };
                    })
                    .Concat(new List<Image>
                    {
                        new()
                        {
                            Identifier = imageConfig.Identifier,
                            Name = imageConfig.ImageName,
                            Tag = imageConfig.ImageTag,
                            IsSnapshot = false
                        }
                    }).ToList()
            };
        }
    }
}