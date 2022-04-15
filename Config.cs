using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace dcma;

public class Config
{
    public string? DockerEndpoint { get; set; }
    public List<Image> Images { get; set; } = new List<Image>();

    public static Config CreateDefault() => new Config
    {
        DockerEndpoint = "unix:///var/run/docker.sock",
        Images = new List<Image>
        {
            new()
            {
                Identifier = "Hello.World",
                ImageName = "alpine",
                ImageTag = "latest",
                PortFrom = 80,
                PortTo = 80
            },
            new()
            {
                Identifier = "Hello.World2",
                ImageName = "hello-world",
                ImageTag = "latest",
                PortFrom = 80,
                PortTo = 80
            },
        }
    };
}