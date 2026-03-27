using port.Commands.List;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Prune;

internal class PruneCliCommand : AsyncCommand<PruneSettings>
{
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly IRemoveImagesCliDependentCommand _removeImagesCliDependentCommand;
    private readonly ListCliCommand _listCliCommand;

    public PruneCliCommand(
        IAllImagesQuery allImagesQuery,
        IRemoveImagesCliDependentCommand removeImagesCliDependentCommand,
        ListCliCommand listCliCommand
    )
    {
        _allImagesQuery = allImagesQuery;
        _removeImagesCliDependentCommand = removeImagesCliDependentCommand;
        _listCliCommand = listCliCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, PruneSettings settings)
    {
        var imageGroups = await _allImagesQuery
            .QueryAsync()
            .Where(g =>
                settings.ImageIdentifier == null || g.Identifier == settings.ImageIdentifier
            )
            .ToListAsync();

        var pruneableImages = imageGroups
            .SelectMany(g => g.Images)
            .Where(i => i.Existing && i.Tag != null && ImageNameHelper.IsDigest(i.Tag))
            .ToList();

        if (pruneableImages.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]Nothing to prune[/]");
            return 0;
        }

        var result = await Spinner.StartAsync(
            "Pruning dangling images",
            ctx =>
                _removeImagesCliDependentCommand.ExecuteAsync(
                    pruneableImages.Select(i => i.Id!).ToList(),
                    ctx
                )
        );

        foreach (var failure in result.Where(r => !r.Successful))
        {
            AnsiConsole.MarkupLine(
                $"[orange3]Unable to remove image '{failure.ImageId}'[/] because it has dependent children"
            );
        }

        var removed = result.Count(r => r.Successful);
        AnsiConsole.MarkupLine($"[green]Pruned {removed} image(s)[/]");

        await _listCliCommand.ExecuteAsync();

        return 0;
    }
}
