using port.Commands.List;
using port.Orchestrators;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Prune;

public class PruneCliCommand : AsyncCommand<PruneSettings>
{
    private readonly IPruneOrchestrator _pruneOrchestrator;
    private readonly ListCliCommand _listCliCommand;

    public PruneCliCommand(
        IPruneOrchestrator pruneOrchestrator,
        ListCliCommand listCliCommand
    )
    {
        _pruneOrchestrator = pruneOrchestrator;
        _listCliCommand = listCliCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, PruneSettings settings)
    {
        var result = await _pruneOrchestrator.WithRenderingAsync(o =>
            o.ExecuteAsync(settings.ImageIdentifier)
        );

        if (result.Removals.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]Nothing to prune[/]");
            return 0;
        }

        foreach (var failure in result.Removals.Where(r => !r.Successful))
        {
            AnsiConsole.MarkupLine(
                $"[orange3]Unable to remove image '{failure.ImageId}'[/] because it has dependent children"
            );
        }

        var removed = result.Removals.Count(r => r.Successful);
        AnsiConsole.MarkupLine($"[green]Pruned {removed} image(s)[/]");

        await _listCliCommand.ExecuteAsync();
        return 0;
    }
}
