using System.Text;

namespace port;

public static class TagTextBuilder
{
    public static string BuildTagText(Image image)
    {
        var imageTypeText = image.IsSnapshot ? "Snapshot" : "Base";
        var sb = new StringBuilder($"[blue]{image.Tag}");
        sb.Append($" ({imageTypeText})[/]");
        switch (image.IsSnapshot)
        {
            case false when !image.Existing:
                sb.Append($" [red]missing[/]");
                break;
            case false when image.Existing:
                sb.Append($" ({image.Created.ToString()})");
                break;
            case true:
                sb.Append($" ({image.Created.ToString()})");
                break;
        }

        return sb.ToString();
    }
}