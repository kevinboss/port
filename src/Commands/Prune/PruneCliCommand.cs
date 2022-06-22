using port.Commands.Remove;
using port.Commands.Run;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Prune;

internal class PruneCliCommand : AsyncCommand<PruneSettings>
{
    private readonly IIdentifierAndTagEvaluator _identifierAndTagEvaluator;
    private readonly IIdentifierPrompt _identifierPrompt;
    private readonly IGetImageIdQuery _getImageIdQuery;
    private readonly Config.Config _config;
    private readonly IRemoveImageCommand _removeImageCommand;
    private readonly IGetContainersQuery _getContainersQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;

    public PruneCliCommand(IIdentifierAndTagEvaluator identifierAndTagEvaluator,
        IIdentifierPrompt identifierPrompt, IGetImageIdQuery getImageIdQuery, Config.Config config,
        IRemoveImageCommand removeImageCommand, IGetContainersQuery getContainersQuery,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand)
    {
        _identifierAndTagEvaluator = identifierAndTagEvaluator;
        _identifierPrompt = identifierPrompt;
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

    private async Task<string> GetIdentifierAsync(IIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _identifierAndTagEvaluator.Evaluate(settings.ImageIdentifier).identifier;
        }

        return await _identifierPrompt.GetUntaggedIdentifierFromUserAsync("prune");
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