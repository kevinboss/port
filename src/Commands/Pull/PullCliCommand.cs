using Spectre.Console.Cli;

namespace port.Commands.Pull;

internal class PullCliCommand(
    IImageIdentifierPrompt imageIdentifierPrompt,
    port.Config.Config config,
    IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
    ICreateImageCliChildCommand createImageCliChildCommand,
    ConditionalListCliCommand conditionalListCliCommand
) : AsyncCommand<PullSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, PullSettings settings)
    {
        var (identifier, tag) = await GetBaseIdentifierAndTagAsync(settings);
        await PullImageAsync(identifier, tag);
        await conditionalListCliCommand.ExecuteAsync();
        return 0;
    }

    private async Task<(string identifier, string? tag)> GetBaseIdentifierAndTagAsync(
        IImageIdentifierSettings settings
    )
    {
        if (settings.ImageIdentifier != null)
        {
            return imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await imageIdentifierPrompt.GetBaseIdentifierAndTagFromUserAsync(
            "pull"
        );
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }

    private async Task PullImageAsync(string identifier, string? tag)
    {
        var imageConfig = config.GetImageConfigByIdentifier(identifier);
        if (imageConfig == null)
        {
            throw new ArgumentException(
                $"There is no config defined for identifier '{identifier}'",
                nameof(identifier)
            );
        }

        var imageName = imageConfig.ImageName;
        await createImageCliChildCommand.ExecuteAsync(imageName, tag);
    }
}
