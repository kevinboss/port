namespace port.Config;

public class Config10
{
    public string Version { get; set; } = Versions.V10;
    public string? DockerEndpoint { get; set; }
    public List<ImageConfig> Images { get; set; } = new();

    public ImageConfig GetImageByImageName(string imageName)
    {
        var imageConfig = Images.SingleOrDefault(e => e.ImageName == imageName);
        if (imageConfig == null)
        {
            throw new ArgumentException($"There is no config defined for imageName '{imageName}'",
                nameof(imageName));
        }

        return imageConfig;
    }

    public ImageConfig GetImageConfigByIdentifier(string identifier)
    {
        var imageConfig = Images.SingleOrDefault(e => e.Identifier == identifier);
        if (imageConfig == null)
        {
            throw new ArgumentException($"There is no config defined for identifier '{identifier}'",
                nameof(identifier));
        }

        return imageConfig;
    }
    
    public class ImageConfig
    {
        public string Identifier { get; set; } = null!;
        public string ImageName { get; set; } = null!;
        public string ImageTag { get; set; } = null!;
        public List<string> Ports { get; set; } = new();
    }
}