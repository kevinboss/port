using Spectre.Console.Cli;

namespace port.Commands.Pull;

public class PullCliCommand : AsyncCommand<PullSettings>
{
    private readonly IIdentifierPrompt _identifierPrompt;
    private readonly Config.Config _config;
    private readonly IImageIdentifierAndTagEvaluator _imageIdentifierAndTagEvaluator;
    private readonly ICreateImageCliCommand _createImageCliCommand;

    public PullCliCommand(IIdentifierPrompt identifierPrompt, Config.Config config,
        IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
        ICreateImageCliCommand createImageCliCommand)
    {
        _identifierPrompt = identifierPrompt;
        _config = config;
        _imageIdentifierAndTagEvaluator = imageIdentifierAndTagEvaluator;
        _createImageCliCommand = createImageCliCommand;
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

        var identifierAndTag = await _identifierPrompt.GetBaseIdentifierFromUserAsync("pull");
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
        await _createImageCliCommand.ExecuteAsync(imageName, tag);
    }
}