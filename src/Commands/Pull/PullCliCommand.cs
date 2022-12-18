using Spectre.Console.Cli;

namespace port.Commands.Pull;

public class PullCliCommand : AsyncCommand<PullSettings>
{
    private readonly IImageIdentifierPrompt _imageIdentifierPrompt;
    private readonly Config.Config _config;
    private readonly IImageIdentifierAndTagEvaluator _imageIdentifierAndTagEvaluator;
    private readonly ICreateImageCliChildCommand _createImageCliChildCommand;

    public PullCliCommand(IImageIdentifierPrompt imageIdentifierPrompt, Config.Config config,
        IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
        ICreateImageCliChildCommand createImageCliChildCommand)
    {
        _imageIdentifierPrompt = imageIdentifierPrompt;
        _config = config;
        _imageIdentifierAndTagEvaluator = imageIdentifierAndTagEvaluator;
        _createImageCliChildCommand = createImageCliChildCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, PullSettings settings)
    {
        var (identifier, tag) = await GetBaseIdentifierAndTagAsync(settings);
        await PullImageAsync(identifier, tag);
        return 0;
    }

    private async Task<(string identifier, string? tag)> GetBaseIdentifierAndTagAsync(IImageIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await _imageIdentifierPrompt.GetBaseIdentifierAndTagFromUserAsync("pull");
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }

    private async Task PullImageAsync(string identifier, string? tag)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        if (imageConfig == null)
        {
            throw new ArgumentException($"There is no config defined for identifier '{identifier}'",
                nameof(identifier));
        }

        var imageName = imageConfig.ImageName;
        await _createImageCliChildCommand.ExecuteAsync(imageName, tag);
    }
}