using System.Text.RegularExpressions;
using Docker.DotNet.Models;

namespace port;

public partial class Container
{
    private readonly IDictionary<string, string> _labels;
    private readonly string _containerName;

    public Container(ContainerListResponse containerListResponse, ContainerInspectResponse inspectContainerResponse)
    {
        Id = containerListResponse.ID;
        _containerName = containerListResponse.Names.Single().Remove(0, 1);
        Created = inspectContainerResponse.Created;

        (ImageIdentifier, ImageTag) = GetImageInfo(containerListResponse);

        PortBindings = inspectContainerResponse.HostConfig.PortBindings;
        _labels = containerListResponse.Labels;
        Environment = inspectContainerResponse.Config.Env;
        Running = inspectContainerResponse.State.Running;
    }

    private static (string identifier, string? tag) GetImageInfo(ContainerListResponse containerListResponse)
    {
        if (!ImageNameHelper.TryGetImageNameAndTag(containerListResponse.Image, out var imageNameAndTag))
            return (containerListResponse.Image, null);

        var tag = imageNameAndTag.tag;
        var tagPrefix = containerListResponse.Labels.GetValueOrDefault(Constants.TagPrefix);
        
        if (tagPrefix is not null && tag.StartsWith(tagPrefix))
            tag = tag[tagPrefix.Length..];

        var containerName = containerListResponse.Names.Single().Remove(0, 1);
        return containerName.EndsWith(tag) 
            ? (imageNameAndTag.imageName, imageNameAndTag.tag)
            : (containerListResponse.Image, null);
    }

    public DateTime Created { get; set; }

    public string Id { get; }

    public string ContainerName
    {
        get
        {
            if (!ContainerNameHelper.TryGetContainerNameAndTag(_containerName, out var containerNameAndTag))
                return _containerName;

            var tagPrefix = TagPrefix;
            var tag = containerNameAndTag.tag;
            if (tag.StartsWith(tagPrefix))
                tag = tag[tagPrefix.Length..];

            return ContainerNameHelper.BuildContainerName(containerNameAndTag.containerName, tag);
        }
    }

    public string ContainerIdentifier =>
        _labels.GetValueOrDefault(Constants.IdentifierLabel) ?? 
        (ContainerTag is not null ? ContainerName.Replace($".{ContainerTag}", string.Empty) : ContainerName);

    public string TagPrefix => TagPrefixHelper.GetTagPrefix(ContainerIdentifier);

    public string? ContainerTag => ImageTag;
    public string ImageIdentifier { get; }
    public string? ImageTag { get; }
    public IDictionary<string, IList<PortBinding>> PortBindings { get; }
    public bool Running { get; }
    public IList<string> Environment { get; }

    public string? GetLabel(string label) => _labels.GetValueOrDefault(label);
}
