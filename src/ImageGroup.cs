namespace port;

public class ImageGroup
{
    public string? Identifier { get; set; }
    public List<Image> Images { get; set; } = new();
}