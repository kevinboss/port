using System.Text.RegularExpressions;
using Docker.DotNet.Models;

namespace port;

public partial class Container
{
    private readonly IDictionary<string, string> _labels;
    private readonly string _containerName;
    private readonly (string ImageIdentifier, string? ImageTag) _imageInfo;

    public Container(ContainerListResponse containerListResponse, ContainerInspectResponse inspectContainerResponse)
    {
        _labels = containerListResponse.Labels;
        _containerName = containerListResponse.Names.Single().Remove(0, 1);
        
        Id = containerListResponse.ID;
        Created = inspectContainerResponse.Created;
        PortBindings = inspectContainerResponse.HostConfig.PortBindings;
        Environment = inspectContainerResponse.Config.Env;
        Running = inspectContainerResponse.State.Running;

        _imageInfo = ImageNameHelper.TryGetImageNameAndTag(containerListResponse.Image, out var imageNameAndTag)
            ? ProcessImageInfo(imageNameAndTag, containerListResponse, _containerName)
            : (containerListResponse.Image, null);
    }

    public string Id { get; }
    public DateTime Created { get; }
    public IDictionary<string, IList<PortBinding>> PortBindings { get; }
    public IList<string> Environment { get; }
    public bool Running { get; }

    public string ImageIdentifier => _imageInfo.ImageIdentifier;
    public string? ImageTag => _imageInfo.ImageTag;

    private static (string ImageIdentifier, string? ImageTag) ProcessImageInfo(
        (string imageName, string tag) imageNameAndTag,
        ContainerListResponse containerListResponse,
        string containerName)
    {
        var tag = imageNameAndTag.tag;
        var tagPrefix = containerListResponse.Labels
            .Where(l => l.Key == Constants.TagPrefix)
            .Select(l => l.Value)
            .SingleOrDefault();

        if (tagPrefix is not null && tag.StartsWith(tagPrefix))
        {
            tag = tag[tagPrefix.Length..];
        }

        return containerName.EndsWith(tag)
            ? (imageNameAndTag.imageName, imageNameAndTag.tag)
            : (containerListResponse.Image, null);
    }
    public string ContainerName
    {
        get
        {
            if (!ContainerNameHelper.TryGetContainerNameAndTag(_containerName, out var containerNameAndTag))
                return _containerName;
                
            var tagPrefix = TagPrefix;
            var tag = containerNameAndTag.tag.StartsWith(tagPrefix)
                ? containerNameAndTag.tag[tagPrefix.Length..]
                : containerNameAndTag.tag;
                
            return ContainerNameHelper.BuildContainerName(containerNameAndTag.containerName, tag);
        }
    }

    public string ContainerIdentifier =>
        _labels.Where(l => l.Key == Constants.IdentifierLabel)
            .Select(l => l.Value)
            .SingleOrDefault() 
        ?? (ContainerTag is not null
            ? ContainerName.Replace($".{ContainerTag}", string.Empty)
            : ContainerName);

    public string TagPrefix => TagPrefixHelper.GetTagPrefix(ContainerIdentifier);
    public string? ContainerTag => ImageTag;

    public string? GetLabel(string label) => 
        _labels
            .Where(l => l.Key == label)
            .Select(l => l.Value)
            .SingleOrDefault();
}
