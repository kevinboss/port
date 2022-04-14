using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace dcma;

public class Config
{
    private const string ConfigFileName = ".dcma";
    private static Config? _config;

    public string? DockerEndpoint { get; set; }
    public List<Image> Images { get; set; } = new List<Image>();

    public static Config CreateDefault() => new Config
    {
        DockerEndpoint = "unix:///var/run/docker.sock",
        Images = new List<Image>
        {
            new Image
            {
                ImageAlias = "Hello.World",
                ImageName = "hello-world",
                ImageTag = "latest",
                PortFrom = 80,
                PortTo = 80
            }
        }
    };

    public static Config Instance
    {
        get
        {
            if (_config != null)
            {
                return _config;
            }

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
                return _config = serializer.Deserialize<Config>(yaml);
            }

            if (File.Exists(ConfigFileName))
            {
                return LoadConfig(configFilePath);
            }

            var config = Config.CreateDefault();
            //PersistConfig(config, configFilePath);
            return _config = config;
        }
    }
}