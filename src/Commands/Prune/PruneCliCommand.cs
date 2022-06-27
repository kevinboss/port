using port.Commands.Remove;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Prune;

internal class PruneCliCommand : AsyncCommand<PruneSettings>
{
    private readonly IImageIdentifierAndTagEvaluator _imageIdentifierAndTagEvaluator;
    private readonly IImageIdentifierPrompt _imageIdentifierPrompt;
    private readonly IGetImageIdQuery _getImageIdQuery;
    private readonly Config.Config _config;
    private readonly IRemoveImageCommand _removeImageCommand;
    private readonly IGetContainersQuery _getContainersQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;

    public PruneCliCommand(IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
        IImageIdentifierPrompt imageIdentifierPrompt, IGetImageIdQuery getImageIdQuery, Config.Config config,
        IRemoveImageCommand removeImageCommand, IGetContainersQuery getContainersQuery,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand)
    {
        _imageIdentifierAndTagEvaluator = imageIdentifierAndTagEvaluator;
        _imageIdentifierPrompt = imageIdentifierPrompt;
        _getImageIdQuery = getImageIdQuery;
        _config = config;
        _removeImageCommand = removeImageCommand;
        _getContainersQuery = getContainersQuery;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, PruneSettings settings)
    {
        var identifier = await GetIdentifierAsync(settings);
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Removing untagged images", ctx => RemoveUntaggedImagesAsync(identifier, ctx));
        AnsiConsole.WriteLine("Removed untagged images");
        return 0;
    }

    private async Task<string> GetIdentifierAsync(IImageIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier).identifier;
        }

        return await _imageIdentifierPrompt.GetUntaggedIdentifierFromUserAsync("prune");
    }

    private async Task RemoveUntaggedImagesAsync(string identifier, StatusContext ctx)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        var imageName = imageConfig.ImageName;
        var imageId = await _getImageIdQuery.QueryAsync(imageName, null);
        if (string.IsNullOrEmpty(imageId))
            throw new InvalidOperationException(
                $"Image {identifier}:<none> does not exist or does not have an Id".EscapeMarkup());

        var containers = await _getContainersQuery.QueryByImageIdAsync(imageId);
        ctx.Status = $"Removing containers for {identifier}:<none>".EscapeMarkup();
        foreach (var container in containers)
        {
            await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
        }

        ctx.Status = $"Containers for {identifier}:<none> removed".EscapeMarkup();
        await _removeImageCommand.ExecuteAsync(imageId);
        ctx.Status = $"Removed image {identifier}:<none>".EscapeMarkup();
    }
}