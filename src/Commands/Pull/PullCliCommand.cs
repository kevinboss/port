using Spectre.Console.Cli;

namespace port.Commands.Pull;

public class PullCliCommand(
    IImageIdentifierPrompt imageIdentifierPrompt,
    port.Config.Config config,
    IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
    ICreateImageCliChildCommand createImageCliChildCommand) : AsyncCommand<PullSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, PullSettings settings)
    {
        var (identifier, tag) = await GetBaseIdentifierAndTagAsync(settings);
        await PullImageAsync(identifier, tag);
        return 0;
    }

    private async Task<(string identifier, string? tag)> GetBaseIdentifierAndTagAsync(IImageIdentifierSettings settings) =>
        settings.ImageIdentifier is not null
            ? imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier)
            : await imageIdentifierPrompt.GetBaseIdentifierAndTagFromUserAsync("pull");

    private async Task PullImageAsync(string identifier, string? tag)
    {
        var imageConfig = config.GetImageConfigByIdentifier(identifier) 
            ?? throw new ArgumentException($"There is no config defined for identifier '{identifier}'", nameof(identifier));

        await createImageCliChildCommand.ExecuteAsync(imageConfig.ImageName, tag);
    }
}
