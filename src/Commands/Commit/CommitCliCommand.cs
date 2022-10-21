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

    public CommitCliCommand(ICreateImageFromContainerCommand createImageFromContainerCommand,
        IGetRunningContainersQuery getRunningContainersQuery, IGetImageQuery getImageQuery,
        IContainerNamePrompt containerNamePrompt, IStopContainerCommand stopContainerCommand,
        ICreateContainerCommand createContainerCommand, IRunContainerCommand runContainerCommand,
        IGetDigestsByIdQuery getDigestsByIdQuery)
    {
        _createImageFromContainerCommand = createImageFromContainerCommand;
        _getRunningContainersQuery = getRunningContainersQuery;
        _getImageQuery = getImageQuery;
        _containerNamePrompt = containerNamePrompt;
        _stopContainerCommand = stopContainerCommand;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
        _getDigestsByIdQuery = getDigestsByIdQuery;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CommitSettings settings)
    {
        var tag = settings.Tag ?? $"{DateTime.Now:yyyyMMddhhmmss}";

        var container = await GetContainerAsync(settings);
        if (container == null)
        {
            throw new InvalidOperationException("No running container found");
        }

        var newTag = await CommitContainerAsync(container, tag);

        if (!settings.Switch) return 0;

        if (newTag == null)
            throw new InvalidOperationException("newTag is null");

        if (container.ImageTag == null)
            throw new InvalidOperationException(
                "Switch argument not supported when creating image from untagged container");

        await SwitchToNewImageAsync(container, newTag);

        return 0;
    }

    private async Task SwitchToNewImageAsync(Container container, string newTag)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Stopping running container '{container.ContainerName}'",
                _ => _stopContainerCommand.ExecuteAsync(container.Id));
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Launching {ImageNameHelper.BuildImageName(container.ImageIdentifier, newTag)}", async _ =>
            {
                var containerName = await _createContainerCommand.ExecuteAsync(container.ContainerIdentifier,
                    container.ImageIdentifier, newTag, container.Ports);
                await _runContainerCommand.ExecuteAsync(containerName);
            });
        AnsiConsole.WriteLine($"Launched {ImageNameHelper.BuildImageName(container.ImageIdentifier, newTag)}");
    }

    private async Task<string?> CommitContainerAsync(Container container, string tag)
    {
        var image = await _getImageQuery.QueryAsync(container.ImageIdentifier, container.ImageTag);
        string imageName;
        string? baseTag = null;
        if (image == null)
        {
            var digests = await _getDigestsByIdQuery.QueryAsync(container.ImageIdentifier);
            var digest = digests?.SingleOrDefault();
            if (digest == null || !DigestHelper.TryGetImageNameAndId(digest, out var nameNameAndId))
                throw new InvalidOperationException($"Unable to determine image name from running container '{container.ContainerName}'");
            imageName = nameNameAndId.imageName;
        }
        else
        {
            while (image.Parent != null)
            {
                image = image.Parent;
            }

            imageName = image.Name;
            baseTag = image.Tag;
        }
        
        baseTag ??= (ContainerNameHelper.TryGetContainerNameAndTag(container.ContainerName, out var identifierAndTag) 
            ? identifierAndTag.tag 
            : container.ContainerName);
        
        string? newTag = null;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Creating image from running container '{container.ContainerName}'", async _ =>
            {
                return newTag =
                    await _createImageFromContainerCommand.ExecuteAsync(container.Id,
                        imageName,
                        baseTag,
                        tag);
            });
        AnsiConsole.WriteLine($"Created image with tag {tag}");
        return newTag;
    }

    private async Task<Container?> GetContainerAsync(IContainerIdentifierSettings settings)
    {
        var containers = await _getRunningContainersQuery.QueryAsync();
        if (settings.ContainerIdentifier != null)
        {
            return containers.SingleOrDefault(c => c.ContainerName == settings.ContainerIdentifier);
        }

        var identifier = _containerNamePrompt.GetIdentifierOfContainerFromUser(containers, "commit");
        return containers.SingleOrDefault(c => c.ContainerName == identifier);
    }
}