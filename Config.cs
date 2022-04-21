using System.Runtime.InteropServices;

namespace dcma;

public class Config
{
    public string? DockerEndpoint { get; set; }
    public List<Image> Images { get; set; } = new();

    public Image? GetImageByImageName(string imageName)
    {
        return Images.SingleOrDefault(e => e.ImageName == imageName);
    } 

    public Image? GetImageByIdentifier(string identifier)
    {
        return Images.SingleOrDefault(e => e.Identifier == identifier);
    } 

    public static Config CreateDefault() => new()
    {
        DockerEndpoint = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "npipe://./pipe/docker_engine" : "unix:///var/run/docker.sock",
        Images = new List<Image>
        {
            new()
            {
                Identifier = "Getting.Started",
                ImageName = "docker/getting-started",
                ImageTag = "latest",
                PortFrom = 80,
                PortTo = 80
            },
            new()
            {
                Identifier = "NgInx",
                ImageName = "nginx",
                ImageTag = "alpine",
                PortFrom = 80,
                PortTo = 80
            },
        }
    };
}