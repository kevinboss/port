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
        Identifier = containerNameAndTag.identifier;
        ImageName = imageNameAndTag.imageName;
        Tag = imageNameAndTag.tag;
        Ports = containerListResponse.Ports;
    }

    public string Id { get; }
    public string Identifier { get; }
    public string ImageName { get; }
    public string Tag { get; }
    public IList<Port> Ports { get; }
}