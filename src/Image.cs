namespace port;

public class Image
{
    public bool IsSnapshot { get; set; }
    public string Identifier { get; set; } = null!;
    public string? Tag { get; set; }
    public string Name { get; set; } = null!;
    public bool Existing { get; set; }
    public DateTime? Created { get; set; }
    public bool Running { get; set; }
    public bool RunningUntaggedImage { get; set; }
}