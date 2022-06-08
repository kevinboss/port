using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.List;

internal class ListCommand : AsyncCommand<ListSettings>
{
    private readonly IAllImagesQuery _allImagesQuery;

    public ListCommand(IAllImagesQuery allImagesQuery)
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
        var root = new Tree("Images");
        await foreach (var imageGroup in imageGroups.Where(e =>
                           imageIdentifier == null || e.Identifier == imageIdentifier))
        {
            if (imageGroup.Identifier == null) continue;


            var nodeHeader = $"[yellow]{imageGroup.Identifier} Tags[/]";
            if (imageGroup.Images.Any(e => e.Tag == null))
                nodeHeader = $"{nodeHeader} [red]{"[has untagged images]".EscapeMarkup()}[/]";
            var imageNode = root.AddNode(nodeHeader);

            foreach (var image in imageGroup.Images
                         .Where(e => e.Tag != null)
                         .OrderBy(e => e.Tag))
            {
                imageNode.AddNode(TagTextBuilder.BuildTagText(image));
            }
        }

        AnsiConsole.Write(root);
    }
}