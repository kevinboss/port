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

    public override async Task<int> ExecuteAsync(CommandContext context, ListSettings settings)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Loading images", _ => LoadImages(settings.ImageIdentifier));
        return 0;
    }

    private async Task LoadImages(string? imageIdentifier)
    {
        var imageGroups = _allImagesQuery.QueryAsync();
        await foreach (var imageGroup in imageGroups.Where(e =>
                           imageIdentifier == null || e.Identifier == imageIdentifier))
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

            AnsiConsole.Write(root);
        }
    }
}