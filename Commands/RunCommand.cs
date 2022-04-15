using Docker.DotNet.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dcma.Commands;

public class RunCommand : AsyncCommand<RunSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        settings.ImageAlias ??= GetImageAliasFromUser();

        var image = Services.Config.Value.Images.SingleOrDefault(e => e.Identifier == settings.ImageAlias);
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

    private static string GetImageAliasFromUser()
    {
        var selectionPrompt = new SelectionPrompt<string>()
            .PageSize(10)
            .Title("Select image you wish to [green]run[/]")
            .MoreChoicesText("[grey](Move up and down to reveal more images)[/]");
        foreach (var image in Services.Config.Value.Images)
        {
            if (image.Identifier != null) selectionPrompt.AddChoice(image.Identifier);
        }

        return AnsiConsole.Prompt(selectionPrompt);
    }

    private static async Task TerminateOtherContainers(Image image)
    {
        var containerNames
            = Services.Config.Value.Images
                .Where(e => e.Identifier != null && e.Identifier != image.Identifier)
                .Select(e => e.Identifier)!
                .ToList<string>();
        await DockerClientFacade.TerminateContainers(containerNames);
    }

    private async Task LaunchImageAsync(Image image)
    {
        if (image.Identifier == null)
        {
            throw new InvalidOperationException();
        }

        var containerListResponse = await DockerClientFacade.GetContainerAsync(image.Identifier);
        if (containerListResponse != null && containerListResponse.Image != image.ImageName)
        {
            await Services.DockerClient.Value.Containers.StopContainerAsync(containerListResponse.ID,
                new ContainerStopParameters());
            await Services.DockerClient.Value.Containers.RemoveContainerAsync(containerListResponse.ID,
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


    private static async Task<bool> DoesImageExistAsync(Image image)
    {
        if (image.ImageName == null)
        {
            throw new InvalidOperationException();
        }

        return await Services.DockerClient.Value.Images.ListImagesAsync(new ImagesListParameters
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

    private static Task CreateImage(Image image)
    {
        return Services.DockerClient.Value.Images.CreateImageAsync(
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
        return Services.DockerClient.Value.Containers.CreateContainerAsync(new CreateContainerParameters
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

    private static Task RunContainerAsync(Image image)
    {
        return Services.DockerClient.Value.Containers.StartContainerAsync(
            image.Identifier,
            new ContainerStartParameters()
        );
    }
}