using Docker.DotNet;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace dcma;

public static class Services
{
    private const string ConfigFileName = ".dcma";

    public static readonly Lazy<DockerClient> DockerClient = new(CreateDockerClient);
    public static readonly Lazy<Config> Config = new(GetOrCreateConfig);

    private static DockerClient CreateDockerClient()
    {
        if (Config.Value.DockerEndpoint == null)
        {
            throw new InvalidOperationException();
        }

        var endpoint = new Uri(Config.Value.DockerEndpoint);
        return new DockerClientConfiguration(endpoint)
            .CreateClient();
    }

    private static Config GetOrCreateConfig()
    {
        var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configFilePath = Path.Combine(userProfilePath, ConfigFileName);

        void PersistConfig(Config config, string path)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(config);
            File.WriteAllText(path, yaml);
        }

        Config LoadConfig(string path)
        {
            var yaml = File.ReadAllText(path);
            var serializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            return serializer.Deserialize<Config>(yaml);
        }

        if (File.Exists(ConfigFileName))
        {
            return LoadConfig(configFilePath);
        }

        var config = dcma.Config.CreateDefault();
        //PersistConfig(config, configFilePath);
        return config;
    }
}