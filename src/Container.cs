using Docker.DotNet.Models;

namespace dcma;

public class Container
{
    public Container(string id, string containerName, string imageName, string tag, IList<Port> ports)
    {
        Id = id;
        ContainerName = containerName;
        ImageName = imageName;
        Tag = tag;
        Ports = ports;
    }

    public string Id { get; }
    public string ContainerName { get; }
    public string ImageName { get; }
    public string Tag { get; }
    public IList<Port> Ports { get; }
}