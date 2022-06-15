using Docker.DotNet.Models;

namespace port;

public class Container
{
    public Container(ContainerListResponse containerListResponse)
    {
        Id = containerListResponse.ID;
        var containerName = containerListResponse.Names.Single().Remove(0, 1);
        IsPortContainer = ContainerNameHelper.TryGetContainerNameAndTag(containerName, out var containerNameAndTag);
        if (IsPortContainer)
        {
            ContainerName = containerNameAndTag.containerName;
            ContainerTag = containerNameAndTag.tag;
        }
        else
        {
            ContainerName = containerName;
            ContainerTag = null;
        }

        var imageNameAndTag = ImageNameHelper.GetImageNameAndTag(containerListResponse.Image);
        if (IsPortContainer && containerNameAndTag.tag == imageNameAndTag.tag)
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

    public bool IsPortContainer { get; set; }

    public string Id { get; }
    public string ContainerName { get; }
    public string? ContainerTag { get; }
    public string ImageName { get; }
    public string? ImageTag { get; }
    public IList<Port> Ports { get; }
    public bool Running { get; }
}