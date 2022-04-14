using Docker.DotNet;

namespace dcma;

public static class DockerClient
{
    private static Docker.DotNet.DockerClient? _dockerClient;
    public static Docker.DotNet.DockerClient Instance
    {
        get
        {
            if (Config.Instance.DockerEndpoint == null)
            {
                throw new InvalidOperationException();
            }
            return _dockerClient ??= new DockerClientConfiguration(
                    new Uri(Config.Instance.DockerEndpoint))
                .CreateClient();
        }
    }
}