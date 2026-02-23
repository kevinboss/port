using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Prune;

internal class PruneCliCommand : Command<PruneSettings>
{
    public override int Execute(CommandContext context, PruneSettings settings)
    {
        AnsiConsole.MarkupLine(
            "[yellow]The prune command has been removed.[/] Untagged images are now visible via [blue]list[/] and can be removed via [blue]rm[/].");
        return 0;
    }
}
