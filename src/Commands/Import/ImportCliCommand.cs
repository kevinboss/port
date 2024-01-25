using System.Text.RegularExpressions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Import;

internal class ImportCliCommand : AsyncCommand<ImportSettings>
{
    private readonly IImageIdentifierPrompt _imageIdentifierPrompt;
    private readonly port.Config.Config _config;
    private readonly IImportImageCommand _importImageCommand;
    private readonly IGetImageIdQuery _getImageIdQuery;

    public ImportCliCommand(IImageIdentifierPrompt imageIdentifierPrompt, port.Config.Config config, IImportImageCommand importImageCommand,
        IGetImageIdQuery getImageIdQuery)
    {
        _imageIdentifierPrompt = imageIdentifierPrompt;
        _config = config;
        _importImageCommand = importImageCommand;
        _getImageIdQuery = getImageIdQuery;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ImportSettings settings)
    {
        var identifier = _imageIdentifierPrompt.GetBaseIdentifierFromUser("import");
        await ImportAsync(identifier, settings.Tag, settings.Path);
        return 0;
    }

    private async Task ImportAsync(string identifier, string tag, string path)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        var imageName = imageConfig.ImageName;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Import {imageName} from {path}", async _ =>
            {
                var fileInfo = new FileInfo(path);
                if (!fileInfo.Exists) 
                    throw new InvalidOperationException(
                        $"Image file {fileInfo.FullName} does not exist".EscapeMarkup());
                await _importImageCommand.ExecuteAsync(path, imageName, tag);
            });
    }
}