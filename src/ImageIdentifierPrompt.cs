using Spectre.Console;

namespace port;

internal class ImageIdentifierPrompt : IImageIdentifierPrompt
{
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly port.Config.Config _config;
    private const string GreyMoveUpAndDownToRevealMoreImages = "[grey](Move up and down to reveal more images)[/]";

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

    public async Task<(string identifier, string? tag)> GetBaseIdentifierAndTagFromUserAsync(string command)
    {
        var groups = await Spinner.StartAsync($"Loading images to [green]{command}[/]",
            async _ => await _allImagesQuery.QueryAsync().ToListAsync());
        var lengths = TagTextBuilder.GetLengths(groups.SelectMany(group => group.Images));
        var selectionPrompt = CreateSelectionPrompt(command, lengths);
        foreach (var imageGroup in groups.OrderBy(i => i.Identifier))
        {
            selectionPrompt.AddChoices(imageGroup.Images
                .Where(e => !e.IsSnapshot)
                .Where(e => e.Tag != null)
                .OrderBy(e => e.Tag));
        }

        var selectedImage = (Image)AnsiConsole.Prompt(selectionPrompt);
        return (selectedImage.Group.Identifier, selectedImage.Tag);
    }

    public async Task<(string identifier, string? tag)> GetDownloadedIdentifierAndTagFromUserAsync(string command)
    {
        var groups = await Spinner.StartAsync($"Loading images to [green]{command}[/]",
            async _ => await _allImagesQuery.QueryAsync().ToListAsync());
        var lengths = TagTextBuilder.GetLengths(groups.SelectMany(group => group.Images));
        var selectionPrompt = CreateSelectionPrompt(command, lengths);
        foreach (var imageGroup in groups.OrderBy(i => i.Identifier))
        {
            selectionPrompt.AddChoices(imageGroup.Images
                .Where(e => e.Existing)
                .OrderBy(e => e.Tag));
        }

        var selectedImage = (Image)AnsiConsole.Prompt(selectionPrompt);
        return (selectedImage.Group.Identifier, selectedImage.Tag);
    }

    public async Task<(string identifier, string? tag)> GetRunnableIdentifierAndTagFromUserAsync(string command)
    {
        var groups = await Spinner.StartAsync($"Loading images to [green]{command}[/]",
            async _ => await _allImagesQuery.QueryAsync().ToListAsync());
        var lengths = TagTextBuilder.GetLengths(groups.SelectMany(group => group.Images));
        var selectionPrompt = CreateSelectionPrompt(command, lengths);
        foreach (var imageGroup in groups.OrderBy(i => i.Identifier))
        {
            selectionPrompt.AddChoices(imageGroup.Images
                .Where(e => e.Tag != null)
                .OrderBy(e => e.Tag));
        }

        var selectedImage = (Image)AnsiConsole.Prompt(selectionPrompt);
        return (selectedImage.Group.Identifier, selectedImage.Tag);
    }

    private static SelectionPrompt<Image> CreateSelectionPrompt(string command,
        (int first, int second) lengths)
    {
        return new SelectionPrompt<Image>()
            .UseConverter(image => TagTextBuilder.BuildTagText(image, lengths))
            .PageSize(10)
            .Title($"Select image you wish to [green]{command}[/]")
            .MoreChoicesText(GreyMoveUpAndDownToRevealMoreImages);
    }
}