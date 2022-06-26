using Docker.DotNet.Models;

namespace port;

public class Container
{
    public Container(ContainerListResponse containerListResponse)
    {
        Id = containerListResponse.ID;
        var containerName = containerListResponse.Names.Single().Remove(0, 1);
        
        Name = containerName;

        var imageNameAndTag = ImageNameHelper.GetImageNameAndTag(containerListResponse.Image);
        if (imageNameAndTag.tag != null && containerName.EndsWith(imageNameAndTag.tag))
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
    public string Name { get; }
    public string ImageName { get; }
    public string? ImageTag { get; }
    public IList<Port> Ports { get; }
    public bool Running { get; }
}