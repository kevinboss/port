using System.Text.RegularExpressions;
using port.Commands.List;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Commit;

internal class CommitCliCommand : AsyncCommand<CommitSettings>
{
    private readonly ICreateImageFromContainerCommand _createImageFromContainerCommand;
    private readonly IGetRunningContainersQuery _getRunningContainersQuery;
    private readonly IGetImageQuery _getImageQuery;
    private readonly IContainerNamePrompt _containerNamePrompt;
    private readonly IStopContainerCommand _stopContainerCommand;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;
    private readonly IGetDigestsByIdQuery _getDigestsByIdQuery;
    private readonly IGetContainersQuery _getContainersQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly ListCliCommand _listCliCommand;

    public CommitCliCommand(ICreateImageFromContainerCommand createImageFromContainerCommand,
        IGetRunningContainersQuery getRunningContainersQuery, IGetImageQuery getImageQuery,
        IContainerNamePrompt containerNamePrompt, IStopContainerCommand stopContainerCommand,
        ICreateContainerCommand createContainerCommand, IRunContainerCommand runContainerCommand,
        IGetDigestsByIdQuery getDigestsByIdQuery, IGetContainersQuery getContainersQuery,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand, ListCliCommand listCliCommand)
    {
        _createImageFromContainerCommand = createImageFromContainerCommand;
        _getRunningContainersQuery = getRunningContainersQuery;
        _getImageQuery = getImageQuery;
        _containerNamePrompt = containerNamePrompt;
        _stopContainerCommand = stopContainerCommand;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
        _getDigestsByIdQuery = getDigestsByIdQuery;
        _getContainersQuery = getContainersQuery;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _listCliCommand = listCliCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CommitSettings settings)
    {
        if (settings.Overwrite) settings.Switch = true;
        
        var container = await GetContainerAsync(settings);
        if (container == null)
        {
            throw new InvalidOperationException("No running container found");
        }

        string imageName;
        string tagPrefix;
        string newTag;
        if (settings.Overwrite)
        {
            newTag = container.ImageTag ?? throw new InvalidOperationException("When using --overwrite, container must have an image tag");
            imageName = container.ImageIdentifier;
            tagPrefix = container.TagPrefix;
        }
        else
        {
            var tag = settings.Tag ?? $"{DateTime.Now:yyyyMMddhhmmss}";
            (imageName, tagPrefix, newTag) = await GetNewTagAsync(container, tag);
        }


        var containerWithSameTag = await Spinner.StartAsync(
            $"Looking for existing container named '{container.ContainerName}'",
            async _ => await _getContainersQuery
                .QueryByContainerIdentifierAndTagAsync(container.ContainerIdentifier, newTag)
                .ToListAsync());

        await Spinner.StartAsync($"Creating image from running container '{container.ContainerName}'",
            async _ =>
            {
                return newTag =
                    await _createImageFromContainerCommand.ExecuteAsync(container, imageName, tagPrefix, newTag);
            });


        await Spinner.StartAsync($"Removing containers named '{container.ContainerName}'",
            async _ =>
            {
                await Task.WhenAll(containerWithSameTag.Select(async container1 =>
                    await _stopAndRemoveContainerCommand.ExecuteAsync(container1.Id)));
            });

        if (settings.Switch)
        {
            if (newTag == null)
                throw new InvalidOperationException("newTag is null");

            if (container.ImageTag == null)
                throw new InvalidOperationException(
                    "Switch argument not supported when creating image from untagged container");

            await SwitchToNewImageAsync(container, tagPrefix, newTag);
        }

        await _listCliCommand.ExecuteAsync();

        return 0;
    }

    private async Task SwitchToNewImageAsync(Container container, string tagPrefix, string newTag)
    {
        await Spinner.StartAsync($"Stopping running container '{container.ContainerName}'",
            async _ =>
            {
                try
                {
                    await _stopContainerCommand.ExecuteAsync(container.Id);
                }
                catch (Exception)
                {
                    // ignored
                }
            });
        await Spinner.StartAsync("Launching image", async _ =>
        {
            var containerName = await _createContainerCommand.ExecuteAsync(container, tagPrefix, newTag);
            await _runContainerCommand.ExecuteAsync(containerName);
        });
    }

    private async Task<(string imageName, string tagPrefix, string newTag)> GetNewTagAsync(Container container,
        string tag)
    {
        var image = await _getImageQuery.QueryAsync(container.ImageIdentifier, container.ImageTag);
        string imageName;
        string? baseTag = null;
        if (image == null)
        {
            var digests = await _getDigestsByIdQuery.QueryAsync(container.ImageIdentifier);
            var digest = digests?.SingleOrDefault();
            if (digest == null || !DigestHelper.TryGetImageNameAndId(digest, out var nameNameAndId))
                throw new InvalidOperationException(
                    $"Unable to determine image name from running container '{container.ContainerName}'");
            imageName = nameNameAndId.imageName;
        }
        else
        {
            imageName = image.Name;
            baseTag = image.BaseImage?.Tag;
        }

        baseTag = container.GetLabel(Constants.BaseTagLabel) ?? baseTag ?? image?.Tag;

        var tagPrefix = container.TagPrefix;
        var newTag = baseTag == null ? tag : $"{tagPrefix}{baseTag}-{tag}";
        if (newTag.Contains('.')) throw new ArgumentException("only [a-zA-Z0-9][a-zA-Z0-9_-] are allowed");
        return (imageName, tagPrefix, newTag);
    }

    private async Task<Container?> GetContainerAsync(IContainerIdentifierSettings settings)
    {
        var containers = await _getRunningContainersQuery.QueryAsync().ToListAsync();
        if (settings.ContainerIdentifier != null)
        {
            return containers.SingleOrDefault(c => c.ContainerName == settings.ContainerIdentifier);
        }

        var identifier = _containerNamePrompt.GetIdentifierOfContainerFromUser(containers, "commit");
        return containers.SingleOrDefault(c => c.ContainerName == identifier);
    }
}