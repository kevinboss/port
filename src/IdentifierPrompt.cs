using Spectre.Console;

namespace port;

internal class IdentifierPrompt : IIdentifierPrompt
{
    private readonly IAllImagesQuery _allImagesQuery;

    public IdentifierPrompt(IAllImagesQuery allImagesQuery)
    {
        _allImagesQuery = allImagesQuery;
    }

    public async Task<(string identifier, string? tag)> GetBaseIdentifierFromUserAsync(string command)
    {
        var selectionPrompt = CreateSelectionPrompt(command);
        await foreach (var imageGroup in _allImagesQuery.QueryAsync())
        {
            if (imageGroup.Identifier == null)
            {
                continue;
            }

            var nodeHeader = BuildNodeHeader(imageGroup);
            selectionPrompt.AddChoiceGroup(nodeHeader,
                imageGroup.Images
                    .Where(e => !e.IsSnapshot)
                    .Where(e => e.Tag != null)
                    .OrderBy(e => e.Tag));
        }

        var selectedImage = (Image)AnsiConsole.Prompt(selectionPrompt);
        return (selectedImage.Identifier, selectedImage.Tag);
    }

    public async Task<(string identifier, string? tag)> GetDownloadedIdentifierFromUserAsync(string command)
    {
        var selectionPrompt = CreateSelectionPrompt(command);
        await foreach (var imageGroup in _allImagesQuery.QueryAsync())
        {
            if (imageGroup.Identifier == null)
            {
                continue;
            }

            var nodeHeader = BuildNodeHeader(imageGroup);
            selectionPrompt.AddChoiceGroup(nodeHeader,
                imageGroup.Images
                    .Where(e => e.Existing)
                    .OrderBy(e => e.Tag));
        }

        var selectedImage = (Image)AnsiConsole.Prompt(selectionPrompt);
        return (selectedImage.Identifier, selectedImage.Tag);
    }

    public async Task<(string identifier, string? tag)> GetRunnableIdentifierFromUserAsync(string command)
    {
        var selectionPrompt = CreateSelectionPrompt(command);
        await foreach (var imageGroup in _allImagesQuery.QueryAsync())
        {
            if (imageGroup.Identifier == null)
            {
                continue;
            }

            var nodeHeader = BuildNodeHeader(imageGroup);
            selectionPrompt.AddChoiceGroup(nodeHeader,
                imageGroup.Images
                    .Where(e => e.Tag != null)
                    .OrderBy(e => e.Tag));
        }

        var selectedImage = (Image)AnsiConsole.Prompt(selectionPrompt);
        return (selectedImage.Identifier, selectedImage.Tag);
    }

    private static string BuildNodeHeader(ImageGroup imageGroup)
    {
        var nodeHeader = $"[yellow]{imageGroup.Identifier} Tags[/]";
        if (imageGroup.Images.Any(e => e.Tag == null))
            nodeHeader = $"{nodeHeader} [red]{"[has untagged images]".EscapeMarkup()}[/]";
        return nodeHeader;
    }

    private static SelectionPrompt<object> CreateSelectionPrompt(string command)
    {
        return new SelectionPrompt<object>()
            .UseConverter(o =>
            {
                if (o is not Image image)
                {
                    return o as string ?? throw new InvalidOperationException();
                }

                return TagTextBuilder.BuildTagText(image);
            })
            .PageSize(10)
            .Title($"Select image you wish to [green]{command}[/]")
            .MoreChoicesText("[grey](Move up and down to reveal more images)[/]");
    }
}