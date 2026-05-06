using port.Orchestrators;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.List;

public class ListCliCommand : AsyncCommand<ListSettings>
{
    private readonly IListOrchestrator _listOrchestrator;

    public ListCliCommand(IListOrchestrator listOrchestrator)
    {
        _listOrchestrator = listOrchestrator;
    }

    public override async Task<int> ExecuteAsync(CommandContext _, ListSettings settings)
    {
        var result = await _listOrchestrator.WithRenderingAsync(o =>
            o.ExecuteAsync(settings.ImageIdentifier)
        );
        Render(result);
        return 0;
    }

    public async Task ExecuteAsync()
    {
        var result = await _listOrchestrator.WithRenderingAsync(o => o.ExecuteAsync(null));
        Render(result);
    }

    private static void Render(ListResult result)
    {
        var entries = result.ImageGroups.SelectMany(g => g.Images.Select(i => (g.Identifier, i)));
        var lengths = TagTextBuilder.GetLengths(entries);
        AnsiConsole.WriteLine();
        foreach (var group in result.ImageGroups)
        {
            foreach (
                var line in group
                    .Images.Where(e => e.Tag != null)
                    .OrderBy(e => e.Tag)
                    .Select(image => TagTextBuilder.BuildTagText(group.Identifier, image, lengths))
            )
            {
                AnsiConsole.MarkupLine(line);
            }
        }
    }
}
