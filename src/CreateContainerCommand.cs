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

    public Task ExecuteAsync(string identifier, string imageName, string? tag, List<string> ports)
    {
        var portBindings = ports
            .Select(e => e.Split(PortSeparator))
            .ToDictionary(e => e[1], e => CreateHostPortList(e[0]));
        return _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = ContainerNameHelper.JoinContainerNameAndTag(identifier, tag),
            Image = ImageNameHelper.JoinImageNameAndTag(imageName, tag),
            HostConfig = new HostConfig
            {
                PortBindings = portBindings
            },
            ExposedPorts = portBindings.Keys.ToDictionary(port => port, _ => new EmptyStruct())
        });
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
            Name = ContainerNameHelper.JoinContainerNameAndTag(container.Identifier, container.Tag),
            Image = ImageNameHelper.JoinImageNameAndTag(container.ImageName, container.Tag),
            HostConfig = new HostConfig
            {
                PortBindings = portBindings
            },
            ExposedPorts = portBindings.Keys.ToDictionary(port => port, _ => new EmptyStruct())
        });
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