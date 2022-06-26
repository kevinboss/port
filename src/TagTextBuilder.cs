using System.Text;
using Spectre.Console;

namespace port;

public static class TagTextBuilder
{
    public static string BuildTagText(Image image)
    {
        var imageTypeText = image.IsSnapshot ? "Snapshot" : "Base";
        var sb = new StringBuilder($"[white]{image.Tag ?? "<none>".EscapeMarkup()}");
        sb.Append($" ({imageTypeText})[/]");
        switch (image.IsSnapshot)
        {
            case false when !image.Existing:
                sb.Append(" [red]missing[/]");
                break;
            case false when image.Existing:
                sb.Append($" ({image.Created.ToString()})");
                break;
            case true:
                sb.Append($" ({image.Created.ToString()})");
                break;
        }

        if (image.Running && !image.RelatedContainerIsRunningUntaggedImage)
            sb.Append($" [green]running[/]");
        if (image.Running && image.RelatedContainerIsRunningUntaggedImage)
            sb.Append(" (running [orange3]untagged image[/])");
        if (image.Parent != null)
            sb.Append($" (based on {image.Parent.Tag ?? "[orange3]untagged image[/]"})");

        return sb.ToString();
    }
}