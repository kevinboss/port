using System.Net;
using Docker.DotNet;
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
        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Removing untagged images",
                ctx => RemoveUntaggedImagesAsync(identifier, ctx));
        foreach (var imageRemovalResult in result)
        {
            if (imageRemovalResult.Successful)
                AnsiConsole.WriteLine($"Removed image '{imageRemovalResult.ImageId}'");
            else
                AnsiConsole.MarkupLine(
                    $"[orange3]Unable to removed image '{imageRemovalResult.ImageId}'[/] because it has dependent child images");
        }
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

    private async Task<List<ImageRemovalResult>> RemoveUntaggedImagesAsync(string identifier, StatusContext ctx)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        var imageName = imageConfig.ImageName;
        var imageIds = (await _getImageIdQuery.QueryAsync(imageName, null)).ToList();
        if (!imageIds.Any())
            throw new InvalidOperationException(
                $"No images for '{identifier}:<none>' do exist".EscapeMarkup());

        var result = new List<ImageRemovalResult>();
        foreach (var imageId in imageIds)
        {
            var containers = await _getContainersQuery.QueryByImageIdAsync(imageId);
            ctx.Status = $"Removing containers using '{imageId}'".EscapeMarkup();
            foreach (var container in containers)
            {
                await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
            }

            ctx.Status = $"Containers using '{imageId}' removed".EscapeMarkup();

            try
            {
                await _removeImageCommand.ExecuteAsync(imageId);
            }
            catch (DockerApiException e) when (e.StatusCode == HttpStatusCode.Conflict)
            {
                result.Add(new ImageRemovalResult(imageId, false));
            }
        }

        return result;
    }
}