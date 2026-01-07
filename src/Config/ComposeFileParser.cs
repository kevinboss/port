using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace port.Config;

public static class ComposeFileParser
{
    private static readonly string[] ComposeFileNames =
    {
        "docker-compose.yml",
        "docker-compose.yaml",
        "compose.yml",
        "compose.yaml"
    };

    public static ComposeFile? TryParseFromDirectory(string directory)
    {
        var composeFilePath = FindComposeFile(directory);
        if (composeFilePath == null) return null;

        return ParseComposeFile(composeFilePath);
    }

    public static string? FindComposeFile(string directory)
    {
        foreach (var fileName in ComposeFileNames)
        {
            var path = Path.Combine(directory, fileName);
            if (File.Exists(path)) return path;
        }

        return null;
    }

    public static ComposeFile ParseComposeFile(string path)
    {
        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        return deserializer.Deserialize<ComposeFile>(yaml);
    }

    public static Config.ImageConfig? ConvertToImageConfig(string serviceName, ComposeService service)
    {
        if (string.IsNullOrWhiteSpace(service.Image)) return null;

        var (imageName, tags) = ParseImageReference(service.Image);

        return new Config.ImageConfig
        {
            Identifier = serviceName,
            ImageName = imageName,
            ImageTags = tags,
            Ports = NormalizePorts(service.Ports),
            Environment = service.Environment ?? new List<string>()
        };
    }

    private static (string imageName, List<string> tags) ParseImageReference(string imageRef)
    {
        if (imageRef.Contains('@'))
        {
            var atIndex = imageRef.IndexOf('@');
            var imageName = imageRef.Substring(0, atIndex);
            var digest = imageRef.Substring(atIndex + 1);
            return (imageName, new List<string> { digest });
        }

        var lastColonIndex = imageRef.LastIndexOf(':');

        if (lastColonIndex > 0 && !imageRef.Substring(lastColonIndex).Contains('/'))
        {
            var imageName = imageRef.Substring(0, lastColonIndex);
            var tag = imageRef.Substring(lastColonIndex + 1);
            return (imageName, new List<string> { tag });
        }

        return (imageRef, new List<string> { "latest" });
    }

    private static List<string> NormalizePorts(List<string>? ports)
    {
        if (ports == null) return new List<string>();

        return ports.Select(NormalizePort).ToList();
    }

    private static string NormalizePort(string port)
    {
        var parts = port.Split(':');
        return parts.Length switch
        {
            1 => $"{parts[0]}:{parts[0]}",
            2 => port,
            3 => $"{parts[1]}:{parts[2]}",
            _ => port
        };
    }
}
