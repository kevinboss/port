using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.List;

internal class ListCliCommand : AsyncCommand<ListSettings>
{
    private readonly IAllImagesQuery _allImagesQuery;

    public ListCliCommand(IAllImagesQuery allImagesQuery)
    {
        _allImagesQuery = allImagesQuery;
    }

    public override async Task<int> ExecuteAsync(CommandContext _, ListSettings settings)
    {
        var textsGroups = await Spinner.StartAsync(
            "Loading images",
            async _ => await CreateImageTree(settings.ImageIdentifier).ToListAsync()
        );
        AnsiConsole.WriteLine();
        foreach (var text in textsGroups.SelectMany(texts => texts))
        {
            AnsiConsole.MarkupLine(text);
        }

        return 0;
    }

    public async Task ExecuteAsync()
    {
        var textsGroups = await Spinner.StartAsync(
            "Loading images",
            async _ => await CreateImageTree().ToListAsync()
        );
        AnsiConsole.WriteLine();
        foreach (var text in textsGroups.SelectMany(texts => texts))
        {
            AnsiConsole.MarkupLine(text);
        }
    }

    private async IAsyncEnumerable<List<string>> CreateImageTree(string? imageIdentifier = default)
    {
        var imageGroups = (await _allImagesQuery.QueryAsync().ToListAsync())
            .Where(e => imageIdentifier == null || e.Identifier == imageIdentifier)
            .OrderBy(i => i.Identifier)
            .ToList();
        var lengths = TagTextBuilder.GetLengths(
            imageGroups.SelectMany(imageGroup => imageGroup.Images)
        );
        foreach (var imageGroup in imageGroups)
        {
            yield return imageGroup
                .Images.Where(e => e.Tag != null)
                .OrderBy(e => e.Tag)
                .Select(image => TagTextBuilder.BuildTagText(image, lengths))
                .ToList();
        }
    }
}
