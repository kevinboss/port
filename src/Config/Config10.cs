using System.Runtime.InteropServices;

namespace dcma.Config;

public class Config10 : IConfig
{
    public string Version { get; set; } = "1.0";
    public string? DockerEndpoint { get; set; }
    public List<ImageConfig> Images { get; set; } = new();

    public ImageConfig? GetImageByImageName(string imageName)
    {
        return Images.SingleOrDefault(e => e.ImageName == imageName);
    }

    public ImageConfig? GetImageByIdentifier(string identifier)
    {
        return Images.SingleOrDefault(e => e.Identifier == identifier);
    }
}