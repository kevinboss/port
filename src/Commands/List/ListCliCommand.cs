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
        var imageTrees = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Loading images", async _ => await CreateImageTree(settings.ImageIdentifier).ToListAsync());
        foreach (var imageTree in imageTrees)
        {
            AnsiConsole.Write(imageTree);
        }

        return 0;
    }

    public async Task ExecuteAsync()
    {
        var imageTrees = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Loading images", async _ => await CreateImageTree().ToListAsync());
        foreach (var imageTree in imageTrees)
        {
            AnsiConsole.Write(imageTree);
        }
    }

    private async IAsyncEnumerable<Tree> CreateImageTree(string? imageIdentifier = default)
    {
        var imageGroups = _allImagesQuery.QueryAsync();
        await foreach (var imageGroup in imageGroups.Where(e =>
                               imageIdentifier == null || e.Identifier == imageIdentifier)
                           .OrderBy(i => i.Identifier))
        {
            var treeHeader = $"[yellow]{imageGroup.Identifier} Tags[/]";
            if (imageGroup.Images.Any(e => e.Tag == null))
                treeHeader = $"{treeHeader} [red]{"[has untagged images]".EscapeMarkup()}[/]";
            var root = new Tree(treeHeader);
            foreach (var image in imageGroup.Images
                         .Where(e => e.Tag != null)
                         .OrderBy(e => e.Tag))
            {
                root.AddNode(TagTextBuilder.BuildTagText(image));
            }

            yield return root;
        }
    }
}