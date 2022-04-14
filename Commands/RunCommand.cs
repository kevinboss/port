using Docker.DotNet.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dcma.Commands;

public class RunCommand : AsyncCommand<RunSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        if (settings.ImageAlias == null)
        {
            GetImageAliasFromUser(settings);
        }

        var image = Config.Instance.Images.SingleOrDefault(e => e.Identifier == settings.ImageAlias);
        if (image == null)
        {
            throw new InvalidOperationException();
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Terminating containers of other images", _ => TerminateOtherContainers(image));
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Launching {image.Identifier}", _ => LaunchImageAsync(image));
        return 0;
    }

    private static void GetImageAliasFromUser(RunSettings settings)
    {
        var selectionPrompt = new SelectionPrompt<string>()
            .PageSize(10)
            .Title("Select image you wish to [green]run[/]")
            .MoreChoicesText("[grey](Move up and down to reveal more images)[/]");
        foreach (var image in Config.Instance.Images)
        {
            if (image.Identifier != null) selectionPrompt.AddChoice(image.Identifier);
        }

        settings.ImageAlias = AnsiConsole.Prompt(selectionPrompt);
    }

    private async Task TerminateOtherContainers(Image image)
    {
        var containerNames = new Dictionary<string, bool>();
        foreach (var e in Config.Instance.Images)
        {
            if (e.Identifier != null && e.Identifier != image.Identifier) containerNames.Add(e.Identifier, true);
        }

        var containers = await DockerClient.Instance.Containers
            .ListContainersAsync(new ContainersListParameters
            {
                Limit = containerNames.Count,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "name", containerNames
                    }
                }
            });

        foreach (var containerListResponse in containers)
        {
            await DockerClient.Instance.Containers.StopContainerAsync(containerListResponse.ID,
                new ContainerStopParameters());
        }
    }

    private async Task LaunchImageAsync(Image image)
    {
        var containerListResponse = await GetContainerAsync(image);
        if (containerListResponse != null && containerListResponse.Image != image.ImageName)
        {
            await DockerClient.Instance.Containers.StopContainerAsync(containerListResponse.ID,
                new ContainerStopParameters());
            await DockerClient.Instance.Containers.RemoveContainerAsync(containerListResponse.ID,
                new ContainerRemoveParameters());
            containerListResponse = null;
        }

        if (containerListResponse == null)
        {
            var imageExists = await DoesImageExistAsync(image);
            if (!imageExists)
            {
                await CreateImage(image);
            }

            await CreateContainerAsync(image);
        }

        await RunContainerAsync(image);
    }

    private static async Task<ContainerListResponse?> GetContainerAsync(Image image)
    {
        if (image.Identifier == null)
        {
            throw new InvalidOperationException();
        }

        var containerListResponses = await DockerClient.Instance.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                Limit = 100,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "name", new Dictionary<string, bool>
                        {
                            { $"/{image.Identifier}", true }
                        }
                    }
                }
            });
        return containerListResponses.SingleOrDefault(e => e.Names.Any(name => name == $"/{image.Identifier}"));
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
            Name = image.Identifier,
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
            image.Identifier,
            new ContainerStartParameters()
        );
    }
}