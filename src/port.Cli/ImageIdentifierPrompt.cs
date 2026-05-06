using Spectre.Console;

namespace port;

public class ImageIdentifierPrompt : IImageIdentifierPrompt
{
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly port.Config.Config _config;
    private const string GreyMoveUpAndDownToRevealMoreImages =
        "[grey](Move up and down to reveal more images)[/]";

    public ImageIdentifierPrompt(IAllImagesQuery allImagesQuery, port.Config.Config config)
    {
        _allImagesQuery = allImagesQuery;
        _config = config;
    }

    public string GetBaseIdentifierFromUser(string command)
    {
        var selectionPrompt = new SelectionPrompt<port.Config.Config.ImageConfig>()
            .UseConverter(imageConfig => $"[white]{imageConfig.Identifier}[/]")
            .PageSize(10)
            .Title($"Select image you wish to [green]{command}[/]")
            .MoreChoicesText(GreyMoveUpAndDownToRevealMoreImages);
        foreach (var imageConfig in _config.ImageConfigs)
        {
            selectionPrompt.AddChoice(imageConfig);
        }

        var selectedImageConfig = AnsiConsole.Prompt(selectionPrompt);
        return selectedImageConfig.Identifier;
    }

    public Task<(string identifier, string? tag)> GetBaseIdentifierAndTagFromUserAsync(
        string command
    ) => PromptAsync(command, image => !image.IsSnapshot && image.Tag != null);

    public Task<(string identifier, string? tag)> GetDownloadedIdentifierAndTagFromUserAsync(
        string command
    ) => PromptAsync(command, image => image.Existing);

    public Task<(string identifier, string? tag)> GetRunnableIdentifierAndTagFromUserAsync(
        string command
    ) => PromptAsync(command, image => image.Tag != null);

    private async Task<(string identifier, string? tag)> PromptAsync(
        string command,
        Func<Image, bool> filter
    )
    {
        var groups = await Spinner.StartAsync(
            $"Loading images to [green]{command}[/]",
            async _ => await _allImagesQuery.QueryAsync().ToListAsync()
        );
        var lengths = TagTextBuilder.GetLengths(
            groups.SelectMany(g => g.Images.Select(i => (g.Identifier, i)))
        );
        var selectionPrompt = CreateSelectionPrompt(command, lengths);
        foreach (var imageGroup in groups.OrderBy(i => i.Identifier))
        {
            selectionPrompt.AddChoices(
                imageGroup
                    .Images.Where(filter)
                    .OrderBy(e => e.Tag)
                    .Select(i => new ImageChoice(imageGroup.Identifier, i))
            );
        }

        var selected = AnsiConsole.Prompt(selectionPrompt);
        return (selected.Identifier, selected.Image.Tag);
    }

    private static SelectionPrompt<ImageChoice> CreateSelectionPrompt(
        string command,
        (int first, int second) lengths
    )
    {
        return new SelectionPrompt<ImageChoice>()
            .UseConverter(c => TagTextBuilder.BuildTagText(c.Identifier, c.Image, lengths))
            .PageSize(10)
            .Title($"Select image you wish to [green]{command}[/]")
            .MoreChoicesText(GreyMoveUpAndDownToRevealMoreImages);
    }

    private sealed record ImageChoice(string Identifier, Image Image);
}
