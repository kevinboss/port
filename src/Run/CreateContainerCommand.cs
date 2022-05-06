using Docker.DotNet;
using Docker.DotNet.Models;

namespace dcma.Run;

internal class CreateContainerCommand : ICreateContainerCommand
{
    private readonly IDockerClient _dockerClient;

    public CreateContainerCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public Task ExecuteAsync(string identifier, string imageName, string tag, List<string> ports)
    {
        return _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = $"{identifier}.{tag}",
            Image = DockerHelper.JoinImageNameAndTag(imageName, tag),
            HostConfig = new HostConfig
            {
                PortBindings = ports
                    .Select(e => e.Split(":"))
                    .ToDictionary(e => e[0], e => CreateHostPortList(e[1]))
            }
        });
    }

    private IList<PortBinding> CreateHostPortList(string hostPort)
    {
        return new List<PortBinding>
        {
            new()
            {
                HostPort = hostPort
            }
        };
    }
}