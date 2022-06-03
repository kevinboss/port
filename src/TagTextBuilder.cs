using System.Text;
using Spectre.Console;

namespace port;

public static class TagTextBuilder
{
    public static string BuildTagText(Image image)
    {
        var imageTypeText = image.IsSnapshot ? "Snapshot" : "Base";
        var sb = new StringBuilder($"[blue]{image.Tag ?? "<none>".EscapeMarkup()}");
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

        if (image.Running)
            sb.Append(" [running]".EscapeMarkup());
        if (image.Tag == null)
            sb.Append($" [blue]{"[uses untagged image]".EscapeMarkup()}[/]");

        return sb.ToString();
    }
}