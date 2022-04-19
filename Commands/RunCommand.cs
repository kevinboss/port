using Docker.DotNet.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dcma.Commands;

public class RunCommand : AsyncCommand<RunSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        settings.ImageIdentifier ??= PromptHelper.GetImageAliasFromUser();

        var image = Services.Config.Value.Images.SingleOrDefault(e => e.Identifier == settings.ImageIdentifier);
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

    private static async Task TerminateOtherContainers(Image image)
    {
        var containerNames
            = Services.Config.Value.Images
                .Where(e => e.Identifier != null && e.Identifier != image.Identifier)
                .Select(e => e.Identifier)!
                .ToList<string>();
        await DockerClientFacade.TerminateContainers(containerNames);
    }

    private static async Task LaunchImageAsync(Image image)
    {
        if (image.Identifier == null)
        {
            throw new InvalidOperationException();
        }

        var containerListResponse = await DockerClientFacade.GetContainerAsync(image.Identifier);
        if (containerListResponse != null && containerListResponse.Image != image.ImageName)
        {
            var id = containerListResponse.ID;
            await DockerClientFacade.RemoveContainerAsync(id);
            containerListResponse = null;
        }

        if (containerListResponse == null)
        {
            if (image.ImageName == null)
            {
                throw new InvalidOperationException();
            }
            
            if (image.ImageTag == null)
            {
                throw new InvalidOperationException();
            }

            var imagesListResponse = await DockerClientFacade.GetImageAsync(image.ImageName);
            if (imagesListResponse == null)
            {
                await DockerClientFacade.CreateImage(image.ImageName, image.ImageTag);
            }

            await DockerClientFacade.CreateContainerAsync(image.Identifier, image.ImageName, image.ImageTag, image.PortFrom, image.PortTo);
        }

        await DockerClientFacade.RunContainerAsync(image.Identifier);
    }
}