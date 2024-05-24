namespace port.Config;

public class Config
{
    public string Version { get; set; } = Versions.V11;
    public string? DockerEndpoint { get; set; }
    public List<ImageConfig> ImageConfigs { get; set; } = new();

    public ImageConfig GetImageConfigByIdentifier(string identifier)
    {
        var imageConfig = ImageConfigs.SingleOrDefault(e => e.Identifier == identifier);
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
        public List<string> ImageTags { get; set; } = null!;
        public List<string> Ports { get; set; } = new();
        public List<string> Environment { get; set; } = new();
    }
}