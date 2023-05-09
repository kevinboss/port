namespace port;

public class Image
{
    public bool IsSnapshot { get; set; }
    public string? Tag { get; set; }
    public string Name { get; set; } = null!;
    public bool Existing { get; set; }
    public DateTime? Created { get; set; }
    public bool Running => Container is { Running: true };
    public bool RunningUntaggedImage => Container != null && Running && Container.ImageTag != Tag;
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

    public Container? Container { get; set; }
}