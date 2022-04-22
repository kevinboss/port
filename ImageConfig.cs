namespace dcma;

public class ImageConfig
{
    public string Identifier { get; set; } = null!;
    public string ImageName { get; set; } = null!;
    public string ImageTag { get; set; } = null!;
    public int PortFrom { get; set; }
    public int PortTo { get; set; }
}