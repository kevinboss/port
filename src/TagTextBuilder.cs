using System.Globalization;
using System.Text;
using Spectre.Console;

namespace port;

public static class TagTextBuilder
{
    public static string BuildTagText(Image image)
    {
        var sb = new StringBuilder();
        sb.Append(BuildFirstLine(image));
        var secondLine = BuildSecondLine(image);
        if (!string.IsNullOrEmpty(secondLine))
        {
            AddSeparator(sb);
            sb.Append("[dim]");
            sb.Append($"{secondLine}");
            sb.Append("[/]");
        }

        var thirdLine = BuildThirdLine(image);
        if (!string.IsNullOrEmpty(thirdLine))
        {
            AddSeparator(sb);
            sb.Append("[dim]");
            sb.Append($"{thirdLine}");
            sb.Append("[/]");
        }

        return sb.ToString();
    }

    private static string BuildFirstLine(Image image)
    {
        var sb = new StringBuilder();
        sb.Append($"[white]{image.Tag ?? "<none>".EscapeMarkup()}[/]");
        switch (image.IsSnapshot)
        {
            case false when !image.Existing:
                sb.Append(" | [red]missing[/]");
                break;
        }

        if (image is { Running: true, RunningUntaggedImage: false })
            sb.Append(" | [green]running[/]");
        if (image is { Running: true, RunningUntaggedImage: true })
            sb.Append(" | [green]running[/] [orange3]untagged image[/]");

        return sb.ToString();
    }

    private static string BuildSecondLine(Image image)
    {
        var sb = new StringBuilder();
        var imageCreated = image.Created?.ToLocalTime();
        switch (image.IsSnapshot)
        {
            case false when image.Existing:
                sb.Append($"[white]Image: {imageCreated.ToString()}[/]");
                break;
            case true:
                sb.Append($"[white]Image: {imageCreated.ToString()}");
                sb.Append(" | Snapshot");
                if (image.Parent != null)
                    sb.Append($" based on {image.Parent.Tag ?? "[orange3]untagged image[/]"}");
                else
                    sb.Append(" based on [red]unknown image[/]");
                sb.Append("[/]");
                break;
        }

        return sb.ToString();
    }

    private static string BuildThirdLine(Image image)
    {
        var sb = new StringBuilder();
        foreach (var container in image.Containers)
        {
            sb.Append(
                $"Container: {container.Created.ToLocalTime().ToString(CultureInfo.CurrentCulture)}");
        }

        return sb.ToString();
    }

    private static void AddSeparator(StringBuilder sb)
    {
        sb.Append(" | ");
    }
}