using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace dcma;

public static class Services
{
    private const string ConfigFileName = ".dcma";

    public static readonly Lazy<Config> Config = new(GetOrCreateConfig);

    private static Config GetOrCreateConfig()
    {
        var configFilePath = GetConfigFilePath();

        if (File.Exists(ConfigFileName))
        {
            return LoadConfig(configFilePath);
        }

        var config = dcma.Config.CreateDefault();
        PersistConfig(config, configFilePath);
        return config;
    }

    private static string GetConfigFilePath()
    {
        var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configFilePath = Path.Combine(userProfilePath, ConfigFileName);
        return configFilePath;
    }

    private static Config LoadConfig(string path)
    {
        var yaml = File.ReadAllText(path);
        var serializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        return serializer.Deserialize<Config>(yaml);
    }

    private static void PersistConfig(Config config, string path)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var yaml = serializer.Serialize(config);
        File.WriteAllText(path, yaml);
    }
}