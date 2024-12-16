using port.Commands.List;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Reset;

internal class ResetCliCommand : AsyncCommand<ResetSettings>
{
    private readonly IGetRunningContainersQuery _getRunningContainersQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;
    private readonly IContainerNamePrompt _containerNamePrompt;
    private readonly IImageIdentifierPrompt _imageIdentifierPrompt;
    private readonly IGetImageQuery _getImageQuery;
    private readonly ICreateImageCommand _createImageCommand;
    private readonly ListCliCommand _listCliCommand;

    public ResetCliCommand(
        IGetRunningContainersQuery getRunningContainersQuery,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand,
        ICreateContainerCommand createContainerCommand,
        IRunContainerCommand runContainerCommand,
        IContainerNamePrompt containerNamePrompt,
        IImageIdentifierPrompt imageIdentifierPrompt,
        IGetImageQuery getImageQuery,
        ICreateImageCommand createImageCommand,
        ListCliCommand listCliCommand)
    {
        _getRunningContainersQuery = getRunningContainersQuery;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
        _containerNamePrompt = containerNamePrompt;
        _imageIdentifierPrompt = imageIdentifierPrompt;
        _getImageQuery = getImageQuery;
        _createImageCommand = createImageCommand;
        _listCliCommand = listCliCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ResetSettings settings)
    {
        if (settings.IsImage)
        {
            await ResetImageBaseTagAsync(settings);
        }
        else
        {
            var container = await GetContainerAsync(settings);
            if (container == null)
            {
                throw new InvalidOperationException("No running container found");
            }

            await ResetContainerAsync(container);
        }

        await _listCliCommand.ExecuteAsync();

        return 0;
    }

    private async Task<Container?> GetContainerAsync(IContainerIdentifierSettings settings)
    {
        var containers = await _getRunningContainersQuery.QueryAsync().ToListAsync();
        if (settings.ContainerIdentifier != null)
        {
            return containers.SingleOrDefault(c => c.ContainerName == settings.ContainerIdentifier);
        }

        var identifier = _containerNamePrompt.GetIdentifierOfContainerFromUser(containers, "reset");
        return containers.SingleOrDefault(c => c.ContainerName == identifier);
    }

    private async Task ResetContainerAsync(Container container)
    {
        await Spinner.StartAsync(
                $"Resetting container '{container.ContainerName}'",
                async _ =>
                {
                    await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
                    await _createContainerCommand.ExecuteAsync(container);
                    await _runContainerCommand.ExecuteAsync(container);
                });
    }

    private async Task ResetImageBaseTagAsync(ResetSettings settings)
    {
        var image = await GetImageAsync(settings);
        if (image == null)
        {
            throw new InvalidOperationException("Image not found");
        }

        var tag = settings.Tag ?? image.Tag;
        if (tag == null)
        {
            throw new InvalidOperationException("No tag specified and image has no tag");
        }

        await Spinner.StartAsync(
            $"Resetting base tag for image '{image.Name}' to '{tag}'",
            async _ =>
            {
                // Create a new image with the same content but new base tag
                var labels = new Dictionary<string, string>
                {
                    { Constants.BaseTag, tag }
                };

                await _createImageCommand.ExecuteAsync(image.Name, tag);
            });
    }

    private async Task<Image?> GetImageAsync(IImageIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return await _getImageQuery.QueryAsync(settings.ImageIdentifier);
        }

        var images = await _getImageQuery.QueryAllAsync().ToListAsync();
        var identifier = _imageIdentifierPrompt.GetIdentifierOfImageFromUser(images, "reset base tag");
        return await _getImageQuery.QueryAsync(identifier);
    }
}
