namespace dcma.Config;

public interface IConfig
{
    string? DockerEndpoint { get; set; }
    List<ImageConfig> Images { get; set; }
    ImageConfig? GetImageByImageName(string imageName);
    ImageConfig? GetImageByIdentifier(string identifier);
}