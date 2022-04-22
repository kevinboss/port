using Docker.DotNet.Models;

namespace dcma;

public static class DockerClientFacade
{
    public static Task CreateImageAsync(string? imageName, string? imageTag)
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

    public static async Task CreateImageFromContainerAsync(ContainerListResponse containerToCommit, string? identifier)
    {
        await Services.DockerClient.Value.Images.CommitContainerChangesAsync(new CommitContainerChangesParameters
        {
            ContainerID = containerToCommit.ID,
            RepositoryName = DockerHelper.GetImageNameAndTag(containerToCommit.Image).ImageName,
            Tag = identifier
        });
    }

    public static Task<IList<ImagesListResponse>> GetImagesAndChildrenAsync(string imageName)
    {
       return Services.DockerClient.Value.Images.ListImagesAsync(new ImagesListParameters
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
    }

    public static Task<ImagesListResponse?> GetImageAsync(string imageName)
    {
        return Services.DockerClient.Value.Images.ListImagesAsync(new ImagesListParameters
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
        }).ContinueWith(task => task.Result.SingleOrDefault());
    }

    public static async Task<ContainerListResponse?> GetContainerAsync(string containerName)
    {
        var containerListResponses = await Services.DockerClient.Value.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "name", new Dictionary<string, bool>
                        {
                            { $"/{containerName}", true }
                        }
                    }
                }
            });
        return containerListResponses.SingleOrDefault(e => e.Names.Any(name => name == $"/{containerName}"));
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

        var runningContainers = await Services.DockerClient.Value.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = long.MaxValue
            }).ContinueWith(task => task.Result.Where(e => e.State == "running"));
        var candidates = runningContainers.Where(e => imageNames.Contains(DockerHelper.GetImageNameAndTag(e.Image).ImageName));
        var containerToCommit = candidates.SingleOrDefault();
        return containerToCommit;
    }

    public static Task CreateContainerAsync(string imageIdentifier, string imageName, string tag, int portFrom, int portTo)
    {
        return Services.DockerClient.Value.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = imageIdentifier,
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

    public static Task RunContainerAsync(string imageIdentifier)
    {
        return Services.DockerClient.Value.Containers.StartContainerAsync(
            imageIdentifier,
            new ContainerStartParameters()
        );
    }

    public static async Task TerminateContainers(IEnumerable<string> containerNames)
    {
        var containerNameParams = containerNames.ToDictionary(s => s, _ => true);
        var containers = await Services.DockerClient.Value.Containers
            .ListContainersAsync(new ContainersListParameters
            {
                Limit = long.MaxValue,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "name", containerNameParams
                    }
                }
            });

        foreach (var containerListResponse in containers)
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