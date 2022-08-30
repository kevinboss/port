using System.Text;
using Spectre.Console;

namespace port;

public static class TagTextBuilder
{
    public static string BuildTagText(Image image)
    {
        var sb = new StringBuilder();
        sb.Append($"[white]{image.Tag ?? "<none>".EscapeMarkup()}");
        sb.Append(" (");
        if (image.IsSnapshot)
        {
            sb.Append("Snapshot");
            if (image.Parent != null)
                sb.Append($" based on {image.Parent.Tag ?? "[orange3]untagged image[/]"}");
        }
        else
            sb.Append("Base");
        sb.Append("[/]");
        var imageCreated = image.Created?.ToLocalTime();
        switch (image.IsSnapshot)
        {
            case false when !image.Existing:
                sb.Append(" / [red]missing[/]");
                break;
            case false when image.Existing:
                sb.Append($" / {imageCreated.ToString()}");
                break;
            case true:
                sb.Append($" / {imageCreated.ToString()}");
                break;
        }
        sb.Append(')');

        if (image.Running && !image.RelatedContainerIsRunningUntaggedImage)
            sb.Append(" [green]running[/]");
        if (image.Running && image.RelatedContainerIsRunningUntaggedImage)
            sb.Append(" (running [orange3]untagged image[/])");

        return sb.ToString();
    }
}