using Spectre.Console;

namespace dcma;

public class PromptHelper : IPromptHelper
{
    private readonly IAllImagesQuery _allImagesQuery;

    public PromptHelper(IAllImagesQuery allImagesQuery)
    {
        _allImagesQuery = allImagesQuery;
    }

    public async Task<(string identifier, string? tag)> GetIdentifierFromUserAsync(string command)
    {
        var selectionPrompt = new SelectionPrompt<string>()
            .PageSize(10)
            .Title($"Select image you wish to [green]{command}[/]")
            .MoreChoicesText("[grey](Move up and down to reveal more images)[/]");

        var options = new Dictionary<string, Image>();
        await foreach (var imageGroup in _allImagesQuery.QueryAsync())
        {
            if (imageGroup.Identifier == null)
            {
                continue;
            }
            
            
            var groupOptions = new List<string>();
            foreach (var image in imageGroup.Images.OrderBy(e => e.IsSnapshot))
            {
                var imageTypeText = image.IsSnapshot ? "Snapshot" : "Base";
                var choiceIdentifier = $"{image.Tag} ({imageTypeText})";
                groupOptions.Add(choiceIdentifier);
                options.Add(choiceIdentifier, image);
            }
            selectionPrompt.AddChoiceGroup(imageGroup.Identifier, groupOptions);
        }

        var selectedImage = options[AnsiConsole.Prompt(selectionPrompt)];
        return (selectedImage.Identifier, selectedImage.Tag);
    }
}