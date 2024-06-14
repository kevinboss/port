using Spectre.Console;

namespace port;

internal class ImageIdentifierPrompt : IImageIdentifierPrompt
{
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly port.Config.Config _config;

    public ImageIdentifierPrompt(IAllImagesQuery allImagesQuery, port.Config.Config config)
    {
        _allImagesQuery = allImagesQuery;
        _config = config;
    }

    public string GetBaseIdentifierFromUser(string command)
    {
        var selectionPrompt = CreateSelectionPrompt(command);
        foreach (var imageConfig in _config.ImageConfigs)
        {
            selectionPrompt.AddChoice(imageConfig);
        }

        var selectedImageConfig = (port.Config.Config.ImageConfig)AnsiConsole.Prompt(selectionPrompt);
        return selectedImageConfig.Identifier;
    }

    public async Task<(string identifier, string? tag)> GetBaseIdentifierAndTagFromUserAsync(string command)
    {
        var selectionPrompt = CreateSelectionPrompt(command);
        var allImages = await Spinner.StartAsync($"Loading images to [green]{command}[/]",
            async _ => await _allImagesQuery.QueryAsync().ToListAsync());
        foreach (var imageGroup in allImages.OrderBy(i => i.Identifier))
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
        var selectionPrompt = CreateSelectionPrompt(command);
        var allImages = await Spinner.StartAsync($"Loading images to [green]{command}[/]",
            async _ => await _allImagesQuery.QueryAsync().ToListAsync());
        foreach (var imageGroup in allImages.OrderBy(i => i.Identifier))
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
        var selectionPrompt = CreateSelectionPrompt(command);
        var allImages = await Spinner.StartAsync($"Loading images to [green]{command}[/]",
            async _ => await _allImagesQuery.QueryAsync().ToListAsync());
        foreach (var imageGroup in allImages.OrderBy(i => i.Identifier))
        {
            selectionPrompt.AddChoices(imageGroup.Images
                .Where(e => e.Tag != null)
                .OrderBy(e => e.Tag));
        }

        var selectedImage = (Image)AnsiConsole.Prompt(selectionPrompt);
        return (selectedImage.Group.Identifier, selectedImage.Tag);
    }

    private static SelectionPrompt<object> CreateSelectionPrompt(string command)
    {
        return new SelectionPrompt<object>()
            .UseConverter(o =>
            {
                return o switch
                {
                    Image image => TagTextBuilder.BuildTagText(image),
                    port.Config.Config.ImageConfig imageConfig => $"[white]{imageConfig.Identifier}[/]",
                    _ => o as string ?? throw new InvalidOperationException()
                };
            })
            .PageSize(10)
            .Title($"Select image you wish to [green]{command}[/]")
            .MoreChoicesText("[grey](Move up and down to reveal more images)[/]");
    }
}