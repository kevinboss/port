using Spectre.Console;
using Spectre.Console.Cli;

namespace dcma.Commands;

public class ListCommand : AsyncCommand<ListSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ListSettings settings)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Loading images", _ => LoadImages(settings.ImageIdentifier));
        return 0;
    }

    private static async Task LoadImages(string? imageIdentifier)
    {
        var images = Services.Config.Value.Images;

        var imageNames = new List<string>();
        if (imageIdentifier != null)
        {
            var image = Services.Config.Value.GetImageByIdentifier(imageIdentifier);
            if (image != null) Add(image, imageNames);
        }
        else
        {
            foreach (var image in images)
            {
                Add(image, imageNames);
            }
        }
        foreach (var imageName in imageNames)
        {
            var image = Services.Config.Value.GetImageByImageName(imageName);
            if (image?.Identifier == null)
            {
                continue;
            }
            var root = new Tree(image.Identifier);
            var imagesListResponses = await DockerClientFacade.GetImagesAndChildrenAsync(imageName);
            if (imagesListResponses.Any())
            {
                foreach (var imagesListResponse in imagesListResponses)
                {
                    root.AddNode($"[yellow]{string.Join(", ", imagesListResponse.Labels.Keys)}[/]");
                }
            }
            else
            {
                root.AddNode("[grey]No children found[/]");
            }
            AnsiConsole.Write(root);
        }
    }

    private static void Add(Image image, ICollection<string> imageNames)
    {
        if (image?.ImageName == null)
        {
            throw new InvalidOperationException();
        }

        imageNames.Add(image.ImageName);
    }
}