using Docker.DotNet.Models;

namespace port;

public class Container
{
    public Container(ContainerListResponse containerListResponse, ContainerInspectResponse inspectContainerResponse)
    {
        Id = containerListResponse.ID;
        var containerName = containerListResponse.Names.Single().Remove(0, 1);

        if (ContainerNameHelper.TryGetContainerNameAndTag(containerName, out var containerNameAndTag))
        {
            ContainerIdentifier = containerNameAndTag.containerName;
            ContainerTag = containerNameAndTag.tag;
        }
        else
        {
            ContainerIdentifier = containerNameAndTag.containerName;
            ContainerTag = null;
        }

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
        Environment = inspectContainerResponse.Config.Env;
        Running = inspectContainerResponse.State.Running;
    }

    public string Id { get; }
    public string ContainerName => ContainerNameHelper.BuildContainerName(ContainerIdentifier, ContainerTag);
    public string ContainerIdentifier { get; }
    public string? ContainerTag { get; }
    public string ImageIdentifier { get; }
    public string? ImageTag { get; }
    public IDictionary<string, IList<PortBinding>> PortBindings { get; }
    public bool Running { get; }
    public IList<string> Environment { get; }
}