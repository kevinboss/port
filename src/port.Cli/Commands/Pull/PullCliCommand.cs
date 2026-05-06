using port.Orchestrators;
using Spectre.Console.Cli;

namespace port.Commands.Pull;

public class PullCliCommand(
    IImageIdentifierPrompt imageIdentifierPrompt,
    IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
    IPullOrchestrator pullOrchestrator
) : AsyncCommand<PullSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, PullSettings settings)
    {
        var (identifier, tag) = await ResolveIdentifierAndTagAsync(settings);
        await pullOrchestrator.WithProgressAsync(o => o.ExecuteAsync(identifier, tag));
        return 0;
    }

    private async Task<(string identifier, string? tag)> ResolveIdentifierAndTagAsync(
        IImageIdentifierSettings settings
    )
    {
        if (settings.ImageIdentifier != null)
            return imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);

        var identifierAndTag = await imageIdentifierPrompt.GetBaseIdentifierAndTagFromUserAsync(
            "pull"
        );
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }
}
