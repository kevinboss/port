namespace port;

public class Image
{
    private readonly IDictionary<string, string> _labels;

    public Image(IDictionary<string, string> labels)
    {
        _labels = labels;
    }
    
    public bool IsSnapshot { get; set; }
    public string? Tag { get; set; }
    public string Name { get; set; } = null!;
    public bool Existing { get; set; }
    public DateTime? Created { get; set; }
    public bool Running => Containers.Any(container => container is { Running: true });

    public bool RunningUntaggedImage =>
        Containers.Any(container =>
        {
            var imageTag = container.ImageTag;
            var tagPrefix = container.GetLabel(Constants.TagPrefix);
            if (tagPrefix is not null && imageTag?.StartsWith(tagPrefix) == true) imageTag = imageTag[tagPrefix.Length..];
            return container is { Running: true } && imageTag != Tag;
        });

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

    public string? GetLabel(string label) => _labels.Where(l => l.Key == label)
        .Select(l => l.Value)
        .SingleOrDefault();
}