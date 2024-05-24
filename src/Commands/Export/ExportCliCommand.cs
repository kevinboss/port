using System.Text.RegularExpressions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Export;

internal class ExportCliCommand : AsyncCommand<ExportSettings>
{
    private readonly IImageIdentifierAndTagEvaluator _imageIdentifierAndTagEvaluator;
    private readonly IImageIdentifierPrompt _imageIdentifierPrompt;
    private readonly port.Config.Config _config;
    private readonly IExportImageCommand _exportImageCommand;
    private readonly IGetImageIdQuery _getImageIdQuery;

    public ExportCliCommand(IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
        IImageIdentifierPrompt imageIdentifierPrompt, port.Config.Config config, IExportImageCommand exportImageCommand,
        IGetImageIdQuery getImageIdQuery)
    {
        _imageIdentifierAndTagEvaluator = imageIdentifierAndTagEvaluator;
        _imageIdentifierPrompt = imageIdentifierPrompt;
        _config = config;
        _exportImageCommand = exportImageCommand;
        _getImageIdQuery = getImageIdQuery;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ExportSettings settings)
    {
        var (identifier, tag) = await GetIdentifierAndTagAsync(settings);
        if (tag == null)
            throw new InvalidOperationException("Can not export untagged image");
        await ExportAsync(identifier, tag, settings.Path);
        return 0;
    }

    private async Task<(string identifier, string? tag)> GetIdentifierAndTagAsync(IImageIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await _imageIdentifierPrompt.GetDownloadedIdentifierAndTagFromUserAsync("export");
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }

    private async Task ExportAsync(string identifier, string tag, string path)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        var imageName = imageConfig.ImageName;
        await Spinner.StartAsync($"Export {imageName} to {path}", async _ =>
            {
                var imageId = (await _getImageIdQuery.QueryAsync(imageName, tag)).SingleOrDefault();
                if (imageId == null)
                    throw new InvalidOperationException(
                        $"No images for '{ImageNameHelper.BuildImageName(imageName, tag)}' do exist".EscapeMarkup());
                var fileInfo = new FileInfo(path);
                var directoryInfo = fileInfo.Directory;
                if (directoryInfo == null) 
                    throw new InvalidOperationException(
                        $"Can not export image for '{ImageNameHelper.BuildImageName(imageName, tag)}' to {fileInfo.FullName}".EscapeMarkup());
                if (!directoryInfo.Exists) directoryInfo.Create();
                await _exportImageCommand.ExecuteAsync(imageId, path);
            });
    }
}