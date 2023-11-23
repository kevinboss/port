namespace port;

public class Image
{
    public bool IsSnapshot { get; set; }
    public string? Tag { get; set; }
    public string Name { get; set; } = null!;
    public bool Existing { get; set; }
    public DateTime? Created { get; set; }
    public bool Running => Containers.Any(container => container is { Running: true });

    public bool RunningUntaggedImage =>
        Containers.Any(container => container is { Running: true } && container.ImageTag != Tag);

    public string? Id { get; set; }
    public string? ParentId { get; set; }
    public Image? Parent { get; set; }
    public ImageGroup Group { get; set; } = null!;

    public Image? BaseImage
    {
        get
        {
            var image = Parent;
            while (image?.Parent != null)
            {
                image = image.Parent;
            }

            return image;
        }
    }

    public IReadOnlyList<Container> Containers { get; set; } = new List<Container>();
}