using System.Globalization;
using System.Text;
using Spectre.Console;

namespace port;

public static class TagTextBuilder
{
    public static (int first, int second) GetLengths(IEnumerable<Image> images)
    {
        var first = 0;
        var second = 0;
        foreach (var image in images)
        {
            var f = BuildFirstLine(image).RemoveMarkup().Length;
            if (first < f)
                first = f;
            var s = BuildSecondLine(image).RemoveMarkup().Length;
            if (second < s)
                second = s;
        }

        return (first, second);
    }

    public static string BuildTagText(Image image, (int first, int second) lengths)
    {
        var sb = new StringBuilder();
        switch (image)
        {
            case { Running: true, RunningUntaggedImage: false }:
                sb.Append("[green]\u25a0[/] ");
                break;
            case { Running: true, RunningUntaggedImage: true }:
                sb.Append("[orange3]\u25a0[/] ");
                break;
            default:
                sb.Append("[red]\u25a0[/] ");
                break;
        }

        var firstLine = BuildFirstLine(image);
        sb.Append(
            firstLine.PadRight(lengths.first + firstLine.Length - firstLine.RemoveMarkup().Length)
        );
        var secondLine = BuildSecondLine(image);
        if (!string.IsNullOrWhiteSpace(secondLine))
        {
            AddSeparator(sb);
            sb.Append("[dim]");
            sb.Append(
                $"{secondLine.PadRight(lengths.second + secondLine.Length - secondLine.RemoveMarkup().Length)}"
            );
            sb.Append("[/]");
        }

        var thirdLine = BuildThirdLine(image);
        if (!string.IsNullOrWhiteSpace(thirdLine))
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
        sb.Append($"[grey78]{image.Group.Identifier}[/]:");
        var imageTag = image.Tag;
        var tagPrefix = image.GetLabel(Constants.TagPrefix);
        if (tagPrefix is not null && imageTag?.StartsWith(tagPrefix) == true)
            imageTag = imageTag[tagPrefix.Length..];
        sb.Append($"[white]{imageTag ?? "<none>".EscapeMarkup()}[/]");

        if (image is { Running: true, RunningUntaggedImage: true })
            sb.Append(" | [orange3]untagged image[/]");

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
                if (image.Parent != null)
                {
                    var imageTag = image.Parent.Tag;
                    var tagPrefix = image.Parent.GetLabel(Constants.TagPrefix);
                    if (tagPrefix is not null && imageTag?.StartsWith(tagPrefix) == true)
                        imageTag = imageTag[tagPrefix.Length..];
                    sb.Append($" based on {imageTag ?? "[orange3]untagged image[/]"}");
                }
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
                $"Container: {container.Created.ToLocalTime().ToString(CultureInfo.CurrentCulture)}"
            );
        }

        return sb.ToString();
    }

    private static void AddSeparator(StringBuilder sb)
    {
        sb.Append("[dim] | [/]");
    }
}
