namespace port;

public class Image
{
    public bool IsSnapshot { get; set; }
    public string? Tag { get; set; }
    public string Name { get; set; } = null!;
    public bool Existing { get; set; }
    public DateTime? Created { get; set; }
    public bool Running { get; set; }
    public bool RelatedContainerIsRunningUntaggedImage { get; set; }
    public string? Id { get; set; }
    public string? ParentId { get; set; }
    public Image? Parent { get; set; }
    public ImageGroup Group { get; set; } = null!;
}