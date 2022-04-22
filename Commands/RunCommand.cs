using Docker.DotNet.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dcma.Commands;

public class RunCommand : AsyncCommand<RunSettings>
{
    private readonly IPromptHelper _promptHelper;
    private readonly IAllImagesQuery _allImagesQuery;

    public RunCommand(IAllImagesQuery allImagesQuery, IPromptHelper promptHelper)
    {
        _allImagesQuery = allImagesQuery;
        _promptHelper = promptHelper;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        string? identifier;
        string? tag;
        if (settings.ImageIdentifier != null)
        {
            var identifierAndTag = DockerHelper.GetImageNameAndTag(settings.ImageIdentifier);
            identifier = identifierAndTag.imageName;
            tag = identifierAndTag.tag;
        }
        else
        {
            var identifierAndTag = await _promptHelper.GetIdentifierFromUserAsync("run");
            identifier = identifierAndTag.imageName;
            tag = identifierAndTag.tag;
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Terminating containers of other images", _ => TerminateOtherContainers(identifier, tag));
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Launching {identifier}", _ => LaunchImageAsync(identifier, tag));
        return 0;
    }

    private async Task TerminateOtherContainers(string identifier, string? tag)
    {
        var imageNames = new List<(string imageName, string tag)>();
        await foreach (var imageName in GetImageNamesExceptAsync(identifier, tag))
        {
            imageNames.Add(imageName);
        }
        await DockerClientFacade.TerminateContainers(imageNames);
    }

    private async IAsyncEnumerable<(string Name, string Tag)> GetImageNamesExceptAsync(string identifier, string? tag)
    {
        await foreach (var imageGroup in _allImagesQuery.QueryAsync())
        {
            foreach (var image in imageGroup.Images.Where(image => image.Identifier != identifier || image.Tag != tag))
            {
                yield return (image.Name, image.Tag);
            }
        }
    }

    private static async Task LaunchImageAsync(string identifier, string? tag)
    {
        var imageConfig = Services.Config.Value.GetImageByIdentifier(identifier);
        if (imageConfig?.ImageName == null)
        {
            throw new InvalidOperationException();
        }

        tag ??= imageConfig.ImageTag;

        var imageName = imageConfig.ImageName;
        var portFrom = imageConfig.PortFrom;
        var portTo = imageConfig.PortTo;
        var containerListResponse = await DockerClientFacade.GetContainerAsync(imageName, tag);
        if (containerListResponse == null)
        {
            var imagesListResponse = await DockerClientFacade.GetImageAsync(imageName, tag);
            if (imagesListResponse == null)
            {
                await DockerClientFacade.CreateImageAsync(imageName, tag);
            }

            await DockerClientFacade.CreateContainerAsync(identifier, imageName, tag, portFrom, portTo);
        }

        await DockerClientFacade.RunContainerAsync(identifier, tag);
    }
}