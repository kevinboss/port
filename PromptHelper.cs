using Spectre.Console;

namespace dcma;

public static class PromptHelper
{
    public static string GetImageAliasFromUser()
    {
        var selectionPrompt = new SelectionPrompt<string>()
            .PageSize(10)
            .Title("Select image you wish to [green]run[/]")
            .MoreChoicesText("[grey](Move up and down to reveal more images)[/]");
        foreach (var image in Services.Config.Value.Images)
        {
            if (image.Identifier != null) selectionPrompt.AddChoice(image.Identifier);
        }

        return AnsiConsole.Prompt(selectionPrompt);
    }
}