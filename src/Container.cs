using Docker.DotNet.Models;

namespace port;

public class Container
{
    public Container(ContainerListResponse containerListResponse, ContainerInspectResponse inspectContainerResponse)
    {
        Id = containerListResponse.ID;
        var containerName = containerListResponse.Names.Single().Remove(0, 1);
        ContainerName = containerName;

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
        BaseTag = containerListResponse.Labels.Where(l => l.Key == Constants.BaseTagLabel)
            .Select(l => l.Value)
            .SingleOrDefault();
        Environment = inspectContainerResponse.Config.Env;
        Running = inspectContainerResponse.State.Running;
    }

    public string Id { get; }
    public string ContainerName { get; }

    public string ContainerIdentifier =>
        ContainerTag != null ? ContainerName.Replace($".{ContainerTag}", string.Empty) : ContainerName;

    public string? ContainerTag => ImageTag;
    public string ImageIdentifier { get; }
    public string? ImageTag { get; }
    public IDictionary<string, IList<PortBinding>> PortBindings { get; }
    public bool Running { get; }
    public IList<string> Environment { get; }
    public string? BaseTag { get; }
}