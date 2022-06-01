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

            selectionPrompt.AddChoiceGroup($"[yellow]{imageGroup.Identifier} Tags[/]",
                imageGroup.Images
                    .Where(e => !e.IsSnapshot)
                    .OrderBy(e => e.Tag));
        }

        var selectedImage = (Image)AnsiConsole.Prompt(selectionPrompt);
        return (selectedImage.Identifier, selectedImage.Tag);
    }

    public async Task<(string identifier, string? tag)> GetIdentifierFromUserAsync(string command, bool hideMissing)
    {
        var selectionPrompt = CreateSelectionPrompt(command);
        await foreach (var imageGroup in _allImagesQuery.QueryAsync())
        {
            if (imageGroup.Identifier == null)
            {
                continue;
            }

            selectionPrompt.AddChoiceGroup($"[yellow]{imageGroup.Identifier} Tags[/]",
                imageGroup.Images
                    .Where(e => !hideMissing || e.Existing || e.Tag != null)
                    .Where(e => e.Tag != null)
                    .OrderBy(e => e.Tag));
        }

        var selectedImage = (Image)AnsiConsole.Prompt(selectionPrompt);
        return (selectedImage.Identifier, selectedImage.Tag);
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