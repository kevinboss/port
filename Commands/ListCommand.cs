using Spectre.Console;
using Spectre.Console.Cli;

namespace dcma.Commands;

public class ListCommand : AsyncCommand<ListSettings>
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
        await foreach (var imageGroup in imageGroups)
        {
            if (imageGroup.Identifier == null) continue;

            var imageNode = root.AddNode($"[green]{imageGroup.Identifier} Tags[/]");

            foreach (var image in imageGroup.Images)
            {
                var imageTypeText = image.IsSnapshot ? "Snapshot" : "Base";
                imageNode.AddNode($"[yellow]{image.Tag} ({imageTypeText})[/]");
            }
        }

        AnsiConsole.Write(root);
    }
}