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

    private async Task LoadImages(string? imageIdentifier)
    {
        var images = Services.Config.Value.Images
            .Where(e => e.ImageName != null);
        var imageNames = new List<string>();
        if (imageIdentifier != null)
        {
            var image = images.SingleOrDefault(e => e.Identifier != imageIdentifier);
            Add(image, imageNames);
        }
        else
        {
            foreach (var image in images)
            {
                Add(image, imageNames);
            }
        }

        var imagesListResponses = await DockerClientFacade.GetImagesAndChildrenAsync(imageNames);
        foreach (var imageName in imageNames)
        {
        }
    }

    private static void Add(Image? image, List<string> imageNames)
    {
        if (image?.ImageName == null)
        {
            throw new InvalidOperationException();
        }

        imageNames.Add(image.ImageName);
    }
}