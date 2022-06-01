using Docker.DotNet.Models;

namespace port;

public class Container
{
    public Container(ContainerListResponse containerListResponse)
    {
        var containerName = containerListResponse.Names.Single().Remove(0, 1);
        var containerNameAndTag = ContainerNameHelper.GetContainerNameAndTag(containerName);
        var imageNameAndTag = ImageNameHelper.GetImageNameAndTag(containerListResponse.Image);
        Id = containerListResponse.ID;
        ContainerName = containerNameAndTag.containerName;
        ContainerTag = containerNameAndTag.tag;
        if (containerNameAndTag.tag == imageNameAndTag.tag)
        {
            ImageName = imageNameAndTag.imageName;
            ImageTag = imageNameAndTag.tag;
        }
        else
        {
            ImageName = containerListResponse.Image;
            ImageTag = null;
        }

        Ports = containerListResponse.Ports;
        Running = containerListResponse.State == "running";
    }

    public string Id { get; }
    public string ContainerName { get; }
    public string? ContainerTag { get; }
    public string ImageName { get; }
    public string? ImageTag { get; }
    public IList<Port> Ports { get; }
    public bool Running { get; }
}