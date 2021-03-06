using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class CreateContainerCommand : ICreateContainerCommand
{
    private const string PortSeparator = ":";
    private readonly IDockerClient _dockerClient;

    public CreateContainerCommand(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task<string> ExecuteAsync(string containerIdentifier, string imageIdentifier, string? tag,
        IEnumerable<string> ports)
    {
        var portBindings = ports
            .Select(e => e.Split(PortSeparator))
            .ToDictionary(e => e[1], e => CreateHostPortList(e[0]));
        var containerName = ContainerNameHelper.BuildContainerName(containerIdentifier, tag);
        await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = containerName,
            Image = ImageNameHelper.BuildImageName(imageIdentifier, tag),
            HostConfig = new HostConfig
            {
                PortBindings = portBindings
            },
            ExposedPorts = portBindings.Keys.ToDictionary(port => port, _ => new EmptyStruct())
        });
        return containerName;
    }

    public Task ExecuteAsync(Container container)
    {
        var portBindings = container.Ports
            .Select(e => new List<string>
            {
                e.PublicPort.ToString(),
                e.PrivatePort.ToString()
            })
            .ToDictionary(e => e[1], e => CreateHostPortList(e[0]));
        return _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = container.ContainerName,
            Image = ImageNameHelper.BuildImageName(container.ImageIdentifier, container.ImageTag),
            HostConfig = new HostConfig
            {
                PortBindings = portBindings
            },
            ExposedPorts = portBindings.Keys.ToDictionary(port => port, _ => new EmptyStruct())
        });
    }

    public async Task<string> ExecuteAsync(string containerIdentifier, string imageIdentifier, string? tag,
        IList<Port> ports)
    {
        var portBindings = ports
            .Select(e => new List<string>
            {
                e.PublicPort.ToString(),
                e.PrivatePort.ToString()
            })
            .ToDictionary(e => e[1], e => CreateHostPortList(e[0]));
        var containerName = ContainerNameHelper.BuildContainerName(containerIdentifier, tag);
        await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = containerName,
            Image = ImageNameHelper.BuildImageName(imageIdentifier, tag),
            HostConfig = new HostConfig
            {
                PortBindings = portBindings
            },
            ExposedPorts = portBindings.Keys.ToDictionary(port => port, _ => new EmptyStruct())
        });
        return containerName;
    }

    private static IList<PortBinding> CreateHostPortList(string hostPort)
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