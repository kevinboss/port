using Docker.DotNet.Models;

namespace port;

public class Container
{
    public Container(ContainerListResponse containerListResponse, ContainerInspectResponse inspectContainerResponse)
    {
        Id = containerListResponse.ID;
        var containerName = containerListResponse.Names.Single().Remove(0, 1);
        
        ContainerName = containerName;

        var imageNameAndTag = ImageNameHelper.GetImageNameAndTag(containerListResponse.Image);
        if (imageNameAndTag.tag != null && containerName.EndsWith(imageNameAndTag.tag))
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
    public string ContainerName { get; }

    public string ContainerIdentifier => ImageTag != null ? ContainerName.Replace($".{ImageTag}", string.Empty) : ContainerIdentifier;

    public string? ContainerTag => ImageTag;
    public string ImageIdentifier { get; }
    public string? ImageTag { get; }
    public IDictionary<string, IList<PortBinding>> PortBindings { get; }
    public bool Running { get; }
    public IList<string> Environment { get; set; }
}