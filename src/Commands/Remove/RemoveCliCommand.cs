using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Remove;

internal class RemoveCliCommand : AsyncCommand<RemoveSettings>
{
    private readonly IIdentifierPrompt _identifierPrompt;
    private readonly IGetContainersQuery _getContainersQuery;
    private readonly IGetImageIdQuery _getImageIdQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly IRemoveImageCommand _removeImageCommand;
    private readonly Config.Config _config;
    private readonly IImageIdentifierAndTagEvaluator _imageIdentifierAndTagEvaluator;

    public RemoveCliCommand(IIdentifierPrompt identifierPrompt, IGetContainersQuery getContainersQuery,
        Config.Config config,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand, IRemoveImageCommand removeImageCommand,
        IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator, IGetImageIdQuery getImageIdQuery)
    {
        _identifierPrompt = identifierPrompt;
        _getContainersQuery = getContainersQuery;
        _config = config;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _removeImageCommand = removeImageCommand;
        _imageIdentifierAndTagEvaluator = imageIdentifierAndTagEvaluator;
        _getImageIdQuery = getImageIdQuery;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RemoveSettings settings)
    {
        var (identifier, tag) = await GetIdentifierAndTagAsync(settings);
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Removing {ImageNameHelper.BuildImageName(identifier, tag)}",
                ctx => RemoveImageAsync(identifier, tag, ctx));
        return 0;
    }

    private async Task<(string identifier, string? tag)> GetIdentifierAndTagAsync(IImageIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await _identifierPrompt.GetDownloadedIdentifierFromUserAsync("remove");
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }

    private async Task RemoveImageAsync(string identifier, string? tag, StatusContext ctx)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        var imageName = imageConfig.ImageName;
        var containers = await _getContainersQuery.QueryByImageNameAndTagAsync(imageName, tag);
        ctx.Status = $"Removing containers for image '{ImageNameHelper.BuildImageName(identifier, tag)}'";
        foreach (var container in containers)
        {
            await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
        }
        AnsiConsole.WriteLine($"Containers for image '{ImageNameHelper.BuildImageName(identifier, tag)}' removed");

        var imageId = await _getImageIdQuery.QueryAsync(imageName, tag);
        if (imageId == null)
            throw new InvalidOperationException(
                $"Image '{ImageNameHelper.BuildImageName(imageName, tag)}' does not exist or does not have an Id".EscapeMarkup());
        await _removeImageCommand.ExecuteAsync(imageId);
        AnsiConsole.WriteLine($"Removed image {ImageNameHelper.BuildImageName(imageName, tag)}");
    }
}