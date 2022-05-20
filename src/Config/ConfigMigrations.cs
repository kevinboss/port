namespace port.Config;

public static class ConfigMigrations
{
    public static Config Migrate10To11(Config10 config)
    {
        return new Config
        {
            DockerEndpoint = config.DockerEndpoint,
            ImageConfigs = config.Images.Select(e => new Config.ImageConfig
            {
                Identifier = e.Identifier,
                ImageName = e.ImageName,
                Ports = e.Ports,
                ImageTags = new List<string>
                {
                    e.ImageTag
                }
            }).ToList()
        };
    }
}