using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class CreateContainerCommand : ICreateContainerCommand
{
    private const string PortSeparator = ":";
    private readonly IDockerClient _dockerClient;
    private readonly IGetImageQuery _getImageQuery;

    public CreateContainerCommand(IDockerClient dockerClient, IGetImageQuery getImageQuery)
    {
        _dockerClient = dockerClient;
        _getImageQuery = getImageQuery;
    }

    public async Task<string> ExecuteAsync(string containerIdentifier, string imageIdentifier, string? tag,
        IEnumerable<string> ports, IList<string> environment)
    {
        var portBindings = ports
            .Select(e => e.Split(PortSeparator))
            .ToDictionary(e => e[1], e => CreateHostPortList(e[0]));
        var containerName = ContainerNameHelper.BuildContainerName(containerIdentifier, tag);
        var image = await _getImageQuery.QueryAsync(imageIdentifier, tag);
        var baseTag = image?.BaseImage?.Tag ?? tag;
        var labels = new Dictionary<string, string>
        {
            { Constants.IdentifierLabel, containerIdentifier }
        };
        if (baseTag is not null) labels.Add(Constants.BaseTagLabel, baseTag);
        await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = containerName,
            Image = ImageNameHelper.BuildImageName(imageIdentifier, tag),
            Env = environment,
            HostConfig = new HostConfig
            {
                PortBindings = portBindings
            },
            ExposedPorts = portBindings.Keys.ToDictionary(port => port, _ => new EmptyStruct()),
            Labels = labels
        });
        return containerName;
    }

    public async Task<string> ExecuteAsync(Container container, string newTag)
    {
        var portBindings = container.PortBindings;
        var environment = container.Environment;
        var containerName = ContainerNameHelper.BuildContainerName(container.ContainerIdentifier, newTag);
        var labels = new Dictionary<string, string>();
        var identifier = container.GetLabel(Constants.IdentifierLabel);
        if (identifier is not null) labels.Add(Constants.IdentifierLabel, identifier);
        var baseTag = container.GetLabel(Constants.BaseTagLabel);
        if (baseTag is not null) labels.Add(Constants.BaseTagLabel, baseTag);
        await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = containerName,
            Image = ImageNameHelper.BuildImageName(container.ImageIdentifier, newTag),
            HostConfig = new HostConfig
            {
                PortBindings = portBindings
            },
            Env = environment,
            ExposedPorts = portBindings.Keys.ToDictionary(port => port, _ => new EmptyStruct()),
            Labels = labels
        });
        return containerName;
    }

    public Task ExecuteAsync(Container container)
    {
        var portBindings = container.PortBindings;
        var environment = container.Environment;
        var labels = new Dictionary<string, string>();
        var identifier = container.GetLabel(Constants.IdentifierLabel);
        if (identifier is not null) labels.Add(Constants.IdentifierLabel, identifier);
        var baseTag = container.GetLabel(Constants.BaseTagLabel);
        if (baseTag is not null) labels.Add(Constants.BaseTagLabel, baseTag);
        return _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = container.ContainerName,
            Image = ImageNameHelper.BuildImageName(container.ImageIdentifier, container.ImageTag),
            HostConfig = new HostConfig
            {
                PortBindings = portBindings
            },
            Env = environment,
            ExposedPorts = portBindings.Keys.ToDictionary(port => port, _ => new EmptyStruct()),
            Labels = labels
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