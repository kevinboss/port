using Spectre.Console;

namespace port;

internal class ImageIdentifierPrompt : IImageIdentifierPrompt
{
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly Config.Config _config;

    public ImageIdentifierPrompt(IAllImagesQuery allImagesQuery, Config.Config config)
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

        var selectedImageConfig = (Config.Config.ImageConfig)AnsiConsole.Prompt(selectionPrompt);
        return selectedImageConfig.Identifier;
    }

    public async Task<(string identifier, string? tag)> GetBaseIdentifierAndTagFromUserAsync(string command)
    {
        var selectionPrompt = CreateSelectionPrompt(command);
        await foreach (var imageGroup in _allImagesQuery
                           .QueryAsync()
                           .OrderBy(i => i.Identifier))
        {
            var nodeHeader = BuildNodeHeader(imageGroup);
            selectionPrompt.AddChoiceGroup(nodeHeader,
                imageGroup.Images
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
        await foreach (var imageGroup in _allImagesQuery
                           .QueryAsync()
                           .OrderBy(i => i.Identifier))
        {
            var nodeHeader = BuildNodeHeader(imageGroup);
            selectionPrompt.AddChoiceGroup(nodeHeader,
                imageGroup.Images
                    .Where(e => e.Existing)
                    .OrderBy(e => e.Tag));
        }

        var selectedImage = (Image)AnsiConsole.Prompt(selectionPrompt);
        return (selectedImage.Group.Identifier, selectedImage.Tag);
    }

    public async Task<(string identifier, string? tag)> GetRunnableIdentifierAndTagFromUserAsync(string command)
    {
        var selectionPrompt = CreateSelectionPrompt(command);
        await foreach (var imageGroup in _allImagesQuery
                           .QueryAsync()
                           .OrderBy(i => i.Identifier))
        {
            var nodeHeader = BuildNodeHeader(imageGroup);
            selectionPrompt.AddChoiceGroup(nodeHeader,
                imageGroup.Images
                    .Where(e => e.Tag != null)
                    .OrderBy(e => e.Tag));
        }

        var selectedImage = (Image)AnsiConsole.Prompt(selectionPrompt);
        return (selectedImage.Group.Identifier, selectedImage.Tag);
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
                return o switch
                {
                    Image image => TagTextBuilder.BuildTagText(image),
                    ImageGroup imageGroup => $"[white]{imageGroup.Identifier}[/]",
                    Config.Config.ImageConfig imageConfig => $"[white]{imageConfig.Identifier}[/]",
                    _ => o as string ?? throw new InvalidOperationException()
                };
            })
            .PageSize(10)
            .Title($"Select image you wish to [green]{command}[/]")
            .MoreChoicesText("[grey](Move up and down to reveal more images)[/]");
    }
}