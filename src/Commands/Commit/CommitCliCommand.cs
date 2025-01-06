using port.Commands.List;
using Spectre.Console.Cli;

namespace port.Commands.Commit;

internal class CommitCliCommand(
    ICreateImageFromContainerCommand createImageFromContainerCommand,
    IGetRunningContainersQuery getRunningContainersQuery,
    IGetImageQuery getImageQuery,
    IContainerNamePrompt containerNamePrompt,
    IStopContainerCommand stopContainerCommand,
    ICreateContainerCommand createContainerCommand,
    IRunContainerCommand runContainerCommand,
    IGetDigestsByIdQuery getDigestsByIdQuery,
    IGetContainersQuery getContainersQuery,
    IStopAndRemoveContainerCommand stopAndRemoveContainerCommand,
    ListCliCommand listCliCommand)
    : AsyncCommand<CommitSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CommitSettings settings)
    {
        if (settings.Overwrite) settings.Switch = true;

        var container = await GetContainerAsync(settings) ??
                        throw new InvalidOperationException("No running container found");

        await Spinner.StartAsync("Committing container", async ctx =>
        {
            string newTag;
            string imageName;
            string tagPrefix;
            if (settings.Overwrite)
            {
                newTag = container.ImageTag ??
                         throw new InvalidOperationException(
                             "When using --overwrite, container must have an image tag");
                imageName = container.ImageIdentifier;
                tagPrefix = container.TagPrefix;
            }
            else
            {
                var tag = settings.Tag ?? $"{DateTime.Now:yyyyMMddhhmmss}";
                (imageName, tagPrefix, newTag) = await GetNewTagAsync(container, tag);
            }


            ctx.Status = $"Looking for existing container named '{container.ContainerName}'";
            var containerWithSameTag = await getContainersQuery
                .QueryByContainerIdentifierAndTagAsync(container.ContainerIdentifier, newTag)
                .ToListAsync();

            ctx.Status = $"Creating image from running container '{container.ContainerName}'";
            newTag = await createImageFromContainerCommand.ExecuteAsync(container, imageName, tagPrefix, newTag);

            ctx.Status = $"Removing containers named '{container.ContainerName}'";
            await Task.WhenAll(containerWithSameTag.Select(async container1 =>
                await stopAndRemoveContainerCommand.ExecuteAsync(container1.Id)));

            if (settings.Switch)
            {
                if (newTag == null)
                    throw new InvalidOperationException("newTag is null");

                if (container.ImageTag == null)
                    throw new InvalidOperationException(
                        "Switch argument not supported when creating image from untagged container");

                ctx.Status = $"Stopping running container '{container.ContainerName}'";
                await stopContainerCommand.ExecuteAsync(container.Id);

                ctx.Status = "Launching new image";
                var containerName = await createContainerCommand.ExecuteAsync(container, tagPrefix, newTag);
                await runContainerCommand.ExecuteAsync(containerName);
            }
            else
            {
                ctx.Status = "Launching new image";
                var containerName = await createContainerCommand.ExecuteAsync(container, tagPrefix, newTag);
                await runContainerCommand.ExecuteAsync(containerName);
            }
        });

        await listCliCommand.ExecuteAsync();
        return 0;
    }

    private async Task<(string imageName, string tagPrefix, string newTag)> GetNewTagAsync(Container container,
        string tag)
    {
        var image = await getImageQuery.QueryAsync(container.ImageIdentifier, container.ImageTag);
        string imageName;
        string? baseTag = null;
        if (image == null)
        {
            var digests = await getDigestsByIdQuery.QueryAsync(container.ImageIdentifier);
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
        var containers = await getRunningContainersQuery.QueryAsync().ToListAsync();
        if (settings.ContainerIdentifier != null)
        {
            return containers.SingleOrDefault(c => c.ContainerName == settings.ContainerIdentifier);
        }

        var identifier = containerNamePrompt.GetIdentifierOfContainerFromUser(containers, "commit");
        return containers.SingleOrDefault(c => c.ContainerName == identifier);
    }
}