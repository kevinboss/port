using System.Diagnostics;
using System.Runtime.InteropServices;
using dcma.Config;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace dcma;

public static class ConfigFactory
{
    private const string ConfigFileName = ".dcma";

    public static IConfig GetOrCreateConfig()
    {
        if (Debugger.IsAttached)
        {
            return CreateDefault();
        }
        
        var configFilePath = GetConfigFilePath();

        if (File.Exists(ConfigFileName))
        {
            return LoadConfig(configFilePath);
        }

        var config = CreateDefault();
        PersistConfig(config, configFilePath);
        return config;
    }

    private static string GetConfigFilePath()
    {
        var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configFilePath = Path.Combine(userProfilePath, ConfigFileName);
        return configFilePath;
    }

    private static Config10 LoadConfig(string path)
    {
        var yaml = File.ReadAllText(path);
        var serializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        return serializer.Deserialize<Config10>(yaml);
    }

    private static IConfig CreateDefault() => new Config10
    {
        DockerEndpoint = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "npipe://./pipe/docker_engine"
            : "unix:///var/run/docker.sock",
        Images = new List<ImageConfig>
        {
            new()
            {
                Identifier = "Getting.Started",
                ImageName = "docker/getting-started",
                ImageTag = "latest",
                Ports = new List<string>
                {
                    "80:80"
                }
            },
            new()
            {
                Identifier = "NgInx",
                ImageName = "nginx",
                ImageTag = "alpine",
                Ports = new List<string>
                {
                    "80:80"
                }
            },
        }
    };

    private static void PersistConfig(IConfig config, string path)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var yaml = serializer.Serialize(config);
        File.WriteAllText(path, yaml);
    }
}