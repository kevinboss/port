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
            var imageExists = await DockerClientFacade.DoesImageExistAsync(image.ImageName);
            if (!imageExists)
            {
                await DockerClientFacade.CreateImage(image.ImageName, image.ImageTag);
            }

            await DockerClientFacade.CreateContainerAsync(image.Identifier, image.ImageName, image.PortFrom, image.PortTo);
        }

        await DockerClientFacade.RunContainerAsync(image.Identifier);
    }
}