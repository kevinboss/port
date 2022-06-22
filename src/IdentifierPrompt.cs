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

    public async Task<(string identifier, string? tag)> GetDownloadedIdentifierFromUserAsync(string command)
    {
        var selectionPrompt = CreateSelectionPrompt(command);
        await foreach (var imageGroup in _allImagesQuery.QueryAsync())
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

    public async Task<(string identifier, string? tag)> GetRunnableIdentifierFromUserAsync(string command)
    {
        var selectionPrompt = CreateSelectionPrompt(command);
        await foreach (var imageGroup in _allImagesQuery.QueryAsync())
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

    public (string identifier, string? tag) GetIdentifierOfContainerFromUser(
        IEnumerable<Container> containers,
        string command)
    {
        var selectionPrompt = CreateSelectionPrompt(command);
        foreach (var container in containers)
        {
            selectionPrompt.AddChoice(container);
        }

        var selectedContainer = (Container)AnsiConsole.Prompt(selectionPrompt);
        return (selectedContainer.Identifier, selectedContainer.Tag);
    }

    public async Task<string> GetUntaggedIdentifierFromUserAsync(string command)
    {
        var selectionPrompt = CreateSelectionPrompt(command);
        await foreach (var imageGroup in _allImagesQuery.QueryAsync().Where(e => e.Images.Any(i => i.Tag == null)))
        {
            selectionPrompt.AddChoice(imageGroup);
        }

        var selectedImageGroup = (ImageGroup)AnsiConsole.Prompt(selectionPrompt);
        return selectedImageGroup.Identifier;
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
                    Container container =>
                        $"[white]{ContainerNameHelper.JoinContainerNameAndTag(container.Identifier, container.Tag)}[/]",
                    ImageGroup imageGroup => $"[white]{imageGroup.Identifier}[/]",
                    _ => o as string ?? throw new InvalidOperationException()
                };
            })
            .PageSize(10)
            .Title($"Select image you wish to [green]{command}[/]")
            .MoreChoicesText("[grey](Move up and down to reveal more images)[/]");
    }
}