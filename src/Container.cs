using System.Text.RegularExpressions;
using Docker.DotNet.Models;

namespace port;

public class Container
{
    private readonly IDictionary<string, string> _labels;
    private readonly string _containerName;

    public Container(
        ContainerListResponse containerListResponse,
        ContainerInspectResponse inspectContainerResponse
    )
    {
        Id = containerListResponse.ID;
        ImageId = containerListResponse.ImageID;
        var containerName = containerListResponse.Names.Single().Remove(0, 1);
        _containerName = containerName;
        Created = inspectContainerResponse.Created;

        // Check if base tag label contains a digest reference
        var baseTagFromLabel = containerListResponse
            .Labels.Where(l => l.Key == Constants.BaseTagLabel)
            .Select(l => l.Value)
            .SingleOrDefault();

        if (baseTagFromLabel != null && ImageNameHelper.IsDigest(baseTagFromLabel))
        {
            // Container was created from a digest reference (by port)
            ImageIdentifier = ImageNameHelper.TryGetImageNameAndTag(
                containerListResponse.Image,
                out var parsed
            )
                ? parsed.imageName
                : containerListResponse.Image;
            ImageTag = baseTagFromLabel;
        }
        else if (
            ImageNameHelper.TryGetImageNameAndTag(
                containerListResponse.Image,
                out var imageNameAndTag
            )
        )
        {
            var tag = imageNameAndTag.tag;

            // Check if this is a digest reference (e.g., from docker-compose)
            if (ImageNameHelper.IsDigest(tag))
            {
                ImageIdentifier = imageNameAndTag.imageName;
                ImageTag = tag;
            }
            else
            {
                var tagPrefix = containerListResponse
                    .Labels.Where(l => l.Key == Constants.TagPrefix)
                    .Select(l => l.Value)
                    .SingleOrDefault();
                if (tagPrefix is not null && tag.StartsWith(tagPrefix))
                    tag = tag[tagPrefix.Length..];
                if (containerName.EndsWith(tag))
                {
                    ImageIdentifier = imageNameAndTag.imageName;
                    ImageTag = imageNameAndTag.tag;
                }
                else
                {
                    ImageIdentifier = containerListResponse.Image;
                    ImageTag = null;
                }
            }
        }
        else
        {
            ImageIdentifier = containerListResponse.Image;
            ImageTag = null;
        }

        PortBindings = inspectContainerResponse.HostConfig.PortBindings;
        _labels = containerListResponse.Labels;
        Environment = inspectContainerResponse.Config.Env;
        Running = inspectContainerResponse.State.Running;
    }

    public DateTime Created { get; set; }

    public string Id { get; }
    public string ImageId { get; }

    public string ContainerName
    {
        get
        {
            if (
                !ContainerNameHelper.TryGetContainerNameAndTag(
                    _containerName,
                    TagPrefix,
                    out var containerNameAndTag
                )
            )
                return _containerName;
            var tagPrefix = TagPrefix;
            var tag = containerNameAndTag.tag.StartsWith(tagPrefix)
                ? containerNameAndTag.tag[tagPrefix.Length..]
                : containerNameAndTag.tag;
            return ContainerNameHelper.BuildContainerName(containerNameAndTag.containerName, tag);
        }
    }

    public string ContainerIdentifier =>
        _labels
            .Where(l => l.Key == Constants.IdentifierLabel)
            .Select(l => l.Value)
            .SingleOrDefault()
        ?? (
            ContainerTag is not null
                ? _containerName.Replace($".{ContainerTag}", string.Empty)
                : _containerName
        );

    public string TagPrefix => TagPrefixHelper.GetTagPrefix(ContainerIdentifier);

    public string? ContainerTag => ImageTag;
    public string ImageIdentifier { get; }
    public string? ImageTag { get; }
    public IDictionary<string, IList<PortBinding>> PortBindings { get; }
    public bool Running { get; }
    public IList<string> Environment { get; }

    public string? GetLabel(string label) =>
        _labels.Where(l => l.Key == label).Select(l => l.Value).SingleOrDefault();
}
