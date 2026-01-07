namespace port.Config;

public static class ConfigMerger
{
    public static Config MergeWithComposeFile(Config globalConfig, ComposeFile composeFile)
    {
        var composeImageConfigs = composeFile.Services
            .Select(kvp => ComposeFileParser.ConvertToImageConfig(kvp.Key, kvp.Value))
            .Where(ic => ic != null)
            .Cast<Config.ImageConfig>()
            .ToList();

        return MergeConfigs(globalConfig, composeImageConfigs);
    }

    private static Config MergeConfigs(Config globalConfig, List<Config.ImageConfig> additionalConfigs)
    {
        var mergedConfigs = globalConfig.ImageConfigs
            .ToDictionary(ic => ic.Identifier, ic => ic);

        foreach (var composeConfig in additionalConfigs)
        {
            mergedConfigs[composeConfig.Identifier] = composeConfig;
        }

        return new Config
        {
            Version = globalConfig.Version,
            DockerEndpoint = globalConfig.DockerEndpoint,
            ImageConfigs = mergedConfigs.Values.ToList()
        };
    }
}
