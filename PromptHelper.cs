using Spectre.Console;

namespace dcma;

public class PromptHelper : IPromptHelper
{
    private readonly IAllImagesQuery _allImagesQuery;

    public PromptHelper(IAllImagesQuery allImagesQuery)
    {
        _allImagesQuery = allImagesQuery;
    }

    public async Task<(string imageName, string? tag)> GetIdentifierFromUserAsync(string command)
    {
        var selectionPrompt = new SelectionPrompt<string>()
            .PageSize(10)
            .Title($"Select image you wish to [green]{command}[/]")
            .MoreChoicesText("[grey](Move up and down to reveal more images)[/]");
        await foreach (var imageGroup in _allImagesQuery.QueryAsync())
        {
            if (imageGroup.Identifier == null)
            {
                continue;
            }

            foreach (var image in imageGroup.Images)
            {
                if (image.Identifier == null)
                {
                    throw new InvalidOperationException();
                }

                if (image.Tag == null)
                {
                    throw new InvalidOperationException();
                }

                selectionPrompt.AddChoice(DockerHelper.JoinImageNameAndTag(image.Identifier, image.Tag));
            }
        }

        return DockerHelper.GetImageNameAndTag(AnsiConsole.Prompt(selectionPrompt));
    }
}