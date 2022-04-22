using Docker.DotNet.Models;

namespace dcma;

public static class DockerClientFacade
{
    public static Task CreateImageAsync(string? imageName, string imageTag)
    {
        return Services.DockerClient.Value.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = imageName,
                Tag = imageTag
            },
            null,
            new Progress<JSONMessage>());
    }

    public static async Task CreateImageFromContainerAsync(ContainerListResponse containerToCommit, string? tag)
    {
        await Services.DockerClient.Value.Images.CommitContainerChangesAsync(new CommitContainerChangesParameters
        {
            ContainerID = containerToCommit.ID,
            RepositoryName = DockerHelper.GetImageNameAndTag(containerToCommit.Image).imageName,
            Tag = tag
        });
    }

    public static async Task<ImagesListResponse?> GetImageAsync(string imageName, string tag)
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

    public static async Task<ContainerListResponse?> GetContainerAsync(string imageName, string tag)
    {
        var containers = await Services.DockerClient.Value.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        return containers.SingleOrDefault(e =>
        {
            var (containerImageName, containerImageTag) = DockerHelper.GetImageNameAndTag(e.Image);
            return imageName == containerImageName && tag == containerImageTag;
        });
    }

    public static async Task<ContainerListResponse?> GetRunningContainersAsync()
    {
        var images = Services.Config.Value.Images;
        var imageNames = new List<string>();
        foreach (var image in images)
        {
            if (image.ImageName == null) continue;
            imageNames.Add(image.ImageName);
        }

        var containers = await Services.DockerClient.Value.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            });
        return containers.SingleOrDefault(e =>
            e.State == "running" && imageNames.Contains(DockerHelper.GetImageNameAndTag(e.Image).imageName));
    }

    public static Task CreateContainerAsync(string identifier, string imageName, string tag, int portFrom, int portTo)
    {
        return Services.DockerClient.Value.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = $"{identifier}.{tag}",
            Image = $"{imageName}:{tag}",
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    {
                        portFrom.ToString(), new List<PortBinding>
                        {
                            new()
                            {
                                HostPort = portTo.ToString()
                            }
                        }
                    }
                }
            }
        });
    }

    public static Task RunContainerAsync(string identifier, string tag)
    {
        return Services.DockerClient.Value.Containers.StartContainerAsync(
            $"{identifier}.{tag}",
            new ContainerStartParameters()
        );
    }

    public static async Task TerminateContainers(IEnumerable<(string imageName, string tag)> imageNames)
    {
        var containers = await Services.DockerClient.Value.Containers
            .ListContainersAsync(new ContainersListParameters
            {
                Limit = long.MaxValue
            });

        foreach (var containerListResponse in containers
                     .Where(e =>
                     {
                         var (containerImageName, containerImageTag) = DockerHelper.GetImageNameAndTag(e.Image);
                         return imageNames.Any(imageNameAndTag =>
                             imageNameAndTag.imageName == containerImageName &&
                             imageNameAndTag.tag == containerImageTag);
                     }))
        {
            await Services.DockerClient.Value.Containers.StopContainerAsync(containerListResponse.ID,
                new ContainerStopParameters());
        }
    }

    public static async Task RemoveContainerAsync(string id)
    {
        await Services.DockerClient.Value.Containers.StopContainerAsync(id,
            new ContainerStopParameters());
        await Services.DockerClient.Value.Containers.RemoveContainerAsync(id,
            new ContainerRemoveParameters());
    }
}