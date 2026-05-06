using port.Commands.List;
using port.Orchestrators;
using Spectre.Console.Cli;

namespace port.Commands.Run;

public class RunCliCommand(
    IImageIdentifierPrompt imageIdentifierPrompt,
    IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
    IRunOrchestrator runOrchestrator,
    ListCliCommand listCliCommand
) : AsyncCommand<RunSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        var (identifier, tag) = await ResolveIdentifierAndTagAsync(settings);
        if (tag == null)
            throw new InvalidOperationException("Can not launch untagged image");

        await runOrchestrator.WithProgressAsync(o =>
            o.ExecuteAsync(identifier, tag, settings.Reset)
        );
        await listCliCommand.ExecuteAsync();
        return 0;
    }

    private async Task<(string identifier, string? tag)> ResolveIdentifierAndTagAsync(
        IImageIdentifierSettings settings
    )
    {
        if (settings.ImageIdentifier != null)
            return imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);

        var identifierAndTag = await imageIdentifierPrompt.GetRunnableIdentifierAndTagFromUserAsync(
            "run"
        );
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }
}
