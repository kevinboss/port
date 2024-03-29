using Docker.DotNet.Models;

namespace port;

public class Container
{
    private readonly IDictionary<string, string> _labels;

    public Container(ContainerListResponse containerListResponse, ContainerInspectResponse inspectContainerResponse)
    {
        Id = containerListResponse.ID;
        var containerName = containerListResponse.Names.Single().Remove(0, 1);
        ContainerName = containerName;
        Created = inspectContainerResponse.Created;

        if (ImageNameHelper.TryGetImageNameAndTag(containerListResponse.Image, out var imageNameAndTag) &&
            containerName.EndsWith(imageNameAndTag.tag))
        {
            ImageIdentifier = imageNameAndTag.imageName;
            ImageTag = imageNameAndTag.tag;
        }
        else
        {
            ImageIdentifier = containerListResponse.Image;
            ImageTag = null;
        }

        PortBindings = inspectContainerResponse.HostConfig.PortBindings;
        _labels = containerListResponse.Labels;
        Environment = inspectContainerResponse.Config.Env;
        Running = inspectContainerResponse.State.Running;
    }

    public DateTime Created { get; set; }

    public string Id { get; }
    public string ContainerName { get; }

    public string ContainerIdentifier =>
        _labels.Where(l => l.Key == Constants.IdentifierLabel)
            .Select(l => l.Value)
            .SingleOrDefault() ?? (ContainerTag is not null
            ? ContainerName.Replace($".{ContainerTag}", string.Empty)
            : ContainerName);

    public string? ContainerTag => ImageTag;
    public string ImageIdentifier { get; }
    public string? ImageTag { get; }
    public IDictionary<string, IList<PortBinding>> PortBindings { get; }
    public bool Running { get; }
    public IList<string> Environment { get; }

    public string? GetLabel(string label) => _labels.Where(l => l.Key == label)
        .Select(l => l.Value)
        .SingleOrDefault();
}