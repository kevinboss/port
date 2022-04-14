using Docker.DotNet.Models;
using Spectre.Console.Cli;

namespace dcma;

public class RunCommand : AsyncCommand<RunSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        var image = Config.Instance.Images.SingleOrDefault(e => e.ImageAlias == settings.ImageAlias);
        if (image == null)
        {
            throw new InvalidOperationException();
        }

        var containerExists = await DoesContainerExistAsync(image);
        if (!containerExists)
        {
            var imageExists = await DoesImageExistAsync(image);
            if (!imageExists)
            {
                await CreateImage(image);
            }

            await CreateContainerAsync(image);
        }

        await RunContainerAsync(image);

        return 0;
    }

    private static async Task<bool> DoesContainerExistAsync(Image image)
    {
        if (image.ImageAlias == null)
        {
            throw new InvalidOperationException();
        }

        return await DockerClient.Instance.Containers.ListContainersAsync(new ContainersListParameters
        {
            Limit = 1,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                {
                    "name", new Dictionary<string, bool>
                    {
                        { image.ImageAlias, true }
                    }
                }
            }
        }).ContinueWith(task => task.Result.Any());
    }


    private static async Task<bool> DoesImageExistAsync(Image image)
    {
        if (image.ImageName == null)
        {
            throw new InvalidOperationException();
        }

        return await DockerClient.Instance.Images.ListImagesAsync(new ImagesListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                {
                    "reference", new Dictionary<string, bool>
                    {
                        { image.ImageName, true }
                    }
                }
            }
        }).ContinueWith(task => task.Result.Any());
    }

    private Task CreateImage(Image image)
    {
        return DockerClient.Instance.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = image.ImageName,
                Tag = image.ImageTag,
            },
            null,
            new Progress<JSONMessage>());
    }

    private static Task CreateContainerAsync(Image image)
    {
        return DockerClient.Instance.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = image.ImageAlias,
            Image = image.ImageName,
            HostConfig = new HostConfig()
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    {
                        image.PortFrom.ToString(), new List<PortBinding>
                        {
                            new()
                            {
                                HostPort = image.PortTo.ToString()
                            }
                        }
                    }
                }
            }
        });
    }

    private Task RunContainerAsync(Image image)
    {
        return DockerClient.Instance.Containers.StartContainerAsync(
            image.ImageAlias,
            new ContainerStartParameters()
        );
    }
}