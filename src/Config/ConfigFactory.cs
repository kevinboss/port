using System.Runtime.InteropServices;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace port.Config;

public static class ConfigFactory
{
    private const string ConfigFileName = ".port";
    private const string OldConfigFileName = ".dcma";

    public static Config GetOrCreateConfig()
    {
        var configFilePath = GetConfigFilePath();

        RenameIfNecessary(configFilePath);

        if (File.Exists(configFilePath))
        {
            MigrateIfNecessary(configFilePath);
            return LoadConfig(configFilePath);
        }

        var config = CreateDefault();
        PersistConfig(config, configFilePath);
        return config;
    }

    private static void RenameIfNecessary(string configFilePath)
    {
        var oldConfigFilePath = GetOldConfigFilePath();
        if (File.Exists(oldConfigFilePath) && !File.Exists(configFilePath))
        {
            File.Move(oldConfigFilePath, configFilePath);
        }
    }

    public static string GetConfigFilePath()
    {
        var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configFilePath = Path.Combine(userProfilePath, ConfigFileName);
        return configFilePath;
    }

    private static string GetOldConfigFilePath()
    {
        var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfilePath, OldConfigFileName);
    }

    private static void MigrateIfNecessary(string path)
    {
        var versionString = File.ReadLines(path).First();
        var serializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var version = serializer.Deserialize<ConfigVersion>(versionString);
        switch (version.Version)
        {
            case Versions.V10:
                var yaml = File.ReadAllText(path);
                PersistConfig(
                    ConfigMigrations.Migrate10To11(serializer.Deserialize<Config10>(yaml)),
                    path
                );
                break;
            case Versions.V11:
                break;
        }
    }

    private static Config LoadConfig(string path)
    {
        var yaml = File.ReadAllText(path);
        var serializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        return serializer.Deserialize<Config>(yaml);
    }

    private static Config CreateDefault() =>
        new Config
        {
            DockerEndpoint = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "npipe://./pipe/docker_engine"
                : "unix:///var/run/docker.sock",
            ImageConfigs = new List<Config.ImageConfig>
            {
                new()
                {
                    Identifier = "Getting.Started",
                    ImageName = "docker/getting-started",
                    ImageTags = new List<string> { "latest" },
                    Ports = new List<string> { "80:80" },
                    Environment = new List<string> { "80:80" },
                },
            },
        };

    private static void PersistConfig(Config config, string path)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var yaml = serializer.Serialize(config);
        File.WriteAllText(path, yaml);
    }

    public class ConfigVersion
    {
        public string Version { get; set; } = null!;
    }
}
